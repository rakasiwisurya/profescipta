<#
    install-database.ps1
    --------------------
    Menjalankan seluruh script database dalam urutan yang benar:
    schema -> view -> stored procedure -> (opsional) data contoh order.

    Script ini tidak butuh sqlcmd, cukup Windows PowerShell
    (memakai System.Data.SqlClient bawaan .NET Framework).

    Contoh pemakaian:
      # Windows Authentication (default)
      .\install-database.ps1 -ServerInstance "localhost\SQLEXPRESS"

      # SQL Authentication
      .\install-database.ps1 -ServerInstance "localhost\SQLEXPRESS" -UserId sa -Password "P@ssw0rd"

      # Tanpa data contoh order
      .\install-database.ps1 -ServerInstance "localhost\SQLEXPRESS" -SkipSampleOrders
#>
[CmdletBinding()]
param(
    [string] $ServerInstance = "localhost\SQLEXPRESS",
    [string] $UserId,
    [string] $Password,
    [switch] $SkipSampleOrders
)

$ErrorActionPreference = "Stop"

# Urutan eksekusi penting: tabel dulu, baru view, baru SP.
$scripts = @(
    "schema.sql",
    "vw_SalesOrderSummary.sql",
    "usp_Customer_GetAll.sql",
    "usp_SalesOrder_Search.sql",
    "usp_SalesOrder_GetById.sql",
    "usp_SalesOrder_Insert.sql",
    "usp_SalesOrder_Update.sql",
    "usp_SalesOrder_Delete.sql",
    "usp_SalesOrder_SoNoExists.sql",
    "usp_SalesOrderItem_Insert.sql",
    "usp_SalesOrderItem_DeleteByOrder.sql"
)
if (-not $SkipSampleOrders) { $scripts += "sample-data-orders.sql" }

if ($UserId) {
    $connectionString = "Server=$ServerInstance;Database=master;User Id=$UserId;Password=$Password;TrustServerCertificate=True"
} else {
    $connectionString = "Server=$ServerInstance;Database=master;Integrated Security=True;TrustServerCertificate=True"
}

Add-Type -AssemblyName System.Data
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

$connection = New-Object System.Data.SqlClient.SqlConnection $connectionString
$connection.Open()
Write-Host "Terhubung ke $ServerInstance" -ForegroundColor Green

try {
    foreach ($file in $scripts) {
        $path = Join-Path $scriptDir $file
        if (-not (Test-Path $path)) { throw "File tidak ditemukan: $path" }

        Write-Host "-> $file" -ForegroundColor Cyan
        $sql = Get-Content $path -Raw

        # Pemisah GO bukan perintah T-SQL, jadi harus dipecah manual
        # sebelum dikirim lewat SqlCommand.
        $batches = [regex]::Split($sql, '(?im)^\s*GO\s*$')

        foreach ($batch in $batches) {
            if ([string]::IsNullOrWhiteSpace($batch)) { continue }
            $command = $connection.CreateCommand()
            $command.CommandText = $batch
            $command.CommandTimeout = 120
            [void] $command.ExecuteNonQuery()
        }
    }
    Write-Host "`nSelesai. Database SalesOrderDb siap dipakai." -ForegroundColor Green
}
finally {
    $connection.Close()
}

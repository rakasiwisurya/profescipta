<#
    run-all.ps1
    -----------
    Menjalankan ketiga komponen sekaligus, masing-masing di jendela
    PowerShell sendiri supaya log-nya mudah dibaca dan bisa dihentikan
    satu per satu dengan Ctrl+C.

    Pemakaian:
      .\run-all.ps1

    Prasyarat: database sudah disiapkan lewat Database\install-database.ps1
#>
[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path

$components = @(
    @{ Name = "Customer Service";   Project = "CustomerService\CustomerService.csproj";     Url = "http://localhost:5001" },
    @{ Name = "Sales Order Service"; Project = "SalesOrderService\SalesOrderService.csproj"; Url = "http://localhost:5002" },
    @{ Name = "Front-End";          Project = "FrontEnd\FrontEnd.csproj";                   Url = "http://localhost:5000" }
)

foreach ($component in $components) {
    $projectPath = Join-Path $root $component.Project

    if (-not (Test-Path $projectPath)) {
        throw "Project tidak ditemukan: $projectPath"
    }

    Write-Host ("Menjalankan {0} di {1} ..." -f $component.Name, $component.Url) -ForegroundColor Cyan

    Start-Process -FilePath "powershell.exe" -ArgumentList @(
        "-NoExit",
        "-Command",
        "Set-Location '$root'; Write-Host '=== $($component.Name) ($($component.Url)) ===' -ForegroundColor Green; dotnet run --project '$($component.Project)' --no-launch-profile"
    )
}

Write-Host ""
Write-Host "Ketiga service sedang dijalankan (butuh beberapa detik untuk siap)." -ForegroundColor Green
Write-Host "UI aplikasi      : http://localhost:5000"
Write-Host "Swagger customer : http://localhost:5001/swagger"
Write-Host "Swagger order    : http://localhost:5002/swagger"
Write-Host ""
Write-Host "Tutup jendela masing-masing service untuk menghentikannya." -ForegroundColor Yellow

namespace FrontEnd.Models;

/// <summary>
/// Salinan bentuk data (DTO) yang dipertukarkan front-end dengan service.
///
/// Kelas-kelas ini sengaja didefinisikan ulang di project Front-End dan
/// tidak dibagi lewat project bersama: tiap service pada arsitektur
/// microservices memiliki kontraknya sendiri, dan front-end hanya
/// bergantung pada kontrak HTTP-nya — bukan pada kode internal service.
/// </summary>
public class CustomerDto
{
    public int CustomerId { get; set; }

    public string CustomerName { get; set; } = string.Empty;
}

/// <summary>Satu baris pada grid Order List.</summary>
public class OrderListItemDto
{
    public int SalesSoId { get; set; }

    public string SoNo { get; set; } = string.Empty;

    public DateTime OrderDate { get; set; }

    public int CustomerId { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public string? Address { get; set; }

    public decimal GrandTotal { get; set; }
}

/// <summary>Detail order beserta item, untuk mengisi form mode Edit.</summary>
public class OrderDetailDto
{
    public int SalesSoId { get; set; }

    public string SoNo { get; set; } = string.Empty;

    public DateTime OrderDate { get; set; }

    public int CustomerId { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public string? Address { get; set; }

    public decimal GrandTotal { get; set; }

    public List<OrderItemDto> Items { get; set; } = new();
}

public class OrderItemDto
{
    public int SalesSoLitemId { get; set; }

    public string ItemName { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal Price { get; set; }

    /// <summary>Selalu berasal dari service, tidak pernah dihitung front-end.</summary>
    public decimal Total { get; set; }
}

/// <summary>Body request untuk POST/PUT order.</summary>
public class SaveOrderRequest
{
    public string? SoNo { get; set; }

    public DateTime? OrderDate { get; set; }

    public int? CustomerId { get; set; }

    public string? Address { get; set; }

    public List<SaveOrderItemRequest> Items { get; set; } = new();
}

public class SaveOrderItemRequest
{
    public string? ItemName { get; set; }

    public int? Quantity { get; set; }

    public decimal? Price { get; set; }
}

/// <summary>Body request untuk POST /api/orders/calculate.</summary>
public class CalculateItemsRequest
{
    public List<SaveOrderItemRequest> Items { get; set; } = new();
}

/// <summary>Respons kalkulasi dari service: TOTAL per baris + Grand Total.</summary>
public class CalculateItemsResponse
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public List<CalculatedItemDto> Items { get; set; } = new();

    public decimal GrandTotal { get; set; }
}

public class CalculatedItemDto
{
    public int RowIndex { get; set; }

    public string ItemName { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal Price { get; set; }

    public decimal Total { get; set; }

    public bool IsValid { get; set; }

    public List<string> Errors { get; set; } = new();
}

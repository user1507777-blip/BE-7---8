using System.ComponentModel.DataAnnotations;

namespace AuthApi.Models;

public enum OrderStatus
{
    Pending,    // Chờ xác nhận
    Confirmed,  // Đã xác nhận
    Cancelled   // Đã hủy
}

public class Order
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }

    public string ReceiverName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;

    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ConfirmedAt { get; set; }

    public List<OrderItem> Items { get; set; } = new();

    [Timestamp]
    public byte[]? RowVersion { get; set; }
}

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order? Order { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    // Lưu lại tên và giá lúc đặt, để sau này đổi giá không ảnh hưởng đơn cũ
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
}

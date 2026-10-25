using System.ComponentModel.DataAnnotations;

namespace AuthApi.Dtos;

public class CreateProductDto
{
    [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm")]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Range(0, 1_000_000_000, ErrorMessage = "Giá không hợp lệ")]
    public decimal Price { get; set; }

    [Range(0, 1_000_000, ErrorMessage = "Số lượng tồn không hợp lệ")]
    public int Stock { get; set; }
}

public class OrderItemDto
{
    [Range(1, int.MaxValue, ErrorMessage = "ProductId không hợp lệ")]
    public int ProductId { get; set; }

    [Range(1, 1000, ErrorMessage = "Số lượng phải từ 1 đến 1000")]
    public int Quantity { get; set; }
}

public class CreateOrderDto
{
    [Required(ErrorMessage = "Vui lòng nhập tên người nhận")]
    [StringLength(100)]
    public string ReceiverName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
    [RegularExpression(@"^\+?[0-9]{9,15}$", ErrorMessage = "Số điện thoại không hợp lệ")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập địa chỉ giao hàng")]
    [StringLength(500)]
    public string Address { get; set; } = string.Empty;

    [Required, MinLength(1, ErrorMessage = "Đơn hàng phải có ít nhất 1 sản phẩm")]
    public List<OrderItemDto> Items { get; set; } = new();
}

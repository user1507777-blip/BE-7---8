using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AuthApi.Data;
using AuthApi.Dtos;
using AuthApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuthApi.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly AppDbContext _db;

    public OrdersController(AppDbContext db) => _db = db;

    private int CurrentUserId => int.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
    private bool IsAdmin => User.IsInRole(Roles.Admin);

    // ĐẶT HÀNG  POST /api/orders
    [HttpPost]
    public async Task<IActionResult> Create(CreateOrderDto dto)
    {
        var productIds = dto.Items.Select(i => i.ProductId).Distinct().ToList();

        var products = await _db.Products
            .Where(p => productIds.Contains(p.Id) && p.IsActive)
            .ToDictionaryAsync(p => p.Id);

        var missing = productIds.Where(id => !products.ContainsKey(id)).ToList();
        if (missing.Count > 0)
            return BadRequest(new
            {
                message = $"Sản phẩm không tồn tại hoặc đã ngừng bán: {string.Join(", ", missing)}"
            });

        var order = new Order
        {
            UserId = CurrentUserId,
            ReceiverName = dto.ReceiverName.Trim(),
            Phone = dto.Phone.Trim(),
            Address = dto.Address.Trim(),
            Status = OrderStatus.Pending
        };

        // Gộp các dòng trùng sản phẩm
        foreach (var group in dto.Items.GroupBy(i => i.ProductId))
        {
            var product = products[group.Key];
            order.Items.Add(new OrderItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                UnitPrice = product.Price,
                Quantity = group.Sum(x => x.Quantity)
            });
        }

        order.TotalAmount = order.Items.Sum(i => i.UnitPrice * i.Quantity);

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = order.Id },
            new { message = "Đặt hàng thành công, đơn đang chờ xác nhận", order = ToDto(order) });
    }

    // Danh sách đơn hàng  GET /api/orders?status=Pending
    // Khách chỉ thấy đơn của mình, Admin thấy tất cả.
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] OrderStatus? status)
    {
        var query = _db.Orders.Include(o => o.Items).AsQueryable();

        if (!IsAdmin)
            query = query.Where(o => o.UserId == CurrentUserId);

        if (status != null)
            query = query.Where(o => o.Status == status);

        var orders = await query.OrderByDescending(o => o.CreatedAt).ToListAsync();
        return Ok(orders.Select(ToDto));
    }

    // Chi tiết đơn hàng  GET /api/orders/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var order = await _db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id);

        if (order == null || (!IsAdmin && order.UserId != CurrentUserId))
            return NotFound(new { message = "Không tìm thấy đơn hàng" });

        return Ok(ToDto(order));
    }

    // XÁC NHẬN ĐƠN HÀNG (chỉ Admin)  POST /api/orders/5/confirm
    [Authorize(Roles = Roles.Admin)]
    [HttpPost("{id:int}/confirm")]
    public async Task<IActionResult> Confirm(int id)
    {
        var order = await _db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id);
        if (order == null)
            return NotFound(new { message = "Không tìm thấy đơn hàng" });

        if (order.Status != OrderStatus.Pending)
            return BadRequest(new
            {
                message = $"Đơn hàng đang ở trạng thái {order.Status}, không thể xác nhận"
            });

        var productIds = order.Items.Select(i => i.ProductId).ToList();
        var products = await _db.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        // Kiểm tra tồn kho trước khi xác nhận
        foreach (var item in order.Items)
        {
            var product = products[item.ProductId];
            if (product.Stock < item.Quantity)
                return Conflict(new
                {
                    message = $"Sản phẩm \"{product.Name}\" chỉ còn {product.Stock}, không đủ {item.Quantity}"
                });
        }

        foreach (var item in order.Items)
            products[item.ProductId].Stock -= item.Quantity;

        order.Status = OrderStatus.Confirmed;
        order.ConfirmedAt = DateTime.UtcNow;

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { message = "Dữ liệu vừa bị thay đổi bởi người khác, vui lòng thử lại" });
        }

        return Ok(new { message = "Đã xác nhận đơn hàng", order = ToDto(order) });
    }

    // HỦY ĐƠN (chủ đơn hoặc Admin, chỉ khi chưa xác nhận)  POST /api/orders/5/cancel
    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id)
    {
        var order = await _db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id);

        if (order == null || (!IsAdmin && order.UserId != CurrentUserId))
            return NotFound(new { message = "Không tìm thấy đơn hàng" });

        if (order.Status != OrderStatus.Pending)
            return BadRequest(new
            {
                message = $"Đơn hàng đang ở trạng thái {order.Status}, không thể hủy"
            });

        order.Status = OrderStatus.Cancelled;

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { message = "Dữ liệu vừa bị thay đổi bởi người khác, vui lòng thử lại" });
        }

        return Ok(new { message = "Đã hủy đơn hàng", order = ToDto(order) });
    }

    private static object ToDto(Order o) => new
    {
        o.Id,
        o.UserId,
        o.ReceiverName,
        o.Phone,
        o.Address,
        Status = o.Status.ToString(),
        o.TotalAmount,
        o.CreatedAt,
        o.ConfirmedAt,
        Items = o.Items.Select(i => new
        {
            i.ProductId,
            i.ProductName,
            i.UnitPrice,
            i.Quantity,
            LineTotal = i.UnitPrice * i.Quantity
        })
    };
}

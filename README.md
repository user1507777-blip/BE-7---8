# AuthApi - Đăng ký / Đăng nhập / Đăng xuất

ASP.NET Core 8 Web API + Entity Framework Core (SQL Server) + JWT + BCrypt.

## Chạy

1. Mở `AuthApi.sln` bằng Visual Studio 2022 (cần .NET 8 SDK), hoặc dùng terminal:
   ```bash
   dotnet restore
   dotnet run --project AuthApi
   ```
2. Chuỗi kết nối nằm ở `AuthApi/appsettings.json` (mặc định dùng SQL Server LocalDB, có sẵn khi cài Visual Studio). Database `AuthDb` và các bảng tự được tạo khi chạy lần đầu.
3. **Đổi `Jwt:Key`** thành chuỗi bí mật của riêng bạn (ít nhất 32 ký tự) trước khi dùng thật. Không nên đẩy khóa thật lên git.
4. Mở Swagger tại `https://localhost:<port>/swagger` để thử.

## API

| Phương thức | Đường dẫn | Mô tả | Body |
|---|---|---|---|
| POST | `/api/auth/register` | Đăng ký | `name`, `email`, `password` |
| POST | `/api/auth/login` | Đăng nhập, trả về `token` | `email`, `password` |
| POST | `/api/auth/logout` | Đăng xuất (cần token) | không |
| GET | `/api/auth/me` | Thông tin người dùng hiện tại | không |

Với các API cần đăng nhập, gửi header `Authorization: Bearer <token>`. Trong Swagger, bấm nút **Authorize** rồi dán token.

## Sản phẩm, đặt hàng, xác nhận đơn

| Phương thức | Đường dẫn | Quyền | Mô tả |
|---|---|---|---|
| GET | `/api/products` | ai cũng xem được | Danh sách sản phẩm |
| POST | `/api/products` | Admin | Thêm sản phẩm (`name`, `price`, `stock`) |
| POST | `/api/orders` | đã đăng nhập | **Đặt hàng**, đơn ở trạng thái `Pending` |
| GET | `/api/orders` | đã đăng nhập | Đơn của mình (Admin thấy tất cả), lọc bằng `?status=Pending` |
| GET | `/api/orders/{id}` | chủ đơn hoặc Admin | Chi tiết đơn |
| POST | `/api/orders/{id}/confirm` | Admin | **Xác nhận đơn**: `Pending` thành `Confirmed`, trừ tồn kho |
| POST | `/api/orders/{id}/cancel` | chủ đơn hoặc Admin | Hủy đơn khi còn `Pending` |

Ví dụ body đặt hàng:

```json
{
  "receiverName": "Nguyễn Văn A",
  "phone": "0901234567",
  "address": "12 Lê Lợi, Quận 1, TP.HCM",
  "items": [
    { "productId": 1, "quantity": 2 },
    { "productId": 3, "quantity": 1 }
  ]
}
```

Luồng thử: đăng ký và đăng nhập tài khoản khách, đặt hàng, rồi đăng nhập tài khoản Admin có sẵn (`admin@example.com` / `Admin@123`, nên đổi trong `Program.cs`) để xác nhận đơn. Khi xác nhận, hệ thống kiểm tra tồn kho, nếu không đủ sẽ báo lỗi 409.

**Lưu ý nếu bạn đã chạy bản cũ:** database `AuthDb` đã tạo trước đó chưa có các bảng mới và cột `Role`, hãy xóa database đó (SQL Server Object Explorer, hoặc `dotnet ef database drop` nếu đã cài `dotnet-ef`) rồi chạy lại, các bảng sẽ được tạo mới.

## Cách hoạt động

- **Đăng ký**: kiểm tra dữ liệu, kiểm tra trùng email, băm mật khẩu bằng BCrypt rồi lưu.
- **Đăng nhập**: so khớp mật khẩu, tạo JWT có `jti` riêng.
- **Đăng xuất**: lưu `jti` của token vào bảng `RevokedTokens`. Mỗi request sau đó đều kiểm tra bảng này nên token cũ bị từ chối dù chưa hết hạn.

## Đưa lên Git

```bash
git init
git add .
git commit -m "Add register, login, logout API"
git branch -M main
git remote add origin https://github.com/<ten-ban>/<ten-repo>.git
git push -u origin main
```

Tạo repo trống trên GitHub trước, rồi thay đường dẫn `origin` bằng link repo của bạn. File `.gitignore` đã loại `bin/`, `obj/`, `.vs/` nên sẽ không bị đẩy lên.

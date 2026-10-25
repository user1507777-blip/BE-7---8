namespace AuthApi.Models;

// Token đã đăng xuất. Dùng để từ chối token cũ dù chưa hết hạn.
public class RevokedToken
{
    public int Id { get; set; }
    public string Jti { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

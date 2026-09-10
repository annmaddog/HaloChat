using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HaloChat.Api.Models;

public class NguoiDung
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    public string TenTaiKhoan { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string MatKhauBam { get; set; } = string.Empty;
    public string Salt { get; set; } = string.Empty;

    // [BẢO MẬT - GĐ6] Cặp khóa RSA (KhoaCongKhai/KhoaBiMat) sẽ được sinh và
    // gán vào đây khi nhóm triển khai mã hóa lai RSA-AES. Để trống ở giai
    // đoạn này theo đúng chính sách stub trong spec (§9).
    public string KhoaCongKhai { get; set; } = string.Empty;
    public string KhoaBiMat { get; set; } = string.Empty;

    public DateTime NgayTao { get; set; } = DateTime.UtcNow;
}

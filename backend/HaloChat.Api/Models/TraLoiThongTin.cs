using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HaloChat.Api.Models;

public class TraLoiThongTin
{
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;
    public string TenNguoiGui { get; set; } = string.Empty;
    public string NoiDungTomTat { get; set; } = string.Empty;
    [BsonRepresentation(BsonType.String)]
    public LoaiTinNhan LoaiTinNhan { get; set; }
}

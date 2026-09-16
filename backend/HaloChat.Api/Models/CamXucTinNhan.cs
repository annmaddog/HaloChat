using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HaloChat.Api.Models;

public enum LoaiCamXuc
{
    Thich,
    YeuThich,
    Haha,
    Wow,
    Buon,
    PhanNo,
}

public class CamXucTinNhan
{
    [BsonRepresentation(BsonType.ObjectId)]
    public string NguoiDungId { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.String)]
    public LoaiCamXuc LoaiCamXuc { get; set; }
}

using HaloChat.Api.Models;

namespace HaloChat.Api.Repositories;

public interface IDocNhomRepository
{
    /// <summary>Ghi nhận nguoiDungId đã đọc tới tinNhanCuoiId trong nhomId (upsert).</summary>
    Task DanhDauDaDocAsync(string nguoiDungId, string nhomId, string tinNhanCuoiId);

    /// <summary>Id tin nhắn cuối nguoiDungId đã đọc trong nhomId, null nếu chưa từng đọc.</summary>
    Task<string?> LayTinNhanCuoiDaDocAsync(string nguoiDungId, string nhomId);
}

using HaloChat.Api.Repositories;

namespace HaloChat.Api.Tests.Fakes;

public class DocNhomGiaLap : IDocNhomRepository
{
    // Khóa "nguoiDungId|nhomId" -> id tin nhắn cuối đã đọc.
    public Dictionary<string, string> DaDoc { get; } = new();

    public Task DanhDauDaDocAsync(string nguoiDungId, string nhomId, string tinNhanCuoiId)
    {
        DaDoc[$"{nguoiDungId}|{nhomId}"] = tinNhanCuoiId;
        return Task.CompletedTask;
    }

    public Task<string?> LayTinNhanCuoiDaDocAsync(string nguoiDungId, string nhomId)
    {
        DaDoc.TryGetValue($"{nguoiDungId}|{nhomId}", out var ketQua);
        return Task.FromResult(ketQua);
    }
}

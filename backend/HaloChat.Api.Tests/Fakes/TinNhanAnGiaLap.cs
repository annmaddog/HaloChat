using HaloChat.Api.Repositories;

namespace HaloChat.Api.Tests.Fakes;

public class TinNhanAnGiaLap : ITinNhanAnRepository
{
    // Danh sách (nguoiDungId, tinNhanId) đã ẩn.
    public List<(string NguoiDungId, string TinNhanId)> DanhSach { get; } = new();

    public Task AnAsync(string nguoiDungId, string tinNhanId)
    {
        if (!DanhSach.Contains((nguoiDungId, tinNhanId)))
        {
            DanhSach.Add((nguoiDungId, tinNhanId));
        }
        return Task.CompletedTask;
    }

    public Task<HashSet<string>> LayDanhSachIdDaAnAsync(string nguoiDungId, IEnumerable<string> tinNhanIds)
    {
        var idQuanTam = tinNhanIds.ToHashSet();
        var ketQua = DanhSach
            .Where(x => x.NguoiDungId == nguoiDungId && idQuanTam.Contains(x.TinNhanId))
            .Select(x => x.TinNhanId)
            .ToHashSet();
        return Task.FromResult(ketQua);
    }
}

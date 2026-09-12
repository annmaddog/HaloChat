using HaloChat.Api.Models;
using HaloChat.Api.Repositories;

namespace HaloChat.Api.Tests.Fakes;

public class TinNhanGiaLap : ITinNhanRepository
{
    public List<TinNhan> DanhSach { get; } = new();

    public Task ThemMoiAsync(TinNhan tinNhan)
    {
        DanhSach.Add(tinNhan);
        return Task.CompletedTask;
    }

    public Task<List<TinNhan>> LayLichSuTheoNguoiDungAsync(string nguoiA, string nguoiB, string? truocId, int soLuong)
    {
        var ketQua = DanhSach
            .Where(t => (t.NguoiGuiId == nguoiA && t.NguoiNhanId == nguoiB) ||
                        (t.NguoiGuiId == nguoiB && t.NguoiNhanId == nguoiA))
            .Where(t => truocId is null || string.CompareOrdinal(t.Id, truocId) < 0)
            .OrderByDescending(t => t.Id)
            .Take(soLuong)
            .ToList();
        return Task.FromResult(ketQua);
    }

    public Task DanhDauDaDocAsync(string nguoiGuiId, string nguoiNhanId)
    {
        foreach (var t in DanhSach.Where(t => t.NguoiGuiId == nguoiGuiId && t.NguoiNhanId == nguoiNhanId))
        {
            t.DaDoc = true;
        }
        return Task.CompletedTask;
    }

    public Task<List<TinNhan>> LayTatCaLienQuanAsync(string nguoiDungId)
    {
        var ketQua = DanhSach
            .Where(t => t.NhomId is null && (t.NguoiGuiId == nguoiDungId || t.NguoiNhanId == nguoiDungId))
            .OrderByDescending(t => t.Id)
            .ToList();
        return Task.FromResult(ketQua);
    }
}

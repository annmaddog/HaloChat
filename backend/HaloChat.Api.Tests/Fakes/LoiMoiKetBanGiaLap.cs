using HaloChat.Api.Models;
using HaloChat.Api.Repositories;

namespace HaloChat.Api.Tests.Fakes;

public class LoiMoiKetBanGiaLap : ILoiMoiKetBanRepository
{
    public List<LoiMoiKetBan> DanhSach { get; } = new();

    private static bool KhopCapDoi(LoiMoiKetBan l, string nguoiA, string nguoiB) =>
        (l.NguoiGuiId == nguoiA && l.NguoiNhanId == nguoiB) || (l.NguoiGuiId == nguoiB && l.NguoiNhanId == nguoiA);

    public Task<bool> TonTaiLoiMoiDangHoatDongAsync(string nguoiA, string nguoiB) =>
        Task.FromResult(DanhSach.Any(l => KhopCapDoi(l, nguoiA, nguoiB) && l.TrangThai != TrangThaiLoiMoiKetBan.DaTuChoi));

    public Task ThemMoiAsync(LoiMoiKetBan loiMoi)
    {
        DanhSach.Add(loiMoi);
        return Task.CompletedTask;
    }

    public Task<LoiMoiKetBan?> TimTheoIdAsync(string id) =>
        Task.FromResult(DanhSach.FirstOrDefault(l => l.Id == id));

    public Task CapNhatTrangThaiAsync(string id, TrangThaiLoiMoiKetBan trangThai)
    {
        var loiMoi = DanhSach.FirstOrDefault(l => l.Id == id);
        if (loiMoi is not null)
        {
            loiMoi.TrangThai = trangThai;
        }
        return Task.CompletedTask;
    }

    public Task<List<LoiMoiKetBan>> LayBanBeAsync(string nguoiDungId) =>
        Task.FromResult(DanhSach
            .Where(l => l.TrangThai == TrangThaiLoiMoiKetBan.DaChapNhan &&
                        (l.NguoiGuiId == nguoiDungId || l.NguoiNhanId == nguoiDungId))
            .ToList());

    public Task<List<LoiMoiKetBan>> LayLoiMoiDenAsync(string nguoiDungId) =>
        Task.FromResult(DanhSach
            .Where(l => l.NguoiNhanId == nguoiDungId && l.TrangThai == TrangThaiLoiMoiKetBan.ChoDuyet)
            .ToList());

    public Task<List<LoiMoiKetBan>> LayLoiMoiGuiAsync(string nguoiDungId) =>
        Task.FromResult(DanhSach
            .Where(l => l.NguoiGuiId == nguoiDungId && l.TrangThai == TrangThaiLoiMoiKetBan.ChoDuyet)
            .ToList());

    public Task<bool> LaBanBeAsync(string nguoiA, string nguoiB) =>
        Task.FromResult(DanhSach.Any(l => KhopCapDoi(l, nguoiA, nguoiB) && l.TrangThai == TrangThaiLoiMoiKetBan.DaChapNhan));
}

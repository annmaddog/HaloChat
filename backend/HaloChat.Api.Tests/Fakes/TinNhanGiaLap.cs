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

    public Task<List<TinNhan>> LayLichSuNhomAsync(string nhomId, string? truocId, int soLuong)
    {
        var ketQua = DanhSach
            .Where(t => t.NhomId == nhomId)
            .Where(t => truocId is null || string.CompareOrdinal(t.Id, truocId) < 0)
            .OrderByDescending(t => t.Id)
            .Take(soLuong)
            .ToList();
        return Task.FromResult(ketQua);
    }

    public Task<int> DemTinNhanSauIdAsync(string nhomId, string? sauId, string loaiTruNguoiGuiId)
    {
        var ketQua = DanhSach
            .Where(t => t.NhomId == nhomId)
            .Where(t => t.NguoiGuiId != loaiTruNguoiGuiId)
            .Where(t => sauId is null || string.CompareOrdinal(t.Id, sauId) > 0)
            .Count();
        return Task.FromResult(ketQua);
    }

    public Task XoaTheoNhomAsync(string nhomId)
    {
        DanhSach.RemoveAll(t => t.NhomId == nhomId);
        return Task.CompletedTask;
    }

    public Task<TinNhan?> TimTheoIdAsync(string id)
    {
        return Task.FromResult(DanhSach.FirstOrDefault(t => t.Id == id));
    }

    public Task DanhDauThuHoiAsync(string id)
    {
        var tinNhan = DanhSach.FirstOrDefault(t => t.Id == id);
        if (tinNhan is not null) tinNhan.DaThuHoi = true;
        return Task.CompletedTask;
    }

    public Task DatGhimAsync(string id, bool daGhim, DateTime? thoiGianGhim)
    {
        var tinNhan = DanhSach.FirstOrDefault(t => t.Id == id);
        if (tinNhan is not null)
        {
            tinNhan.DaGhim = daGhim;
            tinNhan.ThoiGianGhim = thoiGianGhim;
        }
        return Task.CompletedTask;
    }

    public Task<List<TinNhan>> LayTinDaGhimTheoNguoiDungAsync(string nguoiA, string nguoiB)
    {
        var ketQua = DanhSach
            .Where(t => t.DaGhim && ((t.NguoiGuiId == nguoiA && t.NguoiNhanId == nguoiB) || (t.NguoiGuiId == nguoiB && t.NguoiNhanId == nguoiA)))
            .OrderByDescending(t => t.ThoiGianGhim)
            .ToList();
        return Task.FromResult(ketQua);
    }

    public Task<List<TinNhan>> LayTinDaGhimTheoNhomAsync(string nhomId)
    {
        var ketQua = DanhSach
            .Where(t => t.NhomId == nhomId && t.DaGhim)
            .OrderByDescending(t => t.ThoiGianGhim)
            .ToList();
        return Task.FromResult(ketQua);
    }

    public Task<List<TinNhan>> LayMediaTheoNguoiDungAsync(string nguoiA, string nguoiB)
    {
        var ketQua = DanhSach
            .Where(t => t.LoaiTinNhan == LoaiTinNhan.Anh || t.LoaiTinNhan == LoaiTinNhan.File)
            .Where(t => (t.NguoiGuiId == nguoiA && t.NguoiNhanId == nguoiB) || (t.NguoiGuiId == nguoiB && t.NguoiNhanId == nguoiA))
            .OrderByDescending(t => t.ThoiGianTao)
            .ToList();
        return Task.FromResult(ketQua);
    }

    public Task<List<TinNhan>> LayMediaTheoNhomAsync(string nhomId)
    {
        var ketQua = DanhSach
            .Where(t => t.NhomId == nhomId && (t.LoaiTinNhan == LoaiTinNhan.Anh || t.LoaiTinNhan == LoaiTinNhan.File))
            .OrderByDescending(t => t.ThoiGianTao)
            .ToList();
        return Task.FromResult(ketQua);
    }

    public Task<List<TinNhan>> TimKiemTheoNguoiDungAsync(string nguoiA, string nguoiB, string tuKhoa)
    {
        var ketQua = DanhSach
            .Where(t => t.LoaiTinNhan == LoaiTinNhan.Text && !t.DaThuHoi)
            .Where(t => (t.NguoiGuiId == nguoiA && t.NguoiNhanId == nguoiB) || (t.NguoiGuiId == nguoiB && t.NguoiNhanId == nguoiA))
            .Where(t => t.NoiDungTinNhan.Contains(tuKhoa, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(t => t.ThoiGianTao)
            .Take(50)
            .ToList();
        return Task.FromResult(ketQua);
    }

    public Task<List<TinNhan>> TimKiemTheoNhomAsync(string nhomId, string tuKhoa)
    {
        var ketQua = DanhSach
            .Where(t => t.NhomId == nhomId && t.LoaiTinNhan == LoaiTinNhan.Text && !t.DaThuHoi)
            .Where(t => t.NoiDungTinNhan.Contains(tuKhoa, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(t => t.ThoiGianTao)
            .Take(50)
            .ToList();
        return Task.FromResult(ketQua);
    }

    public Task ThaCamXucAsync(string id, string nguoiDungId, LoaiCamXuc loaiCamXuc)
    {
        var tinNhan = DanhSach.FirstOrDefault(t => t.Id == id);
        if (tinNhan is not null)
        {
            tinNhan.DanhSachCamXuc.RemoveAll(cx => cx.NguoiDungId == nguoiDungId);
            tinNhan.DanhSachCamXuc.Add(new CamXucTinNhan { NguoiDungId = nguoiDungId, LoaiCamXuc = loaiCamXuc });
        }
        return Task.CompletedTask;
    }

    public Task BoCamXucAsync(string id, string nguoiDungId)
    {
        var tinNhan = DanhSach.FirstOrDefault(t => t.Id == id);
        tinNhan?.DanhSachCamXuc.RemoveAll(cx => cx.NguoiDungId == nguoiDungId);
        return Task.CompletedTask;
    }
}

using HaloChat.Api.Models;
using HaloChat.Api.Repositories;

namespace HaloChat.Api.Tests.Fakes;

public class NguoiDungGiaLap : INguoiDungRepository
{
    public List<NguoiDung> DanhSach { get; } = new();

    public Task<bool> TonTaiDinhDanhAsync(string dinhDanh) =>
        Task.FromResult(DanhSach.Any(nd => nd.TenTaiKhoan == dinhDanh || nd.Email == dinhDanh));

    public Task ThemMoiAsync(NguoiDung nguoiDung)
    {
        DanhSach.Add(nguoiDung);
        return Task.CompletedTask;
    }

    public Task<NguoiDung?> TimTheoTenTaiKhoanHoacEmailAsync(string tenDangNhap)
    {
        var ketQua = DanhSach.FirstOrDefault(nd =>
            string.Equals(nd.TenTaiKhoan, tenDangNhap, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(nd.Email, tenDangNhap, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(ketQua);
    }

    public Task<List<NguoiDung>> LayTatCaAsync() => Task.FromResult(DanhSach.ToList());

    public Task<NguoiDung?> TimTheoIdAsync(string id) =>
        Task.FromResult(DanhSach.FirstOrDefault(nd => nd.Id == id));

    public Task CapNhatCaiDatAsync(
        string id, bool choPhepTinNhanTuNguoiLa, bool hienThiTrangThaiHoatDong,
        bool choPhepThemVaoNhom, bool thongBaoTinNhanMoi, bool thongBaoLoiMoiKetBan, bool thongBaoNhom)
    {
        var nguoiDung = DanhSach.FirstOrDefault(nd => nd.Id == id);
        if (nguoiDung is not null)
        {
            nguoiDung.ChoPhepTinNhanTuNguoiLa = choPhepTinNhanTuNguoiLa;
            nguoiDung.HienThiTrangThaiHoatDong = hienThiTrangThaiHoatDong;
            nguoiDung.ChoPhepThemVaoNhom = choPhepThemVaoNhom;
            nguoiDung.ThongBaoTinNhanMoi = thongBaoTinNhanMoi;
            nguoiDung.ThongBaoLoiMoiKetBan = thongBaoLoiMoiKetBan;
            nguoiDung.ThongBaoNhom = thongBaoNhom;
        }
        return Task.CompletedTask;
    }

    public Task LuuOtpAsync(string id, string maOtpBam, DateTime hetHan, DateTime guiLucNao)
    {
        var nguoiDung = DanhSach.FirstOrDefault(nd => nd.Id == id);
        if (nguoiDung is not null)
        {
            nguoiDung.MaOtpBam = maOtpBam;
            nguoiDung.MaOtpHetHan = hetHan;
            nguoiDung.MaOtpGuiLucNao = guiLucNao;
            nguoiDung.SoLanThuSai = 0;
        }
        return Task.CompletedTask;
    }

    public Task XoaThoiGianGuiOtpAsync(string id)
    {
        var nguoiDung = DanhSach.FirstOrDefault(nd => nd.Id == id);
        if (nguoiDung is not null)
        {
            nguoiDung.MaOtpGuiLucNao = null;
        }
        return Task.CompletedTask;
    }

    public Task TangSoLanThuSaiOtpAsync(string id)
    {
        var nguoiDung = DanhSach.FirstOrDefault(nd => nd.Id == id);
        if (nguoiDung is not null)
        {
            nguoiDung.SoLanThuSai++;
        }
        return Task.CompletedTask;
    }

    public Task DatLaiMatKhauAsync(string id, string matKhauBamMoi, string saltMoi)
    {
        var nguoiDung = DanhSach.FirstOrDefault(nd => nd.Id == id);
        if (nguoiDung is not null)
        {
            nguoiDung.MatKhauBam = matKhauBamMoi;
            nguoiDung.Salt = saltMoi;
            nguoiDung.MaOtpBam = null;
            nguoiDung.MaOtpHetHan = null;
            nguoiDung.MaOtpGuiLucNao = null;
            nguoiDung.SoLanThuSai = 0;
        }
        return Task.CompletedTask;
    }

    public Task CapNhatTenHienThiAsync(string id, string tenHienThi)
    {
        var nguoiDung = DanhSach.FirstOrDefault(nd => nd.Id == id);
        if (nguoiDung is not null)
        {
            nguoiDung.TenHienThi = tenHienThi;
        }
        return Task.CompletedTask;
    }

    public Task CapNhatAnhDaiDienAsync(string id, string duongDanAnhDaiDien)
    {
        var nguoiDung = DanhSach.FirstOrDefault(nd => nd.Id == id);
        if (nguoiDung is not null)
        {
            nguoiDung.DuongDanAnhDaiDien = duongDanAnhDaiDien;
        }
        return Task.CompletedTask;
    }

    public Task DanhDauHoanTatHoSoAsync(string id)
    {
        var nguoiDung = DanhSach.FirstOrDefault(nd => nd.Id == id);
        if (nguoiDung is not null)
        {
            nguoiDung.DaXemHoanTatHoSo = true;
        }
        return Task.CompletedTask;
    }
}

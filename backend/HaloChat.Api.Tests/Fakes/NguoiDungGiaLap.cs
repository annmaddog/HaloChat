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
        var ketQua = DanhSach.FirstOrDefault(nd => nd.TenTaiKhoan == tenDangNhap || nd.Email == tenDangNhap);
        return Task.FromResult(ketQua);
    }

    public Task<List<NguoiDung>> LayTatCaAsync() => Task.FromResult(DanhSach.ToList());

    public Task<NguoiDung?> TimTheoIdAsync(string id) =>
        Task.FromResult(DanhSach.FirstOrDefault(nd => nd.Id == id));

    public Task CapNhatCaiDatAsync(string id, bool choPhepTinNhanTuNguoiLa, bool hienThiTrangThaiHoatDong)
    {
        var nguoiDung = DanhSach.FirstOrDefault(nd => nd.Id == id);
        if (nguoiDung is not null)
        {
            nguoiDung.ChoPhepTinNhanTuNguoiLa = choPhepTinNhanTuNguoiLa;
            nguoiDung.HienThiTrangThaiHoatDong = hienThiTrangThaiHoatDong;
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
}

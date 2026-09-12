using HaloChat.Api.Dto;
using HaloChat.Api.Models;
using HaloChat.Api.Repositories;

namespace HaloChat.Api.Services;

public class DichVuNguoiDung : IDichVuNguoiDung
{
    private readonly INguoiDungRepository _kho;
    private readonly IDichVuMatKhau _dichVuMatKhau;
    private readonly IDichVuJwt _dichVuJwt;

    public DichVuNguoiDung(INguoiDungRepository kho, IDichVuMatKhau dichVuMatKhau, IDichVuJwt dichVuJwt)
    {
        _kho = kho;
        _dichVuMatKhau = dichVuMatKhau;
        _dichVuJwt = dichVuJwt;
    }

    public async Task<KetQuaDangKyDto> DangKyTaiKhoan(string tenTaiKhoan, string email, string matKhau)
    {
        if (await _kho.TonTaiDinhDanhAsync(tenTaiKhoan))
        {
            return new KetQuaDangKyDto(false, "Tên tài khoản đã tồn tại.");
        }

        if (await _kho.TonTaiDinhDanhAsync(email))
        {
            return new KetQuaDangKyDto(false, "Email đã được sử dụng.");
        }

        var salt = _dichVuMatKhau.TaoSalt();
        var nguoiDungMoi = new NguoiDung
        {
            TenTaiKhoan = tenTaiKhoan,
            Email = email,
            Salt = salt,
            MatKhauBam = _dichVuMatKhau.BamMatKhau(matKhau, salt),
        };

        try
        {
            await _kho.ThemMoiAsync(nguoiDungMoi);
        }
        catch (TrungLapDinhDanhException)
        {
            return new KetQuaDangKyDto(false, "Tên tài khoản hoặc email đã tồn tại.");
        }

        return new KetQuaDangKyDto(true, "Đăng ký thành công.");
    }

    public async Task<string?> DangNhap(string tenDangNhap, string matKhau)
    {
        var nguoiDung = await _kho.TimTheoTenTaiKhoanHoacEmailAsync(tenDangNhap);
        if (nguoiDung is null)
        {
            return null;
        }

        if (!_dichVuMatKhau.KiemTraMatKhau(matKhau, nguoiDung.Salt, nguoiDung.MatKhauBam))
        {
            return null;
        }

        return _dichVuJwt.TaoJwt(nguoiDung);
    }

    public async Task<List<NguoiDungTomTatDto>> LayDanhSachNguoiDung(string idHienTai)
    {
        var tatCa = await _kho.LayTatCaAsync();
        return tatCa
            .Where(nd => nd.Id != idHienTai)
            .Select(nd => new NguoiDungTomTatDto(nd.Id, nd.TenTaiKhoan, nd.Email))
            .ToList();
    }

    public Task CapNhatCaiDatAsync(string idHienTai, bool choPhepTinNhanTuNguoiLa) =>
        _kho.CapNhatChoPhepTinNhanTuNguoiLaAsync(idHienTai, choPhepTinNhanTuNguoiLa);

    public async Task<HoSoCaNhanDto?> LayThongTinCaNhanAsync(string id)
    {
        var nguoiDung = await _kho.TimTheoIdAsync(id);
        return nguoiDung is null
            ? null
            : new HoSoCaNhanDto(nguoiDung.Id, nguoiDung.TenTaiKhoan, nguoiDung.Email, nguoiDung.ChoPhepTinNhanTuNguoiLa);
    }
}

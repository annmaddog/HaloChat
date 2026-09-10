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
        if (await _kho.TonTaiTenTaiKhoanAsync(tenTaiKhoan))
        {
            return new KetQuaDangKyDto(false, "Tên tài khoản đã tồn tại.");
        }

        if (await _kho.TonTaiEmailAsync(email))
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

        await _kho.ThemMoiAsync(nguoiDungMoi);
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
}

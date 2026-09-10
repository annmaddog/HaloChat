using HaloChat.Api.Dto;
using HaloChat.Api.Models;
using HaloChat.Api.Repositories;

namespace HaloChat.Api.Services;

public class DichVuNguoiDung : IDichVuNguoiDung
{
    private readonly INguoiDungRepository _kho;
    private readonly IDichVuMatKhau _dichVuMatKhau;

    public DichVuNguoiDung(INguoiDungRepository kho, IDichVuMatKhau dichVuMatKhau)
    {
        _kho = kho;
        _dichVuMatKhau = dichVuMatKhau;
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
}

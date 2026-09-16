using HaloChat.Api.Models;

namespace HaloChat.Api.Repositories;

public interface INguoiDungRepository
{
    Task<bool> TonTaiDinhDanhAsync(string dinhDanh);
    Task ThemMoiAsync(NguoiDung nguoiDung);
    Task<NguoiDung?> TimTheoTenTaiKhoanHoacEmailAsync(string tenDangNhap);
    Task<List<NguoiDung>> LayTatCaAsync();
    Task<NguoiDung?> TimTheoIdAsync(string id);
    Task CapNhatCaiDatAsync(
        string id, bool choPhepTinNhanTuNguoiLa, bool hienThiTrangThaiHoatDong,
        bool choPhepThemVaoNhom, bool thongBaoTinNhanMoi, bool thongBaoLoiMoiKetBan, bool thongBaoNhom);
    Task LuuOtpAsync(string id, string maOtpBam, DateTime hetHan, DateTime guiLucNao);
    Task XoaThoiGianGuiOtpAsync(string id);
    Task TangSoLanThuSaiOtpAsync(string id);
    Task DatLaiMatKhauAsync(string id, string matKhauBamMoi, string saltMoi);
    Task CapNhatTenHienThiAsync(string id, string tenHienThi);
    Task CapNhatAnhDaiDienAsync(string id, string duongDanAnhDaiDien);
    Task DanhDauHoanTatHoSoAsync(string id);
}

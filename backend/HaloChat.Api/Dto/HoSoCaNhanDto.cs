namespace HaloChat.Api.Dto;

public record HoSoCaNhanDto(
    string Id, string TenTaiKhoan, string Email,
    bool ChoPhepTinNhanTuNguoiLa, bool HienThiTrangThaiHoatDong,
    bool ChoPhepThemVaoNhom, bool ThongBaoTinNhanMoi, bool ThongBaoLoiMoiKetBan, bool ThongBaoNhom,
    string TenHienThi, string? DuongDanAnhDaiDien, bool DaXemHoanTatHoSo);

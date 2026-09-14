namespace HaloChat.Api.Dto;

public record CapNhatCaiDatRequest(
    bool ChoPhepTinNhanTuNguoiLa, bool HienThiTrangThaiHoatDong,
    bool ChoPhepThemVaoNhom, bool ThongBaoTinNhanMoi, bool ThongBaoLoiMoiKetBan, bool ThongBaoNhom);

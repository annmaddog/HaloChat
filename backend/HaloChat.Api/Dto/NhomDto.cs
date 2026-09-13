namespace HaloChat.Api.Dto;

public record NhomDto(
    string Id, string TenNhom, string? MoTa, string? DuongDanAnhDaiDien,
    string NguoiTaoId, List<NguoiDungTomTatDto> ThanhVien, DateTime ThoiGianTao);

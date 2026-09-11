namespace HaloChat.Api.Dto;

public record TinNhanDto(
    string Id,
    string NguoiGuiId,
    string? NguoiNhanId,
    string LoaiTinNhan,
    string NoiDungTinNhan,
    string? DuongDanFile,
    string? TenFileGoc,
    long? KichThuocFile,
    string? LoaiFile,
    bool DaDoc,
    DateTime ThoiGianTao);

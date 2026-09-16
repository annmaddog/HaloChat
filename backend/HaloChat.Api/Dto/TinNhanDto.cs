namespace HaloChat.Api.Dto;

public record TinNhanDto(
    string Id,
    string NguoiGuiId,
    string? NguoiNhanId,
    string? NhomId,
    string LoaiTinNhan,
    string NoiDungTinNhan,
    string? DuongDanFile,
    string? TenFileGoc,
    long? KichThuocFile,
    string? LoaiFile,
    bool DaDoc,
    bool DaNhan,
    DateTime ThoiGianTao,
    TraLoiThongTinDto? TraLoi,
    bool DaThuHoi,
    bool DaGhim,
    DateTime? ThoiGianGhim,
    List<CamXucDto> DanhSachCamXuc);

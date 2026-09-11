using HaloChat.Api.Dto;

namespace HaloChat.Api.Services;

public interface IDichVuTinNhan
{
    Task<TinNhanDto> GuiTinNhanAsync(
        string nguoiGuiId, string nguoiNhanId, string loaiTinNhan, string noiDungTinNhan,
        string? duongDanFile, string? tenFileGoc, long? kichThuocFile, string? loaiFile);

    Task<List<TinNhanDto>> LayLichSuAsync(string nguoiHienTaiId, string nguoiKiaId, string? truocId, int soLuong);

    Task DanhDauDaDocAsync(string nguoiHienTaiId, string nguoiGuiId);
}

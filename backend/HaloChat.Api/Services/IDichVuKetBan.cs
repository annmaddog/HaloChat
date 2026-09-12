using HaloChat.Api.Dto;

namespace HaloChat.Api.Services;

public interface IDichVuKetBan
{
    Task<LoiMoiKetBanDto> GuiLoiMoiAsync(string nguoiGuiId, string nguoiNhanId);
    Task<LoiMoiKetBanDto> ChapNhanAsync(string nguoiHienTaiId, string idLoiMoi);
    Task TuChoiAsync(string nguoiHienTaiId, string idLoiMoi);
    Task<List<NguoiDungTomTatDto>> LayBanBeAsync(string nguoiDungId);
    Task<List<LoiMoiKetBanDto>> LayLoiMoiDenAsync(string nguoiDungId);
    Task<List<LoiMoiKetBanDto>> LayLoiMoiGuiAsync(string nguoiDungId);
}

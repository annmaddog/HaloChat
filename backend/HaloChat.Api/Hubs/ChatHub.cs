using System.IdentityModel.Tokens.Jwt;
using HaloChat.Api.Dto;
using HaloChat.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace HaloChat.Api.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IDichVuTinNhan _dichVuTinNhan;

    public ChatHub(IDichVuTinNhan dichVuTinNhan)
    {
        _dichVuTinNhan = dichVuTinNhan;
    }

    private string NguoiDungHienTaiId =>
        Context.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
        ?? throw new HubException("Không xác định được người dùng hiện tại.");

    public async Task<TinNhanDto> GuiTinNhan(
        string nguoiNhanId, string loaiTinNhan, string noiDungTinNhan,
        string? duongDanFile, string? tenFileGoc, long? kichThuocFile, string? loaiFile)
    {
        try
        {
            var tinNhan = await _dichVuTinNhan.GuiTinNhanAsync(
                NguoiDungHienTaiId, nguoiNhanId, loaiTinNhan, noiDungTinNhan,
                duongDanFile, tenFileGoc, kichThuocFile, loaiFile);

            await Clients.User(nguoiNhanId).SendAsync("NhanTinNhan", tinNhan);
            return tinNhan;
        }
        catch (NguoiNhanKhongTonTaiException loi)
        {
            throw new HubException(loi.Message);
        }
        catch (TinNhanKhongHopLeException loi)
        {
            throw new HubException(loi.Message);
        }
    }

    public Task DanhDauDaDoc(string nguoiGuiId) =>
        _dichVuTinNhan.DanhDauDaDocAsync(NguoiDungHienTaiId, nguoiGuiId);
}

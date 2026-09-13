using System.IdentityModel.Tokens.Jwt;
using HaloChat.Api.Dto;
using HaloChat.Api.Repositories;
using HaloChat.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace HaloChat.Api.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IDichVuTinNhan _dichVuTinNhan;
    private readonly IDichVuNhom _dichVuNhom;
    private readonly IQuanLyKetNoiChat _quanLyKetNoi;
    private readonly INguoiDungRepository _khoNguoiDung;
    private readonly ILoiMoiKetBanRepository _khoLoiMoiKetBan;

    public ChatHub(
        IDichVuTinNhan dichVuTinNhan, IDichVuNhom dichVuNhom, IQuanLyKetNoiChat quanLyKetNoi,
        INguoiDungRepository khoNguoiDung, ILoiMoiKetBanRepository khoLoiMoiKetBan)
    {
        _dichVuTinNhan = dichVuTinNhan;
        _dichVuNhom = dichVuNhom;
        _quanLyKetNoi = quanLyKetNoi;
        _khoNguoiDung = khoNguoiDung;
        _khoLoiMoiKetBan = khoLoiMoiKetBan;
    }

    private string NguoiDungHienTaiId =>
        Context.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
        ?? throw new HubException("Không xác định được người dùng hiện tại.");

    public override async Task OnConnectedAsync()
    {
        var userId = NguoiDungHienTaiId;

        var cacNhom = await _dichVuNhom.LayDanhSachAsync(userId);
        foreach (var nhom in cacNhom)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "nhom-" + nhom.Id);
        }

        var laOnlineMoi = _quanLyKetNoi.ThemKetNoi(userId, Context.ConnectionId);
        if (laOnlineMoi)
        {
            await BaoTrangThaiHoatDongThayDoiAsync(userId, true);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = NguoiDungHienTaiId;
        var vuaOffline = _quanLyKetNoi.XoaKetNoi(userId, Context.ConnectionId);
        if (vuaOffline)
        {
            await BaoTrangThaiHoatDongThayDoiAsync(userId, false);
        }

        await base.OnDisconnectedAsync(exception);
    }

    private async Task BaoTrangThaiHoatDongThayDoiAsync(string userId, bool online)
    {
        var nguoiDung = await _khoNguoiDung.TimTheoIdAsync(userId);
        if (nguoiDung is null || !nguoiDung.HienThiTrangThaiHoatDong)
        {
            return;
        }

        var banBe = await _khoLoiMoiKetBan.LayBanBeAsync(userId);
        foreach (var loiMoi in banBe)
        {
            var idBan = loiMoi.NguoiGuiId == userId ? loiMoi.NguoiNhanId : loiMoi.NguoiGuiId;
            await Clients.User(idBan).SendAsync("TrangThaiHoatDongThayDoi", userId, online);
        }
    }

    public async Task<TinNhanDto> GuiTinNhan(
        string? nguoiNhanId, string? nhomId, string loaiTinNhan, string noiDungTinNhan,
        string? duongDanFile, string? tenFileGoc, long? kichThuocFile, string? loaiFile)
    {
        try
        {
            var tinNhan = await _dichVuTinNhan.GuiTinNhanAsync(
                NguoiDungHienTaiId, nguoiNhanId, nhomId, loaiTinNhan, noiDungTinNhan,
                duongDanFile, tenFileGoc, kichThuocFile, loaiFile);

            if (nhomId is not null)
            {
                await Clients.OthersInGroup("nhom-" + nhomId).SendAsync("NhanTinNhan", tinNhan);
            }
            else
            {
                await Clients.User(nguoiNhanId!).SendAsync("NhanTinNhan", tinNhan);
            }

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
        catch (NhomKhongTonTaiException loi)
        {
            throw new HubException(loi.Message);
        }
        catch (KhongPhaiThanhVienNhomException loi)
        {
            throw new HubException(loi.Message);
        }
    }

    public Task DanhDauDaDoc(string? nguoiGuiId, string? nhomId) =>
        nhomId is not null
            ? _dichVuTinNhan.DanhDauDaDocNhomAsync(NguoiDungHienTaiId, nhomId)
            : _dichVuTinNhan.DanhDauDaDocAsync(NguoiDungHienTaiId, nguoiGuiId!);
}

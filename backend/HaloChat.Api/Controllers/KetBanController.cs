using System.IdentityModel.Tokens.Jwt;
using HaloChat.Api.Hubs;
using HaloChat.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace HaloChat.Api.Controllers;

[ApiController]
[Route("api/ketban")]
[Authorize]
public class KetBanController : ControllerBase
{
    private readonly IDichVuKetBan _dichVu;
    private readonly IHubContext<ChatHub> _hub;

    public KetBanController(IDichVuKetBan dichVu, IHubContext<ChatHub> hub)
    {
        _dichVu = dichVu;
        _hub = hub;
    }

    private string? IdHienTai => User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

    [HttpPost("loi-moi/{nguoiNhanId}")]
    public async Task<IActionResult> GuiLoiMoi(string nguoiNhanId)
    {
        if (IdHienTai is null)
        {
            return Unauthorized();
        }

        try
        {
            var loiMoi = await _dichVu.GuiLoiMoiAsync(IdHienTai, nguoiNhanId);
            await _hub.Clients.User(nguoiNhanId).SendAsync("NhanLoiMoiKetBan", loiMoi);
            return Ok(loiMoi);
        }
        catch (KhongTheTuKetBanException loi)
        {
            return BadRequest(new { thongBao = loi.Message });
        }
        catch (NguoiDuocMoiKhongTonTaiException loi)
        {
            return NotFound(new { thongBao = loi.Message });
        }
        catch (LoiMoiKetBanDaTonTaiException loi)
        {
            return Conflict(new { thongBao = loi.Message });
        }
    }

    [HttpPost("{id}/chap-nhan")]
    public async Task<IActionResult> ChapNhan(string id)
    {
        if (IdHienTai is null)
        {
            return Unauthorized();
        }

        try
        {
            var loiMoi = await _dichVu.ChapNhanAsync(IdHienTai, id);
            await _hub.Clients.User(loiMoi.NguoiGui.Id).SendAsync("LoiMoiKetBanDuocChapNhan", loiMoi.NguoiNhan);
            return Ok(loiMoi);
        }
        catch (LoiMoiKetBanKhongTonTaiException loi)
        {
            return NotFound(new { thongBao = loi.Message });
        }
        catch (KhongCoQuyenXuLyLoiMoiException)
        {
            return Forbid();
        }
        catch (LoiMoiKetBanDaXuLyException loi)
        {
            return Conflict(new { thongBao = loi.Message });
        }
    }

    [HttpPost("{id}/tu-choi")]
    public async Task<IActionResult> TuChoi(string id)
    {
        if (IdHienTai is null)
        {
            return Unauthorized();
        }

        try
        {
            await _dichVu.TuChoiAsync(IdHienTai, id);
            return Ok(new { thongBao = "Đã từ chối lời mời." });
        }
        catch (LoiMoiKetBanKhongTonTaiException loi)
        {
            return NotFound(new { thongBao = loi.Message });
        }
        catch (KhongCoQuyenXuLyLoiMoiException)
        {
            return Forbid();
        }
        catch (LoiMoiKetBanDaXuLyException loi)
        {
            return Conflict(new { thongBao = loi.Message });
        }
    }

    [HttpGet("ban-be")]
    public async Task<IActionResult> LayBanBe()
    {
        if (IdHienTai is null)
        {
            return Unauthorized();
        }

        return Ok(await _dichVu.LayBanBeAsync(IdHienTai));
    }

    [HttpGet("loi-moi-den")]
    public async Task<IActionResult> LayLoiMoiDen()
    {
        if (IdHienTai is null)
        {
            return Unauthorized();
        }

        return Ok(await _dichVu.LayLoiMoiDenAsync(IdHienTai));
    }

    [HttpGet("loi-moi-gui")]
    public async Task<IActionResult> LayLoiMoiGui()
    {
        if (IdHienTai is null)
        {
            return Unauthorized();
        }

        return Ok(await _dichVu.LayLoiMoiGuiAsync(IdHienTai));
    }
}

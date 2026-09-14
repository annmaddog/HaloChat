using System.IdentityModel.Tokens.Jwt;
using HaloChat.Api.Dto;
using HaloChat.Api.Hubs;
using HaloChat.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace HaloChat.Api.Controllers;

[ApiController]
[Route("api/nhom")]
[Authorize]
public class NhomController : ControllerBase
{
    private readonly IDichVuNhom _dichVu;
    private readonly IHubContext<ChatHub> _hub;
    private readonly IQuanLyKetNoiChat _quanLyKetNoi;

    public NhomController(IDichVuNhom dichVu, IHubContext<ChatHub> hub, IQuanLyKetNoiChat quanLyKetNoi)
    {
        _dichVu = dichVu;
        _hub = hub;
        _quanLyKetNoi = quanLyKetNoi;
    }

    private string? IdHienTai => User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

    [HttpPost]
    public async Task<IActionResult> TaoNhom([FromBody] TaoNhomRequest yeuCau)
    {
        if (IdHienTai is null)
        {
            return Unauthorized();
        }

        try
        {
            var nhom = await _dichVu.TaoNhomAsync(IdHienTai, yeuCau.TenNhom, yeuCau.MoTa, yeuCau.DuongDanAnhDaiDien, yeuCau.ThanhVienIds);

            foreach (var thanhVien in nhom.ThanhVien)
            {
                foreach (var connId in _quanLyKetNoi.LayConnectionIds(thanhVien.Id))
                {
                    await _hub.Groups.AddToGroupAsync(connId, "nhom-" + nhom.Id);
                }

                if (thanhVien.Id != IdHienTai)
                {
                    await _hub.Clients.User(thanhVien.Id).SendAsync("DuocThemVaoNhom", nhom);
                }
            }

            return Ok(nhom);
        }
        catch (ThanhVienKhongTonTaiException loi)
        {
            return BadRequest(new { thongBao = loi.Message });
        }
        catch (KhongChoPhepThemVaoNhomException loi)
        {
            return BadRequest(new { thongBao = loi.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> LayDanhSach()
    {
        if (IdHienTai is null)
        {
            return Unauthorized();
        }

        return Ok(await _dichVu.LayDanhSachAsync(IdHienTai));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> LayChiTiet(string id)
    {
        if (IdHienTai is null)
        {
            return Unauthorized();
        }

        try
        {
            return Ok(await _dichVu.LayChiTietAsync(IdHienTai, id));
        }
        catch (NhomKhongTonTaiException loi)
        {
            return NotFound(new { thongBao = loi.Message });
        }
        catch (KhongPhaiThanhVienNhomException loi)
        {
            return StatusCode(403, new { thongBao = loi.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> CapNhat(string id, [FromBody] CapNhatNhomRequest yeuCau)
    {
        if (IdHienTai is null)
        {
            return Unauthorized();
        }

        try
        {
            return Ok(await _dichVu.CapNhatAsync(IdHienTai, id, yeuCau.TenNhom, yeuCau.MoTa, yeuCau.DuongDanAnhDaiDien));
        }
        catch (NhomKhongTonTaiException loi)
        {
            return NotFound(new { thongBao = loi.Message });
        }
        catch (KhongCoQuyenQuanTriNhomException loi)
        {
            return StatusCode(403, new { thongBao = loi.Message });
        }
    }

    [HttpPost("{id}/thanh-vien")]
    public async Task<IActionResult> ThemThanhVien(string id, [FromBody] ThemThanhVienRequest yeuCau)
    {
        if (IdHienTai is null)
        {
            return Unauthorized();
        }

        try
        {
            var nhom = await _dichVu.ThemThanhVienAsync(IdHienTai, id, yeuCau.ThanhVienId);

            foreach (var connId in _quanLyKetNoi.LayConnectionIds(yeuCau.ThanhVienId))
            {
                await _hub.Groups.AddToGroupAsync(connId, "nhom-" + id);
            }
            await _hub.Clients.User(yeuCau.ThanhVienId).SendAsync("DuocThemVaoNhom", nhom);
            await _hub.Clients.Group("nhom-" + id).SendAsync("NhomDaCapNhat", nhom);

            return Ok(nhom);
        }
        catch (NhomKhongTonTaiException loi)
        {
            return NotFound(new { thongBao = loi.Message });
        }
        catch (KhongCoQuyenQuanTriNhomException loi)
        {
            return StatusCode(403, new { thongBao = loi.Message });
        }
        catch (ThanhVienKhongTonTaiException loi)
        {
            return BadRequest(new { thongBao = loi.Message });
        }
        catch (KhongChoPhepThemVaoNhomException loi)
        {
            return BadRequest(new { thongBao = loi.Message });
        }
    }

    [HttpDelete("{id}/thanh-vien/{userId}")]
    public async Task<IActionResult> XoaThanhVien(string id, string userId)
    {
        if (IdHienTai is null)
        {
            return Unauthorized();
        }

        try
        {
            var nhom = await _dichVu.XoaThanhVienAsync(IdHienTai, id, userId);

            foreach (var connId in _quanLyKetNoi.LayConnectionIds(userId))
            {
                await _hub.Groups.RemoveFromGroupAsync(connId, "nhom-" + id);
            }
            await _hub.Clients.User(userId).SendAsync("BiXoaKhoiNhom", id);
            await _hub.Clients.Group("nhom-" + id).SendAsync("NhomDaCapNhat", nhom);

            return Ok(nhom);
        }
        catch (NhomKhongTonTaiException loi)
        {
            return NotFound(new { thongBao = loi.Message });
        }
        catch (KhongCoQuyenQuanTriNhomException loi)
        {
            return StatusCode(403, new { thongBao = loi.Message });
        }
        catch (KhongTheXoaNguoiTaoException loi)
        {
            return BadRequest(new { thongBao = loi.Message });
        }
    }

    [HttpPost("{id}/roi-nhom")]
    public async Task<IActionResult> RoiNhom(string id)
    {
        if (IdHienTai is null)
        {
            return Unauthorized();
        }

        try
        {
            var ketQua = await _dichVu.RoiNhomAsync(IdHienTai, id);

            if (ketQua.DaGiaiTan)
            {
                foreach (var thanhVienId in ketQua.ThanhVienConLai)
                {
                    await _hub.Clients.User(thanhVienId).SendAsync("NhomDaGiaiTan", id);
                }
            }

            return Ok(new { thongBao = ketQua.DaGiaiTan ? "Nhóm đã được giải tán." : "Đã rời nhóm." });
        }
        catch (NhomKhongTonTaiException loi)
        {
            return NotFound(new { thongBao = loi.Message });
        }
        catch (KhongPhaiThanhVienNhomException loi)
        {
            return StatusCode(403, new { thongBao = loi.Message });
        }
    }

    [HttpGet("so-tin-chua-doc")]
    public async Task<IActionResult> LaySoTinChuaDoc()
    {
        if (IdHienTai is null)
        {
            return Unauthorized();
        }

        var soTinChuaDoc = await _dichVu.DemTongChuaDocAsync(IdHienTai);
        return Ok(new { soTinChuaDoc });
    }
}

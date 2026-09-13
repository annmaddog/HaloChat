using System.IdentityModel.Tokens.Jwt;
using HaloChat.Api.Dto;
using HaloChat.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HaloChat.Api.Controllers;

[ApiController]
[Route("api/nhom")]
[Authorize]
public class NhomController : ControllerBase
{
    private readonly IDichVuNhom _dichVu;

    public NhomController(IDichVuNhom dichVu)
    {
        _dichVu = dichVu;
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
            return Ok(nhom);
        }
        catch (ThanhVienKhongTonTaiException loi)
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
}

using HaloChat.Api.Dto;
using HaloChat.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace HaloChat.Api.Controllers;

[ApiController]
[Route("api/nguoidung")]
public class NguoiDungController : ControllerBase
{
    private readonly IDichVuNguoiDung _dichVu;

    public NguoiDungController(IDichVuNguoiDung dichVu)
    {
        _dichVu = dichVu;
    }

    [HttpPost("dang-ky")]
    public async Task<IActionResult> DangKy([FromBody] DangKyTaiKhoanRequest yeuCau)
    {
        var ketQua = await _dichVu.DangKyTaiKhoan(yeuCau.TenTaiKhoan, yeuCau.Email, yeuCau.MatKhau);
        if (!ketQua.ThanhCong)
        {
            return Conflict(new { thongBao = ketQua.ThongBao });
        }

        return Ok(new { thongBao = ketQua.ThongBao });
    }
}

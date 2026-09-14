using System.IdentityModel.Tokens.Jwt;
using HaloChat.Api.Dto;
using HaloChat.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HaloChat.Api.Controllers;

[ApiController]
[Route("api/nguoidung")]
public class NguoiDungController : ControllerBase
{
    private readonly IDichVuNguoiDung _dichVu;
    private readonly IQuanLyKetNoiChat _quanLyKetNoi;

    public NguoiDungController(IDichVuNguoiDung dichVu, IQuanLyKetNoiChat quanLyKetNoi)
    {
        _dichVu = dichVu;
        _quanLyKetNoi = quanLyKetNoi;
    }

    private string? IdHienTai => User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

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

    [HttpPost("dang-nhap")]
    public async Task<IActionResult> DangNhap([FromBody] DangNhapRequest yeuCau)
    {
        var token = await _dichVu.DangNhap(yeuCau.TenDangNhap, yeuCau.MatKhau);
        if (token is null)
        {
            return Unauthorized(new { thongBao = "Sai tên đăng nhập hoặc mật khẩu." });
        }

        return Ok(new DangNhapResponse(token));
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> LayDanhSach()
    {
        var idHienTai = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (idHienTai is null)
        {
            return Unauthorized();
        }

        var danhSach = await _dichVu.LayDanhSachNguoiDung(idHienTai);
        return Ok(danhSach);
    }

    [HttpGet("toi")]
    [Authorize]
    public async Task<IActionResult> LayThongTinCaNhan()
    {
        var idHienTai = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (idHienTai is null)
        {
            return Unauthorized();
        }

        var hoSo = await _dichVu.LayThongTinCaNhanAsync(idHienTai);
        return hoSo is null ? NotFound() : Ok(hoSo);
    }

    [HttpPut("cai-dat")]
    [Authorize]
    public async Task<IActionResult> CapNhatCaiDat([FromBody] CapNhatCaiDatRequest yeuCau)
    {
        var idHienTai = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (idHienTai is null)
        {
            return Unauthorized();
        }

        await _dichVu.CapNhatCaiDatAsync(
            idHienTai, yeuCau.ChoPhepTinNhanTuNguoiLa, yeuCau.HienThiTrangThaiHoatDong,
            yeuCau.ChoPhepThemVaoNhom, yeuCau.ThongBaoTinNhanMoi, yeuCau.ThongBaoLoiMoiKetBan, yeuCau.ThongBaoNhom);
        return Ok(new { thongBao = "Đã cập nhật cài đặt." });
    }

    [HttpGet("trang-thai")]
    [Authorize]
    public async Task<IActionResult> LayTrangThaiHoatDong([FromQuery] string ids)
    {
        if (IdHienTai is null)
        {
            return Unauthorized();
        }

        var ketQua = new Dictionary<string, bool>();
        foreach (var id in ids.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var hoSo = await _dichVu.LayThongTinCaNhanAsync(id);
            ketQua[id] = hoSo is not null && hoSo.HienThiTrangThaiHoatDong && _quanLyKetNoi.DangOnline(id);
        }

        return Ok(ketQua);
    }

    [HttpPost("quen-mat-khau")]
    public async Task<IActionResult> QuenMatKhau([FromBody] QuenMatKhauRequest yeuCau)
    {
        await _dichVu.YeuCauOtpDatLaiMatKhauAsync(yeuCau.Email);
        return Ok(new { thongBao = "Nếu email tồn tại trong hệ thống, mã OTP đã được gửi." });
    }

    [HttpPost("dat-lai-mat-khau")]
    public async Task<IActionResult> DatLaiMatKhau([FromBody] DatLaiMatKhauRequest yeuCau)
    {
        var ketQua = await _dichVu.DatLaiMatKhauAsync(yeuCau.Email, yeuCau.MaOtp, yeuCau.MatKhauMoi);
        if (!ketQua.ThanhCong)
        {
            return BadRequest(new { thongBao = ketQua.ThongBao });
        }

        return Ok(new { thongBao = ketQua.ThongBao });
    }
}

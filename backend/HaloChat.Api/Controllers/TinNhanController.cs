using System.IdentityModel.Tokens.Jwt;
using HaloChat.Api.Dto;
using HaloChat.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HaloChat.Api.Controllers;

[ApiController]
[Route("api/tinnhan")]
[Authorize]
public class TinNhanController : ControllerBase
{
    // Allow-list theo spec §7: chấp nhận đúng các phần mở rộng này, mọi thứ
    // khác (kể cả file thực thi) bị từ chối — đơn giản và an toàn hơn một
    // block-list liệt kê phần mở rộng nguy hiểm.
    private static readonly Dictionary<string, string> LoaiAnhChoPhep = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".gif"] = "image/gif",
        [".webp"] = "image/webp",
    };

    private static readonly Dictionary<string, string> LoaiFileChoPhep = new(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = "application/pdf",
        [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        [".zip"] = "application/zip",
    };

    private const long GioiHanAnhBytes = 5L * 1024 * 1024;
    private const long GioiHanFileBytes = 20L * 1024 * 1024;

    private readonly IDichVuTinNhan _dichVuTinNhan;
    private readonly IWebHostEnvironment _moiTruong;

    public TinNhanController(IDichVuTinNhan dichVuTinNhan, IWebHostEnvironment moiTruong)
    {
        _dichVuTinNhan = dichVuTinNhan;
        _moiTruong = moiTruong;
    }

    private string? IdHienTai => User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

    [HttpGet("nguoi-dung/{id}")]
    public async Task<IActionResult> LayLichSu(string id, [FromQuery] string? truoc, [FromQuery] int soLuong = 30)
    {
        if (IdHienTai is null)
        {
            return Unauthorized();
        }

        var soLuongThucTe = Math.Clamp(soLuong, 1, 100);
        var lichSu = await _dichVuTinNhan.LayLichSuAsync(IdHienTai, id, truoc, soLuongThucTe);
        return Ok(lichSu);
    }

    [HttpPost("upload")]
    [RequestSizeLimit(GioiHanFileBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = GioiHanFileBytes)]
    public async Task<IActionResult> TaiLen(IFormFile tep)
    {
        if (tep.Length == 0)
        {
            return BadRequest(new { thongBao = "File rỗng." });
        }

        var phanMoRong = Path.GetExtension(tep.FileName);
        var laAnh = LoaiAnhChoPhep.TryGetValue(phanMoRong, out var mimeAnh);
        string? mimeFile = null;
        var laFile = !laAnh && LoaiFileChoPhep.TryGetValue(phanMoRong, out mimeFile);

        if (!laAnh && !laFile)
        {
            return BadRequest(new { thongBao = "Định dạng file không được hỗ trợ." });
        }

        var gioiHan = laAnh ? GioiHanAnhBytes : GioiHanFileBytes;
        if (tep.Length > gioiHan)
        {
            return BadRequest(new { thongBao = $"File vượt quá giới hạn {gioiHan / 1024 / 1024}MB." });
        }

        var thuMucTaiLen = Path.Combine(_moiTruong.ContentRootPath, "uploads");
        Directory.CreateDirectory(thuMucTaiLen);
        var tenFileLuu = $"{Guid.NewGuid()}{phanMoRong}";
        var duongDanDayDu = Path.Combine(thuMucTaiLen, tenFileLuu);

        // Stream thẳng ra đĩa (spec §8) — CopyToAsync không tạo byte[] trung
        // gian trong bộ nhớ ứng dụng.
        await using (var luongGhi = new FileStream(duongDanDayDu, FileMode.Create))
        {
            await tep.CopyToAsync(luongGhi);
        }

        return Ok(new TepTinDaTaiLenDto($"/uploads/{tenFileLuu}", tep.FileName, tep.Length, (laAnh ? mimeAnh : mimeFile)!));
    }
}

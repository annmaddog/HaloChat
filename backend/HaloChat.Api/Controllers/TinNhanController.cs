using System.IdentityModel.Tokens.Jwt;
using HaloChat.Api.Dto;
using HaloChat.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;

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
    private readonly IDichVuLuuTruFile _dichVuLuuTruFile;

    public TinNhanController(IDichVuTinNhan dichVuTinNhan, IDichVuLuuTruFile dichVuLuuTruFile)
    {
        _dichVuTinNhan = dichVuTinNhan;
        _dichVuLuuTruFile = dichVuLuuTruFile;
    }

    private string? IdHienTai => User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

    [HttpGet("nguoi-dung/{id}")]
    public async Task<IActionResult> LayLichSu(string id, [FromQuery] string? truoc, [FromQuery] int soLuong = 30)
    {
        if (IdHienTai is null)
        {
            return Unauthorized();
        }

        if (!ObjectId.TryParse(id, out _))
        {
            return BadRequest(new { thongBao = "Id người dùng không hợp lệ." });
        }

        if (truoc is not null && !ObjectId.TryParse(truoc, out _))
        {
            return BadRequest(new { thongBao = "Tham số truoc không hợp lệ." });
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

        var loaiMimeThucTe = (laAnh ? mimeAnh : mimeFile)!;
        await using var luongDoc = tep.OpenReadStream();
        var id = await _dichVuLuuTruFile.LuuAsync(luongDoc, tep.FileName, loaiMimeThucTe);

        return Ok(new TepTinDaTaiLenDto($"/api/tinnhan/file/{id}", tep.FileName, tep.Length, loaiMimeThucTe));
    }

    // Không [Authorize] — thẻ <img>/<a> trên trình duyệt không tự đính kèm
    // được header Authorization. Id là ObjectId ngẫu nhiên của GridFS nên
    // chỉ ai có đúng đường link (nhận qua tin nhắn) mới xem được — cùng mô
    // hình bảo mật "biết link mới xem được" như file tĩnh trước đây.
    [HttpGet("file/{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> LayFile(string id)
    {
        var ketQua = await _dichVuLuuTruFile.LayAsync(id);
        if (ketQua is null)
        {
            return NotFound();
        }

        return File(ketQua.NoiDung, ketQua.LoaiMime, ketQua.TenFile);
    }

    [HttpGet("nhom/{id}")]
    public async Task<IActionResult> LayLichSuNhom(string id, [FromQuery] string? truoc, [FromQuery] int soLuong = 30)
    {
        if (IdHienTai is null)
        {
            return Unauthorized();
        }

        if (!ObjectId.TryParse(id, out _))
        {
            return BadRequest(new { thongBao = "Id nhóm không hợp lệ." });
        }

        try
        {
            var soLuongThucTe = Math.Clamp(soLuong, 1, 100);
            var lichSu = await _dichVuTinNhan.LayLichSuNhomAsync(IdHienTai, id, truoc, soLuongThucTe);
            return Ok(lichSu);
        }
        catch (HaloChat.Api.Services.NhomKhongTonTaiException loi)
        {
            return NotFound(new { thongBao = loi.Message });
        }
        catch (HaloChat.Api.Services.KhongPhaiThanhVienNhomException loi)
        {
            return StatusCode(403, new { thongBao = loi.Message });
        }
    }

    [HttpGet("hoi-thoai")]
    public async Task<IActionResult> LayDanhSachHoiThoai()
    {
        if (IdHienTai is null)
        {
            return Unauthorized();
        }

        return Ok(await _dichVuTinNhan.LayDanhSachHoiThoaiAsync(IdHienTai));
    }

    [HttpPost("{id}/an")]
    public async Task<IActionResult> An(string id)
    {
        if (IdHienTai is null)
        {
            return Unauthorized();
        }

        if (!ObjectId.TryParse(id, out _))
        {
            return BadRequest(new { thongBao = "Id tin nhắn không hợp lệ." });
        }

        try
        {
            await _dichVuTinNhan.AnAsync(IdHienTai, id);
            return Ok(new { thongBao = "Đã ẩn tin nhắn." });
        }
        catch (TinNhanKhongTonTaiException loi) { return NotFound(new { thongBao = loi.Message }); }
        catch (KhongCoQuyenTrenTinNhanException loi) { return StatusCode(403, new { thongBao = loi.Message }); }
        catch (NhomKhongTonTaiException loi) { return NotFound(new { thongBao = loi.Message }); }
        catch (KhongPhaiThanhVienNhomException loi) { return StatusCode(403, new { thongBao = loi.Message }); }
    }

    [HttpGet("nguoi-dung/{id}/ghim")]
    public async Task<IActionResult> LayTinDaGhimTheoNguoiDung(string id)
    {
        if (IdHienTai is null)
        {
            return Unauthorized();
        }

        if (!ObjectId.TryParse(id, out _))
        {
            return BadRequest(new { thongBao = "Id người dùng không hợp lệ." });
        }

        return Ok(await _dichVuTinNhan.LayTinDaGhimTheoNguoiDungAsync(IdHienTai, id));
    }

    [HttpGet("nhom/{id}/ghim")]
    public async Task<IActionResult> LayTinDaGhimTheoNhom(string id)
    {
        if (IdHienTai is null)
        {
            return Unauthorized();
        }

        if (!ObjectId.TryParse(id, out _))
        {
            return BadRequest(new { thongBao = "Id nhóm không hợp lệ." });
        }

        try
        {
            return Ok(await _dichVuTinNhan.LayTinDaGhimTheoNhomAsync(IdHienTai, id));
        }
        catch (NhomKhongTonTaiException loi) { return NotFound(new { thongBao = loi.Message }); }
        catch (KhongPhaiThanhVienNhomException loi) { return StatusCode(403, new { thongBao = loi.Message }); }
    }

    [HttpGet("nguoi-dung/{id}/media")]
    public async Task<IActionResult> LayMediaTheoNguoiDung(string id)
    {
        if (IdHienTai is null)
        {
            return Unauthorized();
        }

        if (!ObjectId.TryParse(id, out _))
        {
            return BadRequest(new { thongBao = "Id người dùng không hợp lệ." });
        }

        return Ok(await _dichVuTinNhan.LayMediaTheoNguoiDungAsync(IdHienTai, id));
    }

    [HttpGet("nhom/{id}/media")]
    public async Task<IActionResult> LayMediaTheoNhom(string id)
    {
        if (IdHienTai is null)
        {
            return Unauthorized();
        }

        if (!ObjectId.TryParse(id, out _))
        {
            return BadRequest(new { thongBao = "Id nhóm không hợp lệ." });
        }

        try
        {
            return Ok(await _dichVuTinNhan.LayMediaTheoNhomAsync(IdHienTai, id));
        }
        catch (NhomKhongTonTaiException loi) { return NotFound(new { thongBao = loi.Message }); }
        catch (KhongPhaiThanhVienNhomException loi) { return StatusCode(403, new { thongBao = loi.Message }); }
    }
}

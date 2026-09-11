using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using HaloChat.Api.Dto;
using Xunit;

namespace HaloChat.Api.Tests;

/// <summary>
/// Kiểm thử tích hợp: gọi thật qua pipeline HTTP (không gọi thẳng
/// class controller) để bắt được các lỗi liên kết DI/JWT/định tuyến mà
/// kiểm thử đơn vị của DichVuNguoiDung không thấy được.
/// </summary>
public class NguoiDungControllerTests : IClassFixture<ThietLapKiemThuTichHop>
{
    private readonly ThietLapKiemThuTichHop _factory;
    private readonly HttpClient _client;

    public NguoiDungControllerTests(ThietLapKiemThuTichHop factory)
    {
        _factory = factory;
        _factory.KhoGiaLap.DanhSach.Clear();
        _client = factory.CreateClient();
    }

    private static DangKyTaiKhoanRequest TaoYeuCauDangKyHopLe(string tenTaiKhoan) =>
        new(tenTaiKhoan, $"{tenTaiKhoan}@gmail.com", "MatKhau123");

    [Fact]
    public async Task DangKy_LanDauThanhCong_LanHaiTrungTenTaiKhoan_TraVe409()
    {
        var yeuCau = TaoYeuCauDangKyHopLe("nguoian01");

        var phanHoiLan1 = await _client.PostAsJsonAsync("/api/nguoidung/dang-ky", yeuCau);
        Assert.Equal(HttpStatusCode.OK, phanHoiLan1.StatusCode);

        var phanHoiLan2 = await _client.PostAsJsonAsync("/api/nguoidung/dang-ky", yeuCau);
        Assert.Equal(HttpStatusCode.Conflict, phanHoiLan2.StatusCode);
    }

    [Fact]
    public async Task DangNhap_SaiMatKhau_TraVe401()
    {
        var yeuCau = TaoYeuCauDangKyHopLe("nguoian02");
        await _client.PostAsJsonAsync("/api/nguoidung/dang-ky", yeuCau);

        var phanHoi = await _client.PostAsJsonAsync(
            "/api/nguoidung/dang-nhap",
            new DangNhapRequest(yeuCau.TenTaiKhoan, "SaiMatKhau"));

        Assert.Equal(HttpStatusCode.Unauthorized, phanHoi.StatusCode);
    }

    [Fact]
    public async Task DangNhap_DungThongTin_TraVe200VaTokenKhongRong()
    {
        var yeuCau = TaoYeuCauDangKyHopLe("nguoian03");
        await _client.PostAsJsonAsync("/api/nguoidung/dang-ky", yeuCau);

        var phanHoi = await _client.PostAsJsonAsync(
            "/api/nguoidung/dang-nhap",
            new DangNhapRequest(yeuCau.TenTaiKhoan, yeuCau.MatKhau));

        Assert.Equal(HttpStatusCode.OK, phanHoi.StatusCode);
        var noiDung = await phanHoi.Content.ReadFromJsonAsync<DangNhapResponse>();
        Assert.False(string.IsNullOrWhiteSpace(noiDung?.Token));
    }

    [Fact]
    public async Task LayDanhSach_KhongCoAuthorizationHeader_TraVe401()
    {
        var phanHoi = await _client.GetAsync("/api/nguoidung");

        Assert.Equal(HttpStatusCode.Unauthorized, phanHoi.StatusCode);
    }

    [Fact]
    public async Task LayDanhSach_CoTokenHopLe_TraVe200VaKhongLoDuLieuNhayCam()
    {
        // Đây là bài kiểm thử "chốt" hồi quy cho MapInboundClaims = false trong
        // Program.cs: nếu dòng đó bị gỡ, claim "sub" trong token sẽ bị JwtBearer
        // đổi tên thành URI chuẩn (ClaimTypes.NameIdentifier) khi ánh xạ vào
        // ClaimsPrincipal, khiến controller đọc JwtRegisteredClaimNames.Sub ra
        // null và trả về 401 thay vì 200 như test này khẳng định.
        var chuTaiKhoan = TaoYeuCauDangKyHopLe("nguoian04");
        await _client.PostAsJsonAsync("/api/nguoidung/dang-ky", chuTaiKhoan);
        var phanHoiDangNhap = await _client.PostAsJsonAsync(
            "/api/nguoidung/dang-nhap",
            new DangNhapRequest(chuTaiKhoan.TenTaiKhoan, chuTaiKhoan.MatKhau));
        var dangNhap = await phanHoiDangNhap.Content.ReadFromJsonAsync<DangNhapResponse>();

        var yeuCau = new HttpRequestMessage(HttpMethod.Get, "/api/nguoidung");
        yeuCau.Headers.Authorization = new AuthenticationHeaderValue("Bearer", dangNhap!.Token);
        var phanHoi = await _client.SendAsync(yeuCau);

        Assert.Equal(HttpStatusCode.OK, phanHoi.StatusCode);

        // Khẳng định trên đúng định dạng "trên dây" (chuỗi JSON thô), không
        // chỉ trên DTO đã giải mã, vì mục tiêu là chứng minh tầng serialize
        // không rò rỉ trường nhạy cảm.
        var noiDungTho = await phanHoi.Content.ReadAsStringAsync();
        Assert.DoesNotContain("matKhauBam", noiDungTho, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("salt", noiDungTho, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("khoaBiMat", noiDungTho, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DangKy_DuLieuKhongHopLe_TraVe400()
    {
        var noiDung = new StringContent(
            "{\"tenTaiKhoan\":\"\",\"email\":\"not-an-email\",\"matKhau\":\"\"}",
            System.Text.Encoding.UTF8,
            "application/json");

        var phanHoi = await _client.PostAsync("/api/nguoidung/dang-ky", noiDung);

        Assert.Equal(HttpStatusCode.BadRequest, phanHoi.StatusCode);
    }
}

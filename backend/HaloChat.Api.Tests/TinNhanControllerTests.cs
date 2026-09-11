using System.Net;
using System.Net.Http.Json;
using HaloChat.Api.Dto;
using HaloChat.Api.Models;
using Xunit;

namespace HaloChat.Api.Tests;

public class TinNhanControllerTests : IClassFixture<ThietLapKiemThuTichHop>
{
    private readonly ThietLapKiemThuTichHop _factory;
    private readonly HttpClient _client;

    public TinNhanControllerTests(ThietLapKiemThuTichHop factory)
    {
        _factory = factory;
        _factory.KhoGiaLap.DanhSach.Clear();
        _factory.KhoTinNhanGiaLap.DanhSach.Clear();
        _client = factory.CreateClient();
    }

    private record DangNhapResponseGiaLap(string Token);

    private async Task<string> DangKyVaDangNhapAsync(string tenTaiKhoan)
    {
        await _client.PostAsJsonAsync("/api/nguoidung/dang-ky", new
        {
            TenTaiKhoan = tenTaiKhoan,
            Email = $"{tenTaiKhoan}@gmail.com",
            MatKhau = "MatKhau123",
        });
        var phanHoi = await _client.PostAsJsonAsync("/api/nguoidung/dang-nhap", new
        {
            TenDangNhap = tenTaiKhoan,
            MatKhau = "MatKhau123",
        });
        var ketQua = await phanHoi.Content.ReadFromJsonAsync<DangNhapResponseGiaLap>();
        return ketQua!.Token;
    }

    [Fact]
    public async Task LayLichSu_ChuaDangNhap_TraVe401()
    {
        var phanHoi = await _client.GetAsync("/api/tinnhan/nguoi-dung/000000000000000000000000");
        Assert.Equal(HttpStatusCode.Unauthorized, phanHoi.StatusCode);
    }

    [Fact]
    public async Task LayLichSu_DaDangNhap_TraVeDanhSach()
    {
        var tokenA = await DangKyVaDangNhapAsync("tinnhannguoia");
        var idA = _factory.KhoGiaLap.DanhSach.Single(nd => nd.TenTaiKhoan == "tinnhannguoia").Id;
        await DangKyVaDangNhapAsync("tinnhannguoib");
        var idB = _factory.KhoGiaLap.DanhSach.Single(nd => nd.TenTaiKhoan == "tinnhannguoib").Id;

        _factory.KhoTinNhanGiaLap.DanhSach.Add(new TinNhan
        {
            NguoiGuiId = idA,
            NguoiNhanId = idB,
            NoiDungTinNhan = "Xin chào",
        });

        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenA);
        var phanHoi = await _client.GetAsync($"/api/tinnhan/nguoi-dung/{idB}");

        Assert.Equal(HttpStatusCode.OK, phanHoi.StatusCode);
        var danhSach = await phanHoi.Content.ReadFromJsonAsync<List<TinNhanDto>>();
        Assert.Single(danhSach!);
    }

    [Fact]
    public async Task TaiLen_DinhDangKhongDuocHoTro_TraVe400()
    {
        var token = await DangKyVaDangNhapAsync("tinnhannguoic");
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        using var noiDung = new MultipartFormDataContent();
        noiDung.Add(new ByteArrayContent(new byte[] { 1, 2, 3 }), "tep", "vi-du.exe");

        var phanHoi = await _client.PostAsync("/api/tinnhan/upload", noiDung);

        Assert.Equal(HttpStatusCode.BadRequest, phanHoi.StatusCode);
    }

    [Fact]
    public async Task TaiLen_AnhHopLe_TraVe200VaDuongDanFile()
    {
        var token = await DangKyVaDangNhapAsync("tinnhannguoid");
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        using var noiDung = new MultipartFormDataContent();
        noiDung.Add(new ByteArrayContent(new byte[] { 1, 2, 3 }), "tep", "anh-mau.png");

        var phanHoi = await _client.PostAsync("/api/tinnhan/upload", noiDung);

        Assert.Equal(HttpStatusCode.OK, phanHoi.StatusCode);
        var ketQua = await phanHoi.Content.ReadFromJsonAsync<TepTinDaTaiLenDto>();
        Assert.StartsWith("/uploads/", ketQua!.DuongDanFile);
        Assert.EndsWith(".png", ketQua.DuongDanFile);

        // Dọn file test tạo ra trên đĩa thật (thư mục uploads/ đã gitignore,
        // nhưng dọn để không tích tụ rác qua nhiều lần chạy test cục bộ).
        var duongDanThat = Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "HaloChat.Api",
            "uploads", Path.GetFileName(ketQua.DuongDanFile));
        if (File.Exists(duongDanThat)) File.Delete(duongDanThat);
    }
}

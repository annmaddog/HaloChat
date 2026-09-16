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
    public async Task LayLichSu_IdKhongPhaiObjectIdHopLe_TraVe400()
    {
        var token = await DangKyVaDangNhapAsync("tinnhannguoie");
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var phanHoi = await _client.GetAsync("/api/tinnhan/nguoi-dung/khong-phai-object-id");

        Assert.Equal(HttpStatusCode.BadRequest, phanHoi.StatusCode);
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
        Assert.StartsWith("/api/tinnhan/file/", ketQua!.DuongDanFile);
    }

    [Fact]
    public async Task TaiLenRoiLayFile_TraVeDungNoiDungGoc()
    {
        var token = await DangKyVaDangNhapAsync("tinnhannguoif");
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        var noiDungGoc = new byte[] { 10, 20, 30, 40, 50 };

        using var formUpload = new MultipartFormDataContent();
        formUpload.Add(new ByteArrayContent(noiDungGoc), "tep", "anh-mau.png");
        var phanHoiUpload = await _client.PostAsync("/api/tinnhan/upload", formUpload);
        var ketQuaUpload = await phanHoiUpload.Content.ReadFromJsonAsync<TepTinDaTaiLenDto>();

        // LayFile không cần đăng nhập — dùng client mới không gắn token, đúng
        // như cách <img>/<a> trên trình duyệt tải file (không đính kèm được
        // header Authorization).
        using var clientKhongDangNhap = _factory.CreateClient();
        var phanHoiFile = await clientKhongDangNhap.GetAsync(ketQuaUpload!.DuongDanFile);

        Assert.Equal(HttpStatusCode.OK, phanHoiFile.StatusCode);
        var noiDungTaiVe = await phanHoiFile.Content.ReadAsByteArrayAsync();
        Assert.Equal(noiDungGoc, noiDungTaiVe);
        Assert.Equal("image/png", phanHoiFile.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task LayFile_KhongTonTai_TraVe404()
    {
        var phanHoi = await _client.GetAsync("/api/tinnhan/file/khong-ton-tai");

        Assert.Equal(HttpStatusCode.NotFound, phanHoi.StatusCode);
    }

    [Fact]
    public async Task LayDanhSachHoiThoai_DaDangNhap_TraVeDanhSach()
    {
        var tokenA = await DangKyVaDangNhapAsync("hoithoainguoia");
        var idA = _factory.KhoGiaLap.DanhSach.Single(nd => nd.TenTaiKhoan == "hoithoainguoia").Id;
        await DangKyVaDangNhapAsync("hoithoainguoib");
        var idB = _factory.KhoGiaLap.DanhSach.Single(nd => nd.TenTaiKhoan == "hoithoainguoib").Id;

        _factory.KhoTinNhanGiaLap.DanhSach.Add(new TinNhan { NguoiGuiId = idB, NguoiNhanId = idA, NoiDungTinNhan = "Chào" });

        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenA);
        var phanHoi = await _client.GetAsync("/api/tinnhan/hoi-thoai");

        Assert.Equal(HttpStatusCode.OK, phanHoi.StatusCode);
        var danhSach = await phanHoi.Content.ReadFromJsonAsync<List<HoiThoaiTomTatDto>>();
        Assert.Single(danhSach!);
        Assert.Equal(idB, danhSach![0].NguoiDung.Id);
    }

    [Fact]
    public async Task LayLichSuNhom_LaThanhVien_TraVeDanhSach()
    {
        var tokenChu = await DangKyVaDangNhapAsync("tinnhannhomchu");
        var idChu = _factory.KhoGiaLap.DanhSach.Single(nd => nd.TenTaiKhoan == "tinnhannhomchu").Id;

        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenChu);
        var nhom = await (await _client.PostAsJsonAsync(
            "/api/nhom", new { TenNhom = "Nhóm Test", MoTa = (string?)null, DuongDanAnhDaiDien = (string?)null, ThanhVienIds = Array.Empty<string>() }))
            .Content.ReadFromJsonAsync<NhomDto>();

        _factory.KhoTinNhanGiaLap.DanhSach.Add(new TinNhan
        {
            NguoiGuiId = idChu,
            NhomId = nhom!.Id,
            NoiDungTinNhan = "Chào nhóm",
        });

        var phanHoi = await _client.GetAsync($"/api/tinnhan/nhom/{nhom.Id}");

        Assert.Equal(HttpStatusCode.OK, phanHoi.StatusCode);
        var danhSach = await phanHoi.Content.ReadFromJsonAsync<List<TinNhanDto>>();
        Assert.Single(danhSach!);
        Assert.Equal(nhom.Id, danhSach![0].NhomId);
    }

    [Fact]
    public async Task LayLichSuNhom_KhongPhaiThanhVien_TraVe403()
    {
        var tokenChu = await DangKyVaDangNhapAsync("tinnhannhomchu2");
        var tokenNguoiNgoai = await DangKyVaDangNhapAsync("tinnhannhomngoai");

        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenChu);
        var nhom = await (await _client.PostAsJsonAsync(
            "/api/nhom", new { TenNhom = "Nhóm Test 2", MoTa = (string?)null, DuongDanAnhDaiDien = (string?)null, ThanhVienIds = Array.Empty<string>() }))
            .Content.ReadFromJsonAsync<NhomDto>();

        using var clientNgoai = _factory.CreateClient();
        clientNgoai.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenNguoiNgoai);
        var phanHoi = await clientNgoai.GetAsync($"/api/tinnhan/nhom/{nhom!.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, phanHoi.StatusCode);
    }

    [Fact]
    public async Task LayLichSuNhom_NhomKhongTonTai_TraVe404()
    {
        var token = await DangKyVaDangNhapAsync("tinnhannhomkhongton");
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var phanHoi = await _client.GetAsync("/api/tinnhan/nhom/000000000000000000000000");

        Assert.Equal(HttpStatusCode.NotFound, phanHoi.StatusCode);
    }

    [Fact]
    public async Task LayLichSuNhom_IdKhongPhaiObjectIdHopLe_TraVe400()
    {
        var token = await DangKyVaDangNhapAsync("tinnhannhomidxau");
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var phanHoi = await _client.GetAsync("/api/tinnhan/nhom/khong-phai-object-id");

        Assert.Equal(HttpStatusCode.BadRequest, phanHoi.StatusCode);
    }

    [Fact]
    public async Task An_IdKhongPhaiTinNhanTonTai_TraVe404()
    {
        await _client.PostAsJsonAsync("/api/nguoidung/dang-ky", new { tenTaiKhoan = "anA", email = "anA@vi.du", matKhau = "MatKhau123!" });
        var dangNhapA = await _client.PostAsJsonAsync("/api/nguoidung/dang-nhap", new { tenDangNhap = "anA", matKhau = "MatKhau123!" });
        var tokenA = (await dangNhapA.Content.ReadFromJsonAsync<DangNhapResponse>())!.Token;
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenA);
        var idA = (await (await _client.GetAsync("/api/nguoidung/toi")).Content.ReadFromJsonAsync<HoSoCaNhanDto>())!.Id;

        var phanHoiAn = await _client.PostAsync($"/api/tinnhan/{idA}/an", null);

        Assert.Equal(System.Net.HttpStatusCode.NotFound, phanHoiAn.StatusCode);
    }
}

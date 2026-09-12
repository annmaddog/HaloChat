using System.Net;
using System.Net.Http.Json;
using HaloChat.Api.Dto;
using Xunit;

namespace HaloChat.Api.Tests;

public class KetBanControllerTests : IClassFixture<ThietLapKiemThuTichHop>
{
    private readonly ThietLapKiemThuTichHop _factory;
    private readonly HttpClient _client;

    public KetBanControllerTests(ThietLapKiemThuTichHop factory)
    {
        _factory = factory;
        _factory.KhoGiaLap.DanhSach.Clear();
        _factory.KhoLoiMoiKetBanGiaLap.DanhSach.Clear();
        _client = factory.CreateClient();
    }

    private record DangNhapResponseGiaLap(string Token);

    private async Task<(string Token, string Id)> DangKyVaDangNhapAsync(string tenTaiKhoan)
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
        var id = _factory.KhoGiaLap.DanhSach.Single(nd => nd.TenTaiKhoan == tenTaiKhoan).Id;
        return (ketQua!.Token, id);
    }

    [Fact]
    public async Task GuiLoiMoi_ChuaDangNhap_TraVe401()
    {
        var phanHoi = await _client.PostAsync("/api/ketban/loi-moi/000000000000000000000000", null);
        Assert.Equal(HttpStatusCode.Unauthorized, phanHoi.StatusCode);
    }

    [Fact]
    public async Task GuiLoiMoi_HopLe_TraVe200VaLuuLoiMoi()
    {
        var (tokenA, _) = await DangKyVaDangNhapAsync("ketbannguoia");
        var (_, idB) = await DangKyVaDangNhapAsync("ketbannguoib");

        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenA);
        var phanHoi = await _client.PostAsync($"/api/ketban/loi-moi/{idB}", null);

        Assert.Equal(HttpStatusCode.OK, phanHoi.StatusCode);
        Assert.Single(_factory.KhoLoiMoiKetBanGiaLap.DanhSach);
    }

    [Fact]
    public async Task GuiLoiMoi_IdKhongPhaiObjectIdHopLe_TraVe404()
    {
        var (tokenA, _) = await DangKyVaDangNhapAsync("ketbannguoin");

        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenA);
        var phanHoi = await _client.PostAsync("/api/ketban/loi-moi/khong-phai-object-id", null);

        Assert.Equal(System.Net.HttpStatusCode.NotFound, phanHoi.StatusCode);
    }

    [Fact]
    public async Task GuiLoiMoi_TuGuiChoChinhMinh_TraVe400()
    {
        var (tokenA, idA) = await DangKyVaDangNhapAsync("ketbannguoic");

        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenA);
        var phanHoi = await _client.PostAsync($"/api/ketban/loi-moi/{idA}", null);

        Assert.Equal(HttpStatusCode.BadRequest, phanHoi.StatusCode);
    }

    [Fact]
    public async Task GuiLoiMoi_DaTonTai_TraVe409()
    {
        var (tokenA, _) = await DangKyVaDangNhapAsync("ketbannguoid");
        var (_, idB) = await DangKyVaDangNhapAsync("ketbannguoie");

        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenA);
        await _client.PostAsync($"/api/ketban/loi-moi/{idB}", null);
        var phanHoiLan2 = await _client.PostAsync($"/api/ketban/loi-moi/{idB}", null);

        Assert.Equal(HttpStatusCode.Conflict, phanHoiLan2.StatusCode);
    }

    [Fact]
    public async Task ChapNhan_KhongPhaiNguoiNhan_TraVe403()
    {
        var (tokenA, _) = await DangKyVaDangNhapAsync("ketbannguoif");
        var (_, idB) = await DangKyVaDangNhapAsync("ketbannguoig");

        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenA);
        var phanHoiGui = await _client.PostAsync($"/api/ketban/loi-moi/{idB}", null);
        var loiMoi = await phanHoiGui.Content.ReadFromJsonAsync<LoiMoiKetBanDto>();

        // Chính người gửi (tokenA) cố chấp nhận lời mời của chính mình — không có quyền.
        var phanHoiChapNhan = await _client.PostAsync($"/api/ketban/{loiMoi!.Id}/chap-nhan", null);

        Assert.Equal(HttpStatusCode.Forbidden, phanHoiChapNhan.StatusCode);
    }

    [Fact]
    public async Task ChapNhan_HopLe_TraVe200VaTaoQuanHeBanBe()
    {
        var (tokenA, idA) = await DangKyVaDangNhapAsync("ketbannguoih");
        var (tokenB, idB) = await DangKyVaDangNhapAsync("ketbannguoik");

        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenA);
        var phanHoiGui = await _client.PostAsync($"/api/ketban/loi-moi/{idB}", null);
        var loiMoi = await phanHoiGui.Content.ReadFromJsonAsync<LoiMoiKetBanDto>();

        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenB);
        var phanHoiChapNhan = await _client.PostAsync($"/api/ketban/{loiMoi!.Id}/chap-nhan", null);
        Assert.Equal(HttpStatusCode.OK, phanHoiChapNhan.StatusCode);

        var phanHoiBanBe = await _client.GetAsync("/api/ketban/ban-be");
        var danhSachBanBe = await phanHoiBanBe.Content.ReadFromJsonAsync<List<NguoiDungTomTatDto>>();
        Assert.Single(danhSachBanBe!);
        Assert.Equal(idA, danhSachBanBe![0].Id);
    }

    [Fact]
    public async Task TuChoi_HopLe_TraVe200()
    {
        var (tokenA, _) = await DangKyVaDangNhapAsync("ketbannguoil");
        var (tokenB, idB) = await DangKyVaDangNhapAsync("ketbannguoim");

        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenA);
        var phanHoiGui = await _client.PostAsync($"/api/ketban/loi-moi/{idB}", null);
        var loiMoi = await phanHoiGui.Content.ReadFromJsonAsync<LoiMoiKetBanDto>();

        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenB);
        var phanHoiTuChoi = await _client.PostAsync($"/api/ketban/{loiMoi!.Id}/tu-choi", null);

        Assert.Equal(HttpStatusCode.OK, phanHoiTuChoi.StatusCode);
    }
}

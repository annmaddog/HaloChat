using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using HaloChat.Api.Dto;
using Xunit;

namespace HaloChat.Api.Tests;

public class NhomControllerTests : IClassFixture<ThietLapKiemThuTichHop>
{
    private readonly ThietLapKiemThuTichHop _factory;
    private readonly HttpClient _client;

    public NhomControllerTests(ThietLapKiemThuTichHop factory)
    {
        _factory = factory;
        _factory.KhoGiaLap.DanhSach.Clear();
        _factory.KhoNhomGiaLap.DanhSach.Clear();
        _client = factory.CreateClient();
    }

    private async Task<(string Token, string Id)> TaoTaiKhoanVaDangNhapAsync(string tenTaiKhoan)
    {
        await _client.PostAsJsonAsync("/api/nguoidung/dang-ky", new
        {
            TenTaiKhoan = tenTaiKhoan, Email = $"{tenTaiKhoan}@gmail.com", MatKhau = "MatKhau123",
        });
        var phanHoi = await _client.PostAsJsonAsync("/api/nguoidung/dang-nhap", new
        {
            TenDangNhap = tenTaiKhoan, MatKhau = "MatKhau123",
        });
        var ketQua = await phanHoi.Content.ReadFromJsonAsync<DangNhapResponse>();
        var id = _factory.KhoGiaLap.DanhSach.Single(nd => nd.TenTaiKhoan == tenTaiKhoan).Id;
        return (ketQua!.Token, id);
    }

    [Fact]
    public async Task TaoNhom_ThanhCong_TraVe200VaGomCaNguoiTao()
    {
        var (token, id) = await TaoTaiKhoanVaDangNhapAsync("nhomnguoia");
        var (_, idThanhVien) = await TaoTaiKhoanVaDangNhapAsync("nhomnguoib");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var phanHoi = await _client.PostAsJsonAsync("/api/nhom", new TaoNhomRequest("Nhóm CNTT", "Mô tả", null, new List<string> { idThanhVien }));

        Assert.Equal(HttpStatusCode.OK, phanHoi.StatusCode);
        var nhom = await phanHoi.Content.ReadFromJsonAsync<NhomDto>();
        Assert.Equal("Nhóm CNTT", nhom!.TenNhom);
        Assert.Equal(id, nhom.NguoiTaoId);
        Assert.Equal(2, nhom.ThanhVien.Count);
    }

    [Fact]
    public async Task TaoNhom_ThanhVienKhongTonTai_TraVe400()
    {
        var (token, _) = await TaoTaiKhoanVaDangNhapAsync("nhomnguoic");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var phanHoi = await _client.PostAsJsonAsync(
            "/api/nhom", new TaoNhomRequest("Nhóm lỗi", null, null, new List<string> { "000000000000000000000000" }));

        Assert.Equal(HttpStatusCode.BadRequest, phanHoi.StatusCode);
    }

    [Fact]
    public async Task LayChiTiet_KhongPhaiThanhVien_TraVe403()
    {
        var (tokenA, _) = await TaoTaiKhoanVaDangNhapAsync("nhomnguoid");
        var (tokenB, _) = await TaoTaiKhoanVaDangNhapAsync("nhomnguoie");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        var nhom = await (await _client.PostAsJsonAsync("/api/nhom", new TaoNhomRequest("Nhóm riêng", null, null, new List<string>())))
            .Content.ReadFromJsonAsync<NhomDto>();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);
        var phanHoi = await _client.GetAsync($"/api/nhom/{nhom!.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, phanHoi.StatusCode);
    }

    [Fact]
    public async Task CapNhat_KhongPhaiAdmin_TraVe403()
    {
        var (tokenA, _) = await TaoTaiKhoanVaDangNhapAsync("nhomnguoif");
        var (tokenB, _) = await TaoTaiKhoanVaDangNhapAsync("nhomnguoig");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        var nhom = await (await _client.PostAsJsonAsync("/api/nhom", new TaoNhomRequest("Nhóm X", null, null, new List<string>())))
            .Content.ReadFromJsonAsync<NhomDto>();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);
        var phanHoi = await _client.PutAsJsonAsync($"/api/nhom/{nhom!.Id}", new CapNhatNhomRequest("Tên mới", null, null));

        Assert.Equal(HttpStatusCode.Forbidden, phanHoi.StatusCode);
    }

    [Fact]
    public async Task LayDanhSach_TraVeDungNhomDaThamGia()
    {
        var (token, _) = await TaoTaiKhoanVaDangNhapAsync("nhomnguoih");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await _client.PostAsJsonAsync("/api/nhom", new TaoNhomRequest("Nhóm Y", null, null, new List<string>()));

        var phanHoi = await _client.GetAsync("/api/nhom");
        var danhSach = await phanHoi.Content.ReadFromJsonAsync<List<NhomDto>>();

        Assert.Single(danhSach!);
    }

    [Fact]
    public async Task ThemThanhVien_LaAdmin_ThanhCong()
    {
        var (tokenAdmin, _) = await TaoTaiKhoanVaDangNhapAsync("nhomthem1");
        var (_, idMoi) = await TaoTaiKhoanVaDangNhapAsync("nhomthem2");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenAdmin);
        var nhom = await (await _client.PostAsJsonAsync("/api/nhom", new TaoNhomRequest("Nhóm thêm", null, null, new List<string>())))
            .Content.ReadFromJsonAsync<NhomDto>();

        var phanHoi = await _client.PostAsJsonAsync($"/api/nhom/{nhom!.Id}/thanh-vien", new ThemThanhVienRequest(idMoi));

        Assert.Equal(HttpStatusCode.OK, phanHoi.StatusCode);
        var nhomSau = await phanHoi.Content.ReadFromJsonAsync<NhomDto>();
        Assert.Contains(nhomSau!.ThanhVien, tv => tv.Id == idMoi);
    }

    [Fact]
    public async Task ThemThanhVien_KhongPhaiAdmin_TraVe403()
    {
        var (tokenAdmin, _) = await TaoTaiKhoanVaDangNhapAsync("nhomthem3");
        var (tokenKhac, idKhac) = await TaoTaiKhoanVaDangNhapAsync("nhomthem4");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenAdmin);
        var nhom = await (await _client.PostAsJsonAsync("/api/nhom", new TaoNhomRequest("Nhóm X2", null, null, new List<string>())))
            .Content.ReadFromJsonAsync<NhomDto>();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenKhac);
        var phanHoi = await _client.PostAsJsonAsync($"/api/nhom/{nhom!.Id}/thanh-vien", new ThemThanhVienRequest(idKhac));

        Assert.Equal(HttpStatusCode.Forbidden, phanHoi.StatusCode);
    }

    [Fact]
    public async Task XoaThanhVien_XoaNguoiTao_TraVe400()
    {
        var (tokenAdmin, idAdmin) = await TaoTaiKhoanVaDangNhapAsync("nhomxoa1");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenAdmin);
        var nhom = await (await _client.PostAsJsonAsync("/api/nhom", new TaoNhomRequest("Nhóm xóa", null, null, new List<string>())))
            .Content.ReadFromJsonAsync<NhomDto>();

        var phanHoi = await _client.DeleteAsync($"/api/nhom/{nhom!.Id}/thanh-vien/{idAdmin}");

        Assert.Equal(HttpStatusCode.BadRequest, phanHoi.StatusCode);
    }

    [Fact]
    public async Task RoiNhom_ThanhVienThuong_KhongGiaiTanNhom()
    {
        var (tokenAdmin, _) = await TaoTaiKhoanVaDangNhapAsync("nhomroi1");
        var (tokenTv, idTv) = await TaoTaiKhoanVaDangNhapAsync("nhomroi2");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenAdmin);
        var nhom = await (await _client.PostAsJsonAsync("/api/nhom", new TaoNhomRequest("Nhóm rời", null, null, new List<string> { idTv })))
            .Content.ReadFromJsonAsync<NhomDto>();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenTv);
        var phanHoi = await _client.PostAsync($"/api/nhom/{nhom!.Id}/roi-nhom", null);

        Assert.Equal(HttpStatusCode.OK, phanHoi.StatusCode);
        var nhomConLai = _factory.KhoNhomGiaLap.DanhSach.Single(n => n.Id == nhom.Id);
        Assert.DoesNotContain(idTv, nhomConLai.ThanhVienIds);
    }

    [Fact]
    public async Task RoiNhom_LaNguoiTao_GiaiTanNhom()
    {
        var (tokenAdmin, _) = await TaoTaiKhoanVaDangNhapAsync("nhomroi3");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenAdmin);
        var nhom = await (await _client.PostAsJsonAsync("/api/nhom", new TaoNhomRequest("Nhóm giải tán", null, null, new List<string>())))
            .Content.ReadFromJsonAsync<NhomDto>();

        var phanHoi = await _client.PostAsync($"/api/nhom/{nhom!.Id}/roi-nhom", null);

        Assert.Equal(HttpStatusCode.OK, phanHoi.StatusCode);
        Assert.DoesNotContain(_factory.KhoNhomGiaLap.DanhSach, n => n.Id == nhom.Id);
    }
}

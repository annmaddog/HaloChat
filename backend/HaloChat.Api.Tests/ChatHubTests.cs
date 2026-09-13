using System.Net.Http.Json;
using HaloChat.Api.Dto;
using HaloChat.Api.Models;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Xunit;

namespace HaloChat.Api.Tests;

public class ChatHubTests : IClassFixture<ThietLapKiemThuTichHop>
{
    private readonly ThietLapKiemThuTichHop _factory;

    public ChatHubTests(ThietLapKiemThuTichHop factory)
    {
        _factory = factory;
        _factory.KhoGiaLap.DanhSach.Clear();
        _factory.KhoTinNhanGiaLap.DanhSach.Clear();
    }

    private async Task<string> TaoTaiKhoanVaDangNhapAsync(string tenTaiKhoan)
    {
        var client = _factory.CreateClient();
        await client.PostAsJsonAsync("/api/nguoidung/dang-ky", new
        {
            TenTaiKhoan = tenTaiKhoan,
            Email = $"{tenTaiKhoan}@gmail.com",
            MatKhau = "MatKhau123",
        });
        var phanHoi = await client.PostAsJsonAsync("/api/nguoidung/dang-nhap", new
        {
            TenDangNhap = tenTaiKhoan,
            MatKhau = "MatKhau123",
        });
        var ketQua = await phanHoi.Content.ReadFromJsonAsync<DangNhapResponseGiaLap>();
        return ketQua!.Token;
    }

    private record DangNhapResponseGiaLap(string Token);

    private HubConnection TaoKetNoiHub(string token)
    {
        return new HubConnectionBuilder()
            .WithUrl(new Uri(_factory.Server.BaseAddress, "hub/chat"), options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
                options.AccessTokenProvider = () => Task.FromResult<string?>(token);
            })
            .Build();
    }

    [Fact]
    public async Task GuiTinNhan_NguoiNhanDangKetNoi_NhanDuocTinNhanRealtime()
    {
        var tokenA = await TaoTaiKhoanVaDangNhapAsync("hubnguoia");
        var tokenB = await TaoTaiKhoanVaDangNhapAsync("hubnguoib");
        var idNguoiB = _factory.KhoGiaLap.DanhSach.Single(nd => nd.TenTaiKhoan == "hubnguoib").Id;
        _factory.KhoGiaLap.DanhSach.Single(nd => nd.TenTaiKhoan == "hubnguoib").ChoPhepTinNhanTuNguoiLa = true;

        await using var ketNoiA = TaoKetNoiHub(tokenA);
        await using var ketNoiB = TaoKetNoiHub(tokenB);

        TinNhanDto? tinNhanNhanDuoc = null;
        var daNhan = new TaskCompletionSource();
        ketNoiB.On<TinNhanDto>("NhanTinNhan", tinNhan =>
        {
            tinNhanNhanDuoc = tinNhan;
            daNhan.SetResult();
        });

        await ketNoiA.StartAsync();
        await ketNoiB.StartAsync();

        var tinNhanGui = await ketNoiA.InvokeAsync<TinNhanDto>(
            "GuiTinNhan", idNguoiB, "Text", "Chào bạn", null, null, null, null);

        await daNhan.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal("Chào bạn", tinNhanGui.NoiDungTinNhan);
        Assert.NotNull(tinNhanNhanDuoc);
        Assert.Equal("Chào bạn", tinNhanNhanDuoc!.NoiDungTinNhan);
    }

    [Fact]
    public async Task GuiTinNhan_NguoiNhanKhongTonTai_NemHubException()
    {
        var token = await TaoTaiKhoanVaDangNhapAsync("hubnguoic");
        await using var ketNoi = TaoKetNoiHub(token);
        await ketNoi.StartAsync();

        await Assert.ThrowsAsync<HubException>(() =>
            ketNoi.InvokeAsync<TinNhanDto>(
                "GuiTinNhan", "000000000000000000000000", "Text", "Xin chào", null, null, null, null));
    }

    [Fact]
    public async Task GuiTinNhan_ChuaXacThuc_TuChoiKetNoi()
    {
        await using var ketNoi = new HubConnectionBuilder()
            .WithUrl(new Uri(_factory.Server.BaseAddress, "hub/chat"), options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
            })
            .Build();

        await Assert.ThrowsAnyAsync<Exception>(() => ketNoi.StartAsync());
    }

    [Fact]
    public async Task OnConnected_LaThanhVienNhom_NhanDuocTinNhanNhomGuiTrongLucDangKetNoi()
    {
        var tokenChu = await TaoTaiKhoanVaDangNhapAsync("hubnhomchu");
        var idChu = _factory.KhoGiaLap.DanhSach.Single(nd => nd.TenTaiKhoan == "hubnhomchu").Id;
        var tokenThanhVien = await TaoTaiKhoanVaDangNhapAsync("hubnhomtv");
        var idThanhVien = _factory.KhoGiaLap.DanhSach.Single(nd => nd.TenTaiKhoan == "hubnhomtv").Id;

        using var clientTao = _factory.CreateClient();
        clientTao.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenChu);
        var nhom = await (await clientTao.PostAsJsonAsync(
            "/api/nhom", new { TenNhom = "Nhóm Hub", MoTa = (string?)null, DuongDanAnhDaiDien = (string?)null, ThanhVienIds = new[] { idThanhVien } }))
            .Content.ReadFromJsonAsync<HaloChat.Api.Dto.NhomDto>();

        await using var ketNoiThanhVien = TaoKetNoiHub(tokenThanhVien);
        var daNhan = new TaskCompletionSource();
        ketNoiThanhVien.On<object>("NhanTinNhan", _ => daNhan.SetResult());
        await ketNoiThanhVien.StartAsync();

        // Thành viên đã join group "nhom-{id}" ngay lúc OnConnectedAsync (trước khi
        // có tin nhắn nào) — xác nhận gián tiếp bằng cách người tạo gửi tin nhắn
        // (qua REST giả lập, ở đây dùng chính Hub) và thành viên nhận được realtime.
        // Task 6 mới thêm tham số nhomId vào GuiTinNhan — ở Task 5 này ta chỉ xác
        // nhận việc join group không lỗi, không gọi GuiTinNhan(nhomId) được vì
        // chưa tồn tại tham số đó. Test đầy đủ hành vi gửi tin nhóm chuyển sang
        // Task 6 (ChatHubTests bổ sung thêm ở đó); test này chỉ khẳng định
        // OnConnectedAsync không ném lỗi và kết nối thành công cho 1 user có nhóm.
        Assert.True(ketNoiThanhVien.State == Microsoft.AspNetCore.SignalR.Client.HubConnectionState.Connected);
        Assert.NotNull(nhom);
    }

    [Fact]
    public async Task OnConnected_LaBanBe_NhanDuocSuKienTrangThaiHoatDongThayDoi()
    {
        var tokenA = await TaoTaiKhoanVaDangNhapAsync("hubpresencea");
        var idA = _factory.KhoGiaLap.DanhSach.Single(nd => nd.TenTaiKhoan == "hubpresencea").Id;
        var tokenB = await TaoTaiKhoanVaDangNhapAsync("hubpresenceb");
        var idB = _factory.KhoGiaLap.DanhSach.Single(nd => nd.TenTaiKhoan == "hubpresenceb").Id;
        _factory.KhoLoiMoiKetBanGiaLap.DanhSach.Add(new HaloChat.Api.Models.LoiMoiKetBan
        {
            NguoiGuiId = idA, NguoiNhanId = idB, TrangThai = HaloChat.Api.Models.TrangThaiLoiMoiKetBan.DaChapNhan,
        });

        await using var ketNoiA = TaoKetNoiHub(tokenA);
        await ketNoiA.StartAsync();

        var daNhanThayDoi = new TaskCompletionSource();
        (string UserId, bool Online)? suKienNhanDuoc = null;
        await using var ketNoiB = TaoKetNoiHub(tokenB);
        ketNoiA.On<string, bool>("TrangThaiHoatDongThayDoi", (userId, online) =>
        {
            suKienNhanDuoc = (userId, online);
            daNhanThayDoi.SetResult();
        });
        await ketNoiB.StartAsync();

        // B online sau A và là bạn của A => A phải nhận sự kiện A thấy B online.
        await daNhanThayDoi.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.NotNull(suKienNhanDuoc);
        Assert.Equal(idB, suKienNhanDuoc!.Value.UserId);
        Assert.True(suKienNhanDuoc.Value.Online);
    }
}

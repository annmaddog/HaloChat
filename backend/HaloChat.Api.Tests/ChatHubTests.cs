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
}

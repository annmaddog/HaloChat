using HaloChat.Api.Models;
using HaloChat.Api.Tests.Fakes;
using Xunit;

namespace HaloChat.Api.Tests.Repositories;

public class TinNhanGiaLapTimTheoIdTests
{
    [Fact]
    public async Task TimTheoIdAsync_TonTai_TraVeDungTinNhan()
    {
        var kho = new TinNhanGiaLap();
        var tinNhan = new TinNhan { NoiDungTinNhan = "Xin chào" };
        kho.DanhSach.Add(tinNhan);

        var ketQua = await kho.TimTheoIdAsync(tinNhan.Id);

        Assert.NotNull(ketQua);
        Assert.Equal("Xin chào", ketQua!.NoiDungTinNhan);
    }

    [Fact]
    public async Task TimTheoIdAsync_KhongTonTai_TraVeNull()
    {
        var kho = new TinNhanGiaLap();

        var ketQua = await kho.TimTheoIdAsync("507f1f77bcf86cd799439099");

        Assert.Null(ketQua);
    }
}

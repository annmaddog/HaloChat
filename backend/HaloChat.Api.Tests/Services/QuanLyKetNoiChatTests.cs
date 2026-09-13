using HaloChat.Api.Services;
using Xunit;

namespace HaloChat.Api.Tests.Services;

public class QuanLyKetNoiChatTests
{
    [Fact]
    public void ThemKetNoi_ConnectionDauTien_TraVeTrueVaDangOnline()
    {
        var quanLy = new QuanLyKetNoiChat();

        var laOnlineMoi = quanLy.ThemKetNoi("user1", "conn1");

        Assert.True(laOnlineMoi);
        Assert.True(quanLy.DangOnline("user1"));
    }

    [Fact]
    public void ThemKetNoi_ConnectionThuHai_TraVeFalse()
    {
        var quanLy = new QuanLyKetNoiChat();
        quanLy.ThemKetNoi("user1", "conn1");

        var laOnlineMoi = quanLy.ThemKetNoi("user1", "conn2");

        Assert.False(laOnlineMoi);
    }

    [Fact]
    public void XoaKetNoi_ConnectionCuoiCung_TraVeTrueVaHetOnline()
    {
        var quanLy = new QuanLyKetNoiChat();
        quanLy.ThemKetNoi("user1", "conn1");

        var vuaOffline = quanLy.XoaKetNoi("user1", "conn1");

        Assert.True(vuaOffline);
        Assert.False(quanLy.DangOnline("user1"));
    }

    [Fact]
    public void XoaKetNoi_ConVaiConnectionKhac_TraVeFalseVaVanOnline()
    {
        var quanLy = new QuanLyKetNoiChat();
        quanLy.ThemKetNoi("user1", "conn1");
        quanLy.ThemKetNoi("user1", "conn2");

        var vuaOffline = quanLy.XoaKetNoi("user1", "conn1");

        Assert.False(vuaOffline);
        Assert.True(quanLy.DangOnline("user1"));
    }

    [Fact]
    public void DangOnline_UserChuaTungKetNoi_TraVeFalse()
    {
        var quanLy = new QuanLyKetNoiChat();

        Assert.False(quanLy.DangOnline("user-la"));
    }
}

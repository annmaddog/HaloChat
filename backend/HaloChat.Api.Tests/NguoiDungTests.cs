using HaloChat.Api.Models;
using Xunit;

namespace HaloChat.Api.Tests;

public class NguoiDungTests
{
    [Fact]
    public void TenHienThiThucTe_ChuaDatTenHienThi_TraVeTenTaiKhoan()
    {
        var nguoiDung = new NguoiDung { TenTaiKhoan = "annguyen", TenHienThi = "" };
        Assert.Equal("annguyen", nguoiDung.TenHienThiThucTe());
    }

    [Fact]
    public void TenHienThiThucTe_TenHienThiChiCoKhoangTrang_TraVeTenTaiKhoan()
    {
        var nguoiDung = new NguoiDung { TenTaiKhoan = "annguyen", TenHienThi = "   " };
        Assert.Equal("annguyen", nguoiDung.TenHienThiThucTe());
    }

    [Fact]
    public void TenHienThiThucTe_DaDatTenHienThi_TraVeTenHienThi()
    {
        var nguoiDung = new NguoiDung { TenTaiKhoan = "annguyen", TenHienThi = "An Nguyễn" };
        Assert.Equal("An Nguyễn", nguoiDung.TenHienThiThucTe());
    }
}

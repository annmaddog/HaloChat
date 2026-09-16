using HaloChat.Api.Models;
using HaloChat.Api.Tests.Fakes;
using Xunit;

namespace HaloChat.Api.Tests.Repositories;

public class TinNhanGiaLapThuHoiGhimTests
{
    [Fact]
    public async Task DanhDauThuHoiAsync_DatDaThuHoiTrue()
    {
        var kho = new TinNhanGiaLap();
        var tinNhan = new TinNhan();
        kho.DanhSach.Add(tinNhan);

        await kho.DanhDauThuHoiAsync(tinNhan.Id);

        Assert.True(kho.DanhSach.Single().DaThuHoi);
    }

    [Fact]
    public async Task DatGhimAsync_Ghim_DatDungCaHaiField()
    {
        var kho = new TinNhanGiaLap();
        var tinNhan = new TinNhan();
        kho.DanhSach.Add(tinNhan);
        var thoiGian = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        await kho.DatGhimAsync(tinNhan.Id, true, thoiGian);

        Assert.True(kho.DanhSach.Single().DaGhim);
        Assert.Equal(thoiGian, kho.DanhSach.Single().ThoiGianGhim);
    }

    [Fact]
    public async Task DatGhimAsync_BoGhim_DatThoiGianGhimNull()
    {
        var kho = new TinNhanGiaLap();
        var tinNhan = new TinNhan { DaGhim = true, ThoiGianGhim = DateTime.UtcNow };
        kho.DanhSach.Add(tinNhan);

        await kho.DatGhimAsync(tinNhan.Id, false, null);

        Assert.False(kho.DanhSach.Single().DaGhim);
        Assert.Null(kho.DanhSach.Single().ThoiGianGhim);
    }

    [Fact]
    public async Task LayTinDaGhimTheoNguoiDungAsync_ChiTraVeTinDaGhimGiuaHaiNguoi()
    {
        var kho = new TinNhanGiaLap();
        kho.DanhSach.Add(new TinNhan { NguoiGuiId = "a", NguoiNhanId = "b", DaGhim = true, ThoiGianGhim = DateTime.UtcNow });
        kho.DanhSach.Add(new TinNhan { NguoiGuiId = "a", NguoiNhanId = "b", DaGhim = false });
        kho.DanhSach.Add(new TinNhan { NguoiGuiId = "a", NguoiNhanId = "c", DaGhim = true, ThoiGianGhim = DateTime.UtcNow });

        var ketQua = await kho.LayTinDaGhimTheoNguoiDungAsync("a", "b");

        Assert.Single(ketQua);
    }

    [Fact]
    public async Task LayTinDaGhimTheoNhomAsync_ChiTraVeTinDaGhimCuaNhomDo()
    {
        var kho = new TinNhanGiaLap();
        kho.DanhSach.Add(new TinNhan { NhomId = "n1", DaGhim = true, ThoiGianGhim = DateTime.UtcNow });
        kho.DanhSach.Add(new TinNhan { NhomId = "n1", DaGhim = false });
        kho.DanhSach.Add(new TinNhan { NhomId = "n2", DaGhim = true, ThoiGianGhim = DateTime.UtcNow });

        var ketQua = await kho.LayTinDaGhimTheoNhomAsync("n1");

        Assert.Single(ketQua);
    }
}

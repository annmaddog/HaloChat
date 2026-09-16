using HaloChat.Api.Models;
using HaloChat.Api.Tests.Fakes;
using Xunit;

namespace HaloChat.Api.Tests.Repositories;

public class TinNhanGiaLapCamXucTests
{
    [Fact]
    public async Task ThaCamXucAsync_ChuaTung_ThemMoi()
    {
        var kho = new TinNhanGiaLap();
        var tinNhan = new TinNhan();
        kho.DanhSach.Add(tinNhan);

        await kho.ThaCamXucAsync(tinNhan.Id, "nguoi-a", LoaiCamXuc.Thich);

        var camXuc = Assert.Single(kho.DanhSach.Single().DanhSachCamXuc);
        Assert.Equal("nguoi-a", camXuc.NguoiDungId);
        Assert.Equal(LoaiCamXuc.Thich, camXuc.LoaiCamXuc);
    }

    [Fact]
    public async Task ThaCamXucAsync_DaCoCamXucKhac_ThayTheKhongCongDon()
    {
        var kho = new TinNhanGiaLap();
        var tinNhan = new TinNhan();
        tinNhan.DanhSachCamXuc.Add(new CamXucTinNhan { NguoiDungId = "nguoi-a", LoaiCamXuc = LoaiCamXuc.Thich });
        kho.DanhSach.Add(tinNhan);

        await kho.ThaCamXucAsync(tinNhan.Id, "nguoi-a", LoaiCamXuc.Haha);

        var camXuc = Assert.Single(kho.DanhSach.Single().DanhSachCamXuc);
        Assert.Equal(LoaiCamXuc.Haha, camXuc.LoaiCamXuc);
    }

    [Fact]
    public async Task BoCamXucAsync_DaCo_XoaDung()
    {
        var kho = new TinNhanGiaLap();
        var tinNhan = new TinNhan();
        tinNhan.DanhSachCamXuc.Add(new CamXucTinNhan { NguoiDungId = "nguoi-a", LoaiCamXuc = LoaiCamXuc.Thich });
        kho.DanhSach.Add(tinNhan);

        await kho.BoCamXucAsync(tinNhan.Id, "nguoi-a");

        Assert.Empty(kho.DanhSach.Single().DanhSachCamXuc);
    }
}

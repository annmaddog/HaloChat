using HaloChat.Api.Models;
using HaloChat.Api.Tests.Fakes;
using Xunit;

namespace HaloChat.Api.Tests.Repositories;

public class TinNhanGiaLapMediaTests
{
    [Fact]
    public async Task LayMediaTheoNguoiDungAsync_ChiTraVeAnhVaFile_LoaiTruText()
    {
        var kho = new TinNhanGiaLap();
        kho.DanhSach.Add(new TinNhan { NguoiGuiId = "a", NguoiNhanId = "b", LoaiTinNhan = LoaiTinNhan.Anh });
        kho.DanhSach.Add(new TinNhan { NguoiGuiId = "a", NguoiNhanId = "b", LoaiTinNhan = LoaiTinNhan.File });
        kho.DanhSach.Add(new TinNhan { NguoiGuiId = "a", NguoiNhanId = "b", LoaiTinNhan = LoaiTinNhan.Text });

        var ketQua = await kho.LayMediaTheoNguoiDungAsync("a", "b");

        Assert.Equal(2, ketQua.Count);
    }

    [Fact]
    public async Task LayMediaTheoNhomAsync_ChiTraVeAnhVaFileCuaDungNhom()
    {
        var kho = new TinNhanGiaLap();
        kho.DanhSach.Add(new TinNhan { NhomId = "n1", LoaiTinNhan = LoaiTinNhan.Anh });
        kho.DanhSach.Add(new TinNhan { NhomId = "n1", LoaiTinNhan = LoaiTinNhan.Text });
        kho.DanhSach.Add(new TinNhan { NhomId = "n2", LoaiTinNhan = LoaiTinNhan.File });

        var ketQua = await kho.LayMediaTheoNhomAsync("n1");

        Assert.Single(ketQua);
    }
}

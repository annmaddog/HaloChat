using HaloChat.Api.Models;
using HaloChat.Api.Tests.Fakes;
using Xunit;

namespace HaloChat.Api.Tests.Repositories;

public class TinNhanGiaLapTimKiemTests
{
    [Fact]
    public async Task TimKiemTheoNguoiDungAsync_KhopKhongPhanBietHoaThuong()
    {
        var kho = new TinNhanGiaLap();
        kho.DanhSach.Add(new TinNhan { NguoiGuiId = "a", NguoiNhanId = "b", LoaiTinNhan = LoaiTinNhan.Text, NoiDungTinNhan = "Hẹn gặp lúc 5 giờ" });
        kho.DanhSach.Add(new TinNhan { NguoiGuiId = "a", NguoiNhanId = "b", LoaiTinNhan = LoaiTinNhan.Text, NoiDungTinNhan = "Không liên quan" });

        var ketQua = await kho.TimKiemTheoNguoiDungAsync("a", "b", "HẸN GẶP");

        Assert.Single(ketQua);
    }

    [Fact]
    public async Task TimKiemTheoNguoiDungAsync_LoaiTruTinDaThuHoi()
    {
        var kho = new TinNhanGiaLap();
        kho.DanhSach.Add(new TinNhan { NguoiGuiId = "a", NguoiNhanId = "b", LoaiTinNhan = LoaiTinNhan.Text, NoiDungTinNhan = "Bí mật quan trọng", DaThuHoi = true });

        var ketQua = await kho.TimKiemTheoNguoiDungAsync("a", "b", "bí mật");

        Assert.Empty(ketQua);
    }

    [Fact]
    public async Task TimKiemTheoNhomAsync_ChiTraVeTinCuaDungNhom()
    {
        var kho = new TinNhanGiaLap();
        kho.DanhSach.Add(new TinNhan { NhomId = "n1", LoaiTinNhan = LoaiTinNhan.Text, NoiDungTinNhan = "họp nhóm 5h" });
        kho.DanhSach.Add(new TinNhan { NhomId = "n2", LoaiTinNhan = LoaiTinNhan.Text, NoiDungTinNhan = "họp nhóm 5h" });

        var ketQua = await kho.TimKiemTheoNhomAsync("n1", "họp");

        Assert.Single(ketQua);
    }
}

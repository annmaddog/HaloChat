using HaloChat.Api.Models;
using HaloChat.Api.Services;
using HaloChat.Api.Tests.Fakes;
using Xunit;

namespace HaloChat.Api.Tests.Services;

public class DichVuNhomTests
{
    private static (DichVuNhom DichVu, NhomGiaLap KhoNhom, NguoiDungGiaLap KhoNguoiDung, TinNhanGiaLap KhoTinNhan, DocNhomGiaLap KhoDocNhom) TaoDichVu()
    {
        var khoNhom = new NhomGiaLap();
        var khoNguoiDung = new NguoiDungGiaLap();
        var khoTinNhan = new TinNhanGiaLap();
        var khoDocNhom = new DocNhomGiaLap();
        var dichVu = new DichVuNhom(khoNhom, khoNguoiDung, khoTinNhan, khoDocNhom);
        return (dichVu, khoNhom, khoNguoiDung, khoTinNhan, khoDocNhom);
    }

    // Id thành viên phải là ObjectId hợp lệ (24 ký tự hex) — TaoNhomAsync/
    // ThemThanhVienAsync đã có sẵn kiểm tra ObjectId.TryParse trước khi tới
    // được kiểm tra ChoPhepThemVaoNhom mới, nên id kiểu "admin"/"b" (không
    // phải hex 24 ký tự) sẽ bị chặn sớm bởi ThanhVienKhongTonTaiException.
    private const string IdAdmin = "507f191e810c19729de860ea";
    private const string IdB = "507f191e810c19729de860eb";

    [Fact]
    public async Task ThemThanhVien_NguoiNhanTatChoPhepThemVaoNhom_NemLoi()
    {
        var (dichVu, khoNhom, khoNguoiDung, _, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = IdAdmin, TenTaiKhoan = "Admin" });
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = IdB, TenTaiKhoan = "NguoiB", ChoPhepThemVaoNhom = false });
        khoNhom.DanhSach.Add(new Nhom { Id = "n1", TenNhom = "Nhom1", NguoiTaoId = IdAdmin, ThanhVienIds = new() { IdAdmin } });

        await Assert.ThrowsAsync<KhongChoPhepThemVaoNhomException>(() => dichVu.ThemThanhVienAsync(IdAdmin, "n1", IdB));
    }

    [Fact]
    public async Task ThemThanhVien_NguoiNhanChoPhep_ThemThanhCong()
    {
        var (dichVu, khoNhom, khoNguoiDung, _, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = IdAdmin, TenTaiKhoan = "Admin" });
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = IdB, TenTaiKhoan = "NguoiB", ChoPhepThemVaoNhom = true });
        khoNhom.DanhSach.Add(new Nhom { Id = "n1", TenNhom = "Nhom1", NguoiTaoId = IdAdmin, ThanhVienIds = new() { IdAdmin } });

        var nhom = await dichVu.ThemThanhVienAsync(IdAdmin, "n1", IdB);

        Assert.Contains(nhom.ThanhVien, tv => tv.Id == IdB);
    }

    [Fact]
    public async Task TaoNhom_MotThanhVienTatChoPhepThemVaoNhom_NemLoi()
    {
        var (dichVu, _, khoNguoiDung, _, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = IdAdmin, TenTaiKhoan = "Admin" });
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = IdB, TenTaiKhoan = "NguoiB", ChoPhepThemVaoNhom = false });

        await Assert.ThrowsAsync<KhongChoPhepThemVaoNhomException>(
            () => dichVu.TaoNhomAsync(IdAdmin, "NhomMoi", null, null, new List<string> { IdB }));
    }

    [Fact]
    public async Task DemTongChuaDoc_ChuaTungDoc_DemTatCaTinNhanCuaMoiNhom()
    {
        var (dichVu, khoNhom, khoNguoiDung, khoTinNhan, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = "toi", TenTaiKhoan = "Toi" });
        khoNhom.DanhSach.Add(new Nhom { Id = "n1", TenNhom = "N1", NguoiTaoId = "toi", ThanhVienIds = new() { "toi" } });
        khoNhom.DanhSach.Add(new Nhom { Id = "n2", TenNhom = "N2", NguoiTaoId = "toi", ThanhVienIds = new() { "toi" } });
        khoTinNhan.DanhSach.Add(new TinNhan { Id = "1", NhomId = "n1" });
        khoTinNhan.DanhSach.Add(new TinNhan { Id = "2", NhomId = "n1" });
        khoTinNhan.DanhSach.Add(new TinNhan { Id = "3", NhomId = "n2" });

        var tong = await dichVu.DemTongChuaDocAsync("toi");

        Assert.Equal(3, tong);
    }

    [Fact]
    public async Task DemTongChuaDoc_DaDocMotPhan_ChiDemTinMoiHonMocDaDoc()
    {
        var (dichVu, khoNhom, khoNguoiDung, khoTinNhan, khoDocNhom) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = "toi", TenTaiKhoan = "Toi" });
        khoNhom.DanhSach.Add(new Nhom { Id = "n1", TenNhom = "N1", NguoiTaoId = "toi", ThanhVienIds = new() { "toi" } });
        khoTinNhan.DanhSach.Add(new TinNhan { Id = "1", NhomId = "n1" });
        khoTinNhan.DanhSach.Add(new TinNhan { Id = "2", NhomId = "n1" });
        khoTinNhan.DanhSach.Add(new TinNhan { Id = "3", NhomId = "n1" });
        await khoDocNhom.DanhDauDaDocAsync("toi", "n1", "1");

        var tong = await dichVu.DemTongChuaDocAsync("toi");

        Assert.Equal(2, tong);
    }
}

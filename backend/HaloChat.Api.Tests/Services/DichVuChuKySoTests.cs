using HaloChat.Security;
using Xunit;

namespace HaloChat.Api.Tests.Services;

public class DichVuChuKySoTests
{
    private static DichVuChuKySo TaoDichVu() => new();
    private static DichVuMaHoa TaoDichVuMaHoa() => new();

    [Fact]
    public void KyRoiXacMinh_DungCapKhoa_TraVeTrue()
    {
        var dichVu = TaoDichVu();
        var (khoaCongKhai, khoaBiMat) = TaoDichVuMaHoa().SinhCapKhoaRsa();
        const string noiDung = "Chào Bình, tối nay học mật mã nhé!";

        var chuKy = dichVu.KyDuLieu(noiDung, khoaBiMat);
        var hopLe = dichVu.XacMinhChuKy(noiDung, chuKy, khoaCongKhai);

        Assert.True(hopLe);
    }

    [Fact]
    public void KyRoiXacMinh_DoiMotKyTuNoiDung_TraVeFalse()
    {
        var dichVu = TaoDichVu();
        var (khoaCongKhai, khoaBiMat) = TaoDichVuMaHoa().SinhCapKhoaRsa();
        var chuKy = dichVu.KyDuLieu("Nội dung gốc", khoaBiMat);

        var hopLe = dichVu.XacMinhChuKy("Nội dung gốc bị sửa", chuKy, khoaCongKhai);

        Assert.False(hopLe);
    }

    [Fact]
    public void XacMinh_SaiKhoaCongKhai_TraVeFalse()
    {
        var dichVu = TaoDichVu();
        var maHoa = TaoDichVuMaHoa();
        var (_, khoaBiMatDung) = maHoa.SinhCapKhoaRsa();
        var (khoaCongKhaiSai, _) = maHoa.SinhCapKhoaRsa();
        const string noiDung = "Xin chào";
        var chuKy = dichVu.KyDuLieu(noiDung, khoaBiMatDung);

        var hopLe = dichVu.XacMinhChuKy(noiDung, chuKy, khoaCongKhaiSai);

        Assert.False(hopLe);
    }

    [Fact]
    public void XacMinh_ChuKyLaChuoiRacKhongPhaiBase64_TraVeFalseKhongNemLoi()
    {
        var dichVu = TaoDichVu();
        var (khoaCongKhai, _) = TaoDichVuMaHoa().SinhCapKhoaRsa();

        var hopLe = dichVu.XacMinhChuKy("Xin chào", "***không phải base64***", khoaCongKhai);

        Assert.False(hopLe);
    }

    [Fact]
    public void XacMinh_ChuKyLaBase64HopLeNhungKhongPhaiChuKyThat_TraVeFalseKhongNemLoi()
    {
        var dichVu = TaoDichVu();
        var (khoaCongKhai, _) = TaoDichVuMaHoa().SinhCapKhoaRsa();
        var chuKyRac = Convert.ToBase64String(new byte[] { 1, 2, 3, 4, 5 });

        var hopLe = dichVu.XacMinhChuKy("Xin chào", chuKyRac, khoaCongKhai);

        Assert.False(hopLe);
    }

    [Fact]
    public void KyHaiLanCungNoiDung_ChuKyKhacNhau()
    {
        // RSA-PSS dùng "salt" ngẫu nhiên nên cùng nội dung + cùng khóa vẫn ra 2 chữ ký khác nhau.
        var dichVu = TaoDichVu();
        var (_, khoaBiMat) = TaoDichVuMaHoa().SinhCapKhoaRsa();
        const string noiDung = "Xin chào";

        var chuKy1 = dichVu.KyDuLieu(noiDung, khoaBiMat);
        var chuKy2 = dichVu.KyDuLieu(noiDung, khoaBiMat);

        Assert.NotEqual(chuKy1, chuKy2);
    }
}

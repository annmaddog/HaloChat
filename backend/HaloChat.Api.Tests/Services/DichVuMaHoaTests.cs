using System.Diagnostics;
using HaloChat.Security;
using Xunit;

namespace HaloChat.Api.Tests.Services;

public class DichVuMaHoaTests
{
    private static DichVuMaHoa TaoDichVu() => new();

    [Fact]
    public void SinhCapKhoaRsa_TraVeCapKhoaKhacRong()
    {
        var dichVu = TaoDichVu();

        var (khoaCongKhai, khoaBiMat) = dichVu.SinhCapKhoaRsa();

        Assert.False(string.IsNullOrWhiteSpace(khoaCongKhai));
        Assert.False(string.IsNullOrWhiteSpace(khoaBiMat));
        Assert.NotEqual(khoaCongKhai, khoaBiMat);
    }

    [Fact]
    public void SinhCapKhoaRsa_MoiLanSinhRaCapKhoaKhacNhau()
    {
        var dichVu = TaoDichVu();

        var (khoa1, _) = dichVu.SinhCapKhoaRsa();
        var (khoa2, _) = dichVu.SinhCapKhoaRsa();

        Assert.NotEqual(khoa1, khoa2);
    }

    [Fact]
    public void SinhKhoaPhienAes_TraVeDung32Byte()
    {
        var dichVu = TaoDichVu();

        var khoa = dichVu.SinhKhoaPhienAes();

        Assert.Equal(32, khoa.Length); // AES-256 = khóa 32 byte (256 bit)
    }

    [Fact]
    public void MaHoaRoiGiaiMaTinNhan_TraVeDungNoiDungGoc()
    {
        var dichVu = TaoDichVu();
        var khoaAes = dichVu.SinhKhoaPhienAes();
        const string noiDungGoc = "Chào Bình, tối nay học mật mã nhé!";

        var maHoa = dichVu.MaHoaTinNhan(noiDungGoc, khoaAes);
        var giaiMa = dichVu.GiaiMaTinNhan(maHoa.Ciphertext, maHoa.Nonce, maHoa.AuthTag, khoaAes);

        Assert.Equal(noiDungGoc, giaiMa);
    }

    [Fact]
    public void MaHoaTinNhan_CiphertextKhacVoiNoiDungGoc()
    {
        var dichVu = TaoDichVu();
        var khoaAes = dichVu.SinhKhoaPhienAes();
        const string noiDungGoc = "Nội dung bí mật";

        var maHoa = dichVu.MaHoaTinNhan(noiDungGoc, khoaAes);

        Assert.DoesNotContain(noiDungGoc, maHoa.Ciphertext);
    }

    [Fact]
    public void MaHoaTinNhan_CungNoiDungMaHoa2LanRaCiphertextKhacNhau()
    {
        // Nonce sinh ngẫu nhiên mỗi lần — cùng nội dung + cùng khóa vẫn phải
        // ra ciphertext khác nhau, tránh lộ thông tin qua so sánh ciphertext.
        var dichVu = TaoDichVu();
        var khoaAes = dichVu.SinhKhoaPhienAes();
        const string noiDung = "Xin chào";

        var maHoa1 = dichVu.MaHoaTinNhan(noiDung, khoaAes);
        var maHoa2 = dichVu.MaHoaTinNhan(noiDung, khoaAes);

        Assert.NotEqual(maHoa1.Ciphertext, maHoa2.Ciphertext);
        Assert.NotEqual(maHoa1.Nonce, maHoa2.Nonce);
    }

    [Fact]
    public void GiaiMaTinNhan_SaiKhoa_NemLoi()
    {
        var dichVu = TaoDichVu();
        var khoaDung = dichVu.SinhKhoaPhienAes();
        var khoaSai = dichVu.SinhKhoaPhienAes();
        var maHoa = dichVu.MaHoaTinNhan("Nội dung", khoaDung);

        Assert.ThrowsAny<Exception>(() => dichVu.GiaiMaTinNhan(maHoa.Ciphertext, maHoa.Nonce, maHoa.AuthTag, khoaSai));
    }

    [Fact]
    public void GiaiMaTinNhan_AuthTagBiSua_NemLoi()
    {
        // AES-GCM là mã hóa có xác thực (authenticated encryption) — sửa dù
        // chỉ 1 bit trong AuthTag phải khiến giải mã thất bại rõ ràng, không
        // âm thầm trả về dữ liệu sai.
        var dichVu = TaoDichVu();
        var khoaAes = dichVu.SinhKhoaPhienAes();
        var maHoa = dichVu.MaHoaTinNhan("Nội dung", khoaAes);
        var tagBytes = Convert.FromBase64String(maHoa.AuthTag);
        tagBytes[0] ^= 0xFF;
        var tagBiSua = Convert.ToBase64String(tagBytes);

        Assert.ThrowsAny<Exception>(() => dichVu.GiaiMaTinNhan(maHoa.Ciphertext, maHoa.Nonce, tagBiSua, khoaAes));
    }

    [Fact]
    public void MaHoaRoiGiaiMaKhoaPhien_TraVeDungKhoaGoc()
    {
        var dichVu = TaoDichVu();
        var (khoaCongKhai, khoaBiMat) = dichVu.SinhCapKhoaRsa();
        var khoaAesGoc = dichVu.SinhKhoaPhienAes();

        var khoaDaMaHoa = dichVu.MaHoaKhoaPhien(khoaAesGoc, khoaCongKhai);
        var khoaGiaiMa = dichVu.GiaiMaKhoaPhien(khoaDaMaHoa, khoaBiMat);

        Assert.Equal(khoaAesGoc, khoaGiaiMa);
    }

    [Fact]
    public void GiaiMaKhoaPhien_SaiPrivateKey_NemLoi()
    {
        var dichVu = TaoDichVu();
        var (khoaCongKhaiA, _) = dichVu.SinhCapKhoaRsa();
        var (_, khoaBiMatB) = dichVu.SinhCapKhoaRsa();
        var khoaAes = dichVu.SinhKhoaPhienAes();
        var khoaDaMaHoa = dichVu.MaHoaKhoaPhien(khoaAes, khoaCongKhaiA);

        Assert.ThrowsAny<Exception>(() => dichVu.GiaiMaKhoaPhien(khoaDaMaHoa, khoaBiMatB));
    }

    [Fact]
    public void LuongDayDu_AGuiChoBinh_BinhGiaiMaDungNoiDung()
    {
        // Mô phỏng đúng luồng spec §3: An mã hóa bằng AES rồi mã hóa khóa AES
        // bằng RSA Public Key của Bình; Bình giải mã khóa bằng Private Key
        // của mình rồi giải mã nội dung.
        var dichVu = TaoDichVu();
        var (khoaCongKhaiBinh, khoaBiMatBinh) = dichVu.SinhCapKhoaRsa();
        const string noiDungGoc = "Chào Bình, tối nay học mật mã nhé!";

        var khoaPhien = dichVu.SinhKhoaPhienAes();
        var maHoaNoiDung = dichVu.MaHoaTinNhan(noiDungGoc, khoaPhien);
        var khoaPhienDaMaHoa = dichVu.MaHoaKhoaPhien(khoaPhien, khoaCongKhaiBinh);

        var khoaPhienBinhGiaiMa = dichVu.GiaiMaKhoaPhien(khoaPhienDaMaHoa, khoaBiMatBinh);
        var noiDungBinhDoc = dichVu.GiaiMaTinNhan(maHoaNoiDung.Ciphertext, maHoaNoiDung.Nonce, maHoaNoiDung.AuthTag, khoaPhienBinhGiaiMa);

        Assert.Equal(noiDungGoc, noiDungBinhDoc);
    }

    // --- Đánh giá thời gian & kích thước dữ liệu sau mã hóa (phục vụ báo cáo) ---

    [Theory]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    public void DoThoiGianVaKichThuoc_GhiLaiKetQua(int soKyTu)
    {
        var dichVu = TaoDichVu();
        var (khoaCongKhai, khoaBiMat) = dichVu.SinhCapKhoaRsa();
        var noiDung = new string('A', soKyTu);
        var khoaAes = dichVu.SinhKhoaPhienAes();

        var dongHoMaHoa = Stopwatch.StartNew();
        var maHoa = dichVu.MaHoaTinNhan(noiDung, khoaAes);
        var khoaDaMaHoa = dichVu.MaHoaKhoaPhien(khoaAes, khoaCongKhai);
        dongHoMaHoa.Stop();

        var dongHoGiaiMa = Stopwatch.StartNew();
        var khoaGiaiMa = dichVu.GiaiMaKhoaPhien(khoaDaMaHoa, khoaBiMat);
        var noiDungGiaiMa = dichVu.GiaiMaTinNhan(maHoa.Ciphertext, maHoa.Nonce, maHoa.AuthTag, khoaGiaiMa);
        dongHoGiaiMa.Stop();

        Assert.Equal(noiDung, noiDungGiaiMa);

        var kichThuocGoc = System.Text.Encoding.UTF8.GetByteCount(noiDung);
        var kichThuocSauMaHoa = maHoa.Ciphertext.Length + maHoa.Nonce.Length + maHoa.AuthTag.Length + khoaDaMaHoa.Length;

        // Không assert thời gian/kích thước cụ thể (phụ thuộc máy chạy) — chỉ
        // ghi lại để đối chiếu thủ công khi viết phần "Đánh giá" trong báo cáo.
        // Chạy `dotnet test --filter DoThoiGianVaKichThuoc -v n` để xem output.
        Console.WriteLine(
            $"[GĐ6 benchmark] soKyTu={soKyTu} kichThuocGoc={kichThuocGoc}B " +
            $"kichThuocSauMaHoa={kichThuocSauMaHoa}B (~{(double)kichThuocSauMaHoa / Math.Max(kichThuocGoc, 1):F1}x) " +
            $"thoiGianMaHoa={dongHoMaHoa.Elapsed.TotalMilliseconds:F3}ms " +
            $"thoiGianGiaiMa={dongHoGiaiMa.Elapsed.TotalMilliseconds:F3}ms");
    }
}

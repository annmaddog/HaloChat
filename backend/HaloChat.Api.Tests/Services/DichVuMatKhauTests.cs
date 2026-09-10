using HaloChat.Api.Services;
using Xunit;

namespace HaloChat.Api.Tests.Services;

public class DichVuMatKhauTests
{
    private readonly DichVuMatKhau _dichVu = new();

    [Fact]
    public void TaoSalt_GoiHaiLan_TraVeHaiGiaTriKhacNhau()
    {
        var salt1 = _dichVu.TaoSalt();
        var salt2 = _dichVu.TaoSalt();

        Assert.NotEmpty(salt1);
        Assert.NotEqual(salt1, salt2);
    }

    [Fact]
    public void BamMatKhau_CungMatKhauVaSalt_TraVeCungKetQua()
    {
        var bam1 = _dichVu.BamMatKhau("MatKhau123", "salt-co-dinh");
        var bam2 = _dichVu.BamMatKhau("MatKhau123", "salt-co-dinh");

        Assert.Equal(bam1, bam2);
    }

    [Fact]
    public void BamMatKhau_SaltKhacNhau_TraVeKetQuaKhacNhau()
    {
        var bam1 = _dichVu.BamMatKhau("MatKhau123", "salt-1");
        var bam2 = _dichVu.BamMatKhau("MatKhau123", "salt-2");

        Assert.NotEqual(bam1, bam2);
    }

    [Fact]
    public void KiemTraMatKhau_DungMatKhau_TraVeTrue()
    {
        var salt = _dichVu.TaoSalt();
        var bam = _dichVu.BamMatKhau("MatKhau123", salt);

        Assert.True(_dichVu.KiemTraMatKhau("MatKhau123", salt, bam));
    }

    [Fact]
    public void KiemTraMatKhau_SaiMatKhau_TraVeFalse()
    {
        var salt = _dichVu.TaoSalt();
        var bam = _dichVu.BamMatKhau("MatKhau123", salt);

        Assert.False(_dichVu.KiemTraMatKhau("SaiRoi", salt, bam));
    }
}

using WebBanHang.Helpers;

namespace WebBanHang.Tests;

public class PasswordHasherTests
{
    [Fact]
    public void Hash_tao_chuoi_3_phan_theo_dinh_dang()
    {
        var hash = PasswordHasher.Hash("MatKhau123");

        var parts = hash.Split('.');
        Assert.Equal(3, parts.Length);
        Assert.Equal(100_000, int.Parse(parts[0]));
        Assert.NotEmpty(parts[1]);
        Assert.NotEmpty(parts[2]);
    }

    [Fact]
    public void Hash_cung_mat_khau_cho_ket_qua_khac_nhau_nho_muoi_ngau_nhien()
    {
        Assert.NotEqual(PasswordHasher.Hash("MatKhau123"), PasswordHasher.Hash("MatKhau123"));
    }

    [Fact]
    public void Verify_dung_mat_khau_tra_ve_true()
    {
        var hash = PasswordHasher.Hash("MatKhau123");

        Assert.True(PasswordHasher.Verify("MatKhau123", hash));
    }

    [Fact]
    public void Verify_sai_mat_khau_tra_ve_false()
    {
        var hash = PasswordHasher.Hash("MatKhau123");

        Assert.False(PasswordHasher.Verify("matkhau123", hash));
        Assert.False(PasswordHasher.Verify("MatKhau124", hash));
        Assert.False(PasswordHasher.Verify("", hash));
    }

    [Theory]
    [InlineData("")]
    [InlineData("khong-co-dau-cham")]
    [InlineData("abc.def.ghi")]
    [InlineData("0.c2FsdA==.aGFzaA==")]
    public void Verify_chuoi_bam_hong_tra_ve_false(string brokenHash)
    {
        Assert.False(PasswordHasher.Verify("MatKhau123", brokenHash));
    }

    [Fact]
    public void Hash_tu_choi_mat_khau_rong()
    {
        Assert.Throws<ArgumentException>(() => PasswordHasher.Hash("   "));
    }
}

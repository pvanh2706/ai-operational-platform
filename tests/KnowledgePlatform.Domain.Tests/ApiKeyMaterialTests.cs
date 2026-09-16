using KnowledgePlatform.Domain.Tenancy;

namespace KnowledgePlatform.Domain.Tests;

/// <summary>
/// Hình dạng khoá API của `AR-e`. Thuần tính toán, không chạm database (`IM-18`).
/// </summary>
public sealed class ApiKeyMaterialTests
{
    [Fact]
    public void Khoa_sinh_ra_tach_lai_duoc_va_hash_khop()
    {
        var (keyId, secretHash, khoaDayDu) = ApiKeyMaterial.Sinh();

        Assert.True(ApiKeyMaterial.TryTach(khoaDayDu, out var keyIdTach, out var secret));
        Assert.Equal(keyId, keyIdTach);
        Assert.True(ApiKeyMaterial.HashKhop(ApiKeyMaterial.BamBiMat(secret), secretHash));
    }

    /// <summary>
    /// 🛑 **Cái bẫy thật của hình dạng này, và nó hỏng NGẪU NHIÊN nên test một lần rất dễ
    /// xanh giả.** Phần bí mật mã base64url, mà base64url dùng `-` và `_` thay cho `+` và
    /// `/` — nên khoảng một nửa số khoá sinh ra CÓ CHỨA dấu gạch dưới bên trong.
    ///
    /// Tách bằng "lấy phần sau dấu `_` cuối cùng" hay "split rồi lấy phần tử thứ ba" đều
    /// cắt cụt đúng những khoá đó: chúng sẽ trượt xác thực, ngẫu nhiên, khoảng 50% số lần
    /// tạo khoá. Kiểu lỗi ấy đọc ra thành *"thỉnh thoảng khoá không dùng được"* — gần như
    /// không truy được nếu không có test này.
    /// </summary>
    [Fact]
    public void Bi_mat_chua_dau_gach_duoi_van_tach_dung_nguyen_ven()
    {
        Assert.True(ApiKeyMaterial.TryTach("kp_abc123_co_nhieu_gach_duoi", out var keyId, out var secret));

        Assert.Equal("abc123", keyId);
        Assert.Equal("co_nhieu_gach_duoi", secret);
    }

    /// <summary>
    /// Bản thân phép sinh cũng phải chịu được ca trên. Sinh nhiều lượt để chạm được cả
    /// những khoá có `_` bên trong — một lượt là không đủ, đó chính là chỗ xanh giả.
    /// </summary>
    [Fact]
    public void Sinh_nhieu_lan_deu_tach_lai_dung()
    {
        var coGachDuoiBenTrong = false;

        for (var i = 0; i < 200; i++)
        {
            var (keyId, secretHash, khoaDayDu) = ApiKeyMaterial.Sinh();

            Assert.True(ApiKeyMaterial.TryTach(khoaDayDu, out var idTach, out var secret),
                $"Không tách được khoá vừa sinh: {khoaDayDu}");
            Assert.Equal(keyId, idTach);
            Assert.Equal(secretHash, ApiKeyMaterial.BamBiMat(secret));

            coGachDuoiBenTrong |= secret.Contains('_');
        }

        Assert.True(coGachDuoiBenTrong,
            "200 lượt sinh mà không lượt nào có '_' trong phần bí mật — phép sinh đã đổi, " +
            "và test ngay trên không còn kiểm được ca mà nó sinh ra để kiểm.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("khong-co-dinh-dang")]
    [InlineData("kp_thieu-bi-mat")]
    [InlineData("kp__bi-mat-nhung-thieu-id")]
    [InlineData("kp_co-id_")]
    [InlineData("xx_sai-tien-to_bi-mat")]
    public void Chuoi_sai_hinh_dang_thi_tu_choi(string? raw)
    {
        Assert.False(ApiKeyMaterial.TryTach(raw, out _, out _));
    }

    [Fact]
    public void Hash_KHONG_chua_bi_mat_o_dang_doc_duoc()
    {
        const string secret = "bi-mat-rat-de-nhan-ra";

        var hash = ApiKeyMaterial.BamBiMat(secret);

        Assert.DoesNotContain(secret, hash);
        Assert.Equal(64, hash.Length);       // SHA-256 dạng hex
        Assert.Equal(hash.ToLowerInvariant(), hash);
    }

    [Fact]
    public void Hai_bi_mat_khac_nhau_cho_hai_hash_khac_nhau()
    {
        Assert.NotEqual(ApiKeyMaterial.BamBiMat("mot"), ApiKeyMaterial.BamBiMat("hai"));
        Assert.Equal(ApiKeyMaterial.BamBiMat("mot"), ApiKeyMaterial.BamBiMat("mot"));
    }

    /// <summary>
    /// Hai khoá sinh liên tiếp phải khác nhau ở CẢ HAI phần. Nghe hiển nhiên, nhưng nó
    /// chặn đúng một lỗi đã xảy ra thật trong dự án này: `Guid.CreateVersion7()` xếp theo
    /// thời gian nên 12 ký tự ĐẦU trùng nhau giữa hai giá trị sinh cùng lúc (`IM-31`).
    /// Dùng nguồn kiểu đó cho `KeyId` là tạo ra va chạm ở chính cột UNIQUE.
    /// </summary>
    [Fact]
    public void Hai_khoa_sinh_lien_tiep_khac_nhau_o_ca_hai_phan()
    {
        var (idA, hashA, _) = ApiKeyMaterial.Sinh();
        var (idB, hashB, _) = ApiKeyMaterial.Sinh();

        Assert.NotEqual(idA, idB);
        Assert.NotEqual(hashA, hashB);
    }
}

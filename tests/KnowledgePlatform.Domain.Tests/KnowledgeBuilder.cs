using KnowledgePlatform.Domain.Knowledge;

namespace KnowledgePlatform.Domain.Tests;

/// <summary>
/// Dựng vật liệu test. Giữ ở đây để mỗi test chỉ nói ĐIỀU NÓ KIỂM, không lặp
/// lại năm dòng khởi tạo bắt buộc của `required`.
/// </summary>
internal static class KnowledgeBuilder
{
    /// <summary>
    /// Mọi test dùng TenantId riêng. Không có DB nên không có chuyện lẫn dữ liệu,
    /// nhưng để nguyên thói quen đó thì test đọc lên giống hai bộ test kia.
    /// </summary>
    public static KnowledgeRecord NewRecord(
        string causeName = "Parser dưới 2.3 bỏ qua payload OTA dạng X",
        KnowledgeType type = KnowledgeType.Diagnostic) => new()
    {
        TenantId = Guid.CreateVersion7(),
        CauseName = causeName,
        Type = type,
    };

    /// <summary>
    /// Assertion mặc định nằm TRÊN thang xác minh và được tạo ở quá khứ xa, để
    /// mặc định không kích hoạt trigger nào của `V3`. Test nào muốn kích hoạt thì
    /// phải nói ra tường minh — nhờ vậy đọc test là thấy ngay nó kiểm trigger nào.
    /// </summary>
    public static Assertion NewAssertion(
        this KnowledgeRecord record,
        AssertionKind kind = AssertionKind.CauseExists,
        VerificationLevel verification = VerificationLevel.Supported,
        Origin origin = Origin.HumanAssessment,
        DateTimeOffset? createdAt = null,
        string content = "Nguyên nhân này tồn tại.")
    {
        var assertion = new Assertion
        {
            TenantId = record.TenantId,
            KnowledgeRecordId = record.Id,
            Kind = kind,
            Content = content,
            Origin = origin,
            Verification = verification,
            CreatedAt = createdAt ?? DateTimeOffset.UtcNow.AddDays(-30),
        };

        record.Assertions.Add(assertion);
        return assertion;
    }
}

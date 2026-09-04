using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace KnowledgePlatform.Infrastructure.Persistence;

/// <summary>
/// Đưa mọi <see cref="DateTimeOffset"/> về UTC trên đường GHI xuống database.
///
/// Sinh ra từ một bug thật, `IM-24` (2026-09-04): Npgsql ném
/// <c>ArgumentException: Cannot write DateTimeOffset with Offset=07:00:00 to PostgreSQL
/// type 'timestamp with time zone', only offset 0 (UTC) is supported</c>. Vì lỗi bật ra
/// từ <c>SaveChangesAsync</c>, endpoint trả <b>500</b> — một đầu vào đúng chuẩn ISO 8601
/// bị báo thành lỗi máy chủ, và bên gửi không có cách nào biết mình phải sửa gì.
///
/// <para>Chiều ĐỌC là <c>v => v</c>, cố ý không đụng vào: Postgres đã luôn trả về UTC,
/// nên chuyển thêm lần nữa chỉ là một phép biến đổi không có tác dụng nhưng vẫn có chỗ
/// để sai.</para>
///
/// ⚠ Đây là chuyển đổi BẢO TOÀN THỜI ĐIỂM, không phải làm tròn hay cắt bớt:
/// <c>2026-09-01T10:00:00+07:00</c> và <c>2026-09-01T03:00:00Z</c> là cùng một khoảnh
/// khắc. Thứ duy nhất mất đi là "người ghi nhận đang ở múi giờ nào" — và cột
/// <c>timestamptz</c> chưa bao giờ lưu được điều đó, kể cả trước khi có lớp này. Ngày
/// nào cần biết múi giờ gốc thì nó phải là một CỘT RIÊNG do bên gửi khai (đúng tinh
/// thần `IM-19`: bên gửi khai, hệ thống không suy), không phải một hy vọng rằng offset
/// tự sống sót qua tầng lưu trữ.
/// </summary>
public sealed class UtcDateTimeOffsetConverter : ValueConverter<DateTimeOffset, DateTimeOffset>
{
    public UtcDateTimeOffsetConverter()
        : base(v => v.ToUniversalTime(), v => v)
    {
    }
}

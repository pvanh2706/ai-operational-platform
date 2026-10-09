using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace KnowledgePlatform.Domain.Documents;

/// <summary>
/// Ô `K` của sơ đồ — "nội dung ĐỌC ĐƯỢC" của một tài liệu (P9, 2026-10-10). Chủ dự án chọn `N4-1`: KHÔNG thêm thư
/// viện. Nên chỉ trích được thứ thư viện chuẩn của .NET đọc được, và nói thẳng phần còn lại.
///
/// <code>
///   txt · md · csv · json · xml · log   UTF-8 NGHIÊM — sai mã hoá thì nói "không giải mã được", không đoán
///   html · htm                          bỏ thẻ, bỏ script/style, giải thực thể
///   docx                                file ZIP → word/document.xml → các đoạn w:t  (System.IO.Compression + XmlReader)
///   pdf · ảnh · còn lại                 "chua-ho-tro" — KHÔNG có OCR, KHÔNG đọc PDF (cần thư viện, §10.1)
/// </code>
///
/// 🛑 Chữ trích ra KHÔNG phải tri thức (`S6`): nó làm tài liệu ĐỌC ĐƯỢC, không làm hệ thống BIẾT gì thêm. Tri thức chỉ
/// ra đời khi có người duyệt.
///
/// ⚠ Hai chốt an toàn cho .docx (file do người ngoài nạp): <c>DtdProcessing.Prohibit</c> (chặn XML entity bomb /
/// XXE) và trần kích thước SAU giải nén (chặn zip bomb). Thiếu một trong hai là một file 30 KB làm sập tiến trình.
/// </summary>
public static class TrichChu
{
    public const string DaTrich = "da-trich";
    public const string ChuaHoTro = "chua-ho-tro";
    public const string KhongGiaiMaDuoc = "khong-giai-ma-duoc";
    public const string Loi = "loi";

    /// <summary>Trần chữ lưu lại. Quá thì cắt, và NÓI ra là đã cắt.</summary>
    public const int TranKyTu = 2_000_000;

    /// <summary>Trần dung lượng <c>word/document.xml</c> SAU giải nén — chặn zip bomb.</summary>
    public const long TranXmlDocx = 50L * 1024 * 1024;

    private static readonly HashSet<string> DuoiChuThuan = new(StringComparer.OrdinalIgnoreCase)
        { ".txt", ".md", ".csv", ".json", ".xml", ".log" };

    private static readonly HashSet<string> DuoiHtml = new(StringComparer.OrdinalIgnoreCase) { ".html", ".htm" };

    private static readonly UTF8Encoding Utf8Nghiem = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    public static KetQuaTrichChu Trich(string tenFile, ReadOnlyMemory<byte> noiDung)
    {
        var duoi = Path.GetExtension(tenFile ?? string.Empty);

        try
        {
            if (DuoiChuThuan.Contains(duoi)) return Gom(GiaiMa(noiDung.Span), null);
            if (DuoiHtml.Contains(duoi)) return Gom(BoTheHtml(GiaiMa(noiDung.Span)), null);
            if (string.Equals(duoi, ".docx", StringComparison.OrdinalIgnoreCase)) return Gom(DocDocx(noiDung), null);

            return new KetQuaTrichChu(ChuaHoTro, null,
                $"Chưa đọc được định dạng \"{duoi}\". Không có OCR, không đọc PDF — cần thư viện mới (N4-1 chọn không thêm).");
        }
        catch (DecoderFallbackException)
        {
            return new KetQuaTrichChu(KhongGiaiMaDuoc, null,
                "Nội dung không phải UTF-8 hợp lệ. Cố ý không đoán bảng mã — đoán sai là chữ vỡ trông như chữ thật.");
        }
        catch (Exception e) when (e is InvalidDataException or XmlException or InvalidOperationException)
        {
            return new KetQuaTrichChu(Loi, null, $"Không đọc được tài liệu: {e.Message}");
        }
    }

    private static KetQuaTrichChu Gom(string chu, string? ghiChu) =>
        chu.Length <= TranKyTu
            ? new KetQuaTrichChu(DaTrich, chu, ghiChu)
            : new KetQuaTrichChu(DaTrich, string.Concat(chu.AsSpan(0, TranKyTu), "…"),
                $"Đã CẮT ở {TranKyTu:N0} ký tự (bản đầy đủ {chu.Length:N0}). File gốc vẫn nguyên ở kho.");

    private static string GiaiMa(ReadOnlySpan<byte> b)
    {
        // Bỏ BOM nếu có — nó không phải nội dung.
        if (b.Length >= 3 && b[0] == 0xEF && b[1] == 0xBB && b[2] == 0xBF) b = b[3..];
        return Utf8Nghiem.GetString(b);
    }

    private static readonly Regex KhoiScriptStyle = new(@"<(script|style)\b[^>]*>.*?</\1\s*>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex XuongDongHtml = new(@"<\s*(br|/p|/div|/li|/tr|/h[1-6])\b[^>]*>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex The = new(@"<[^>]+>", RegexOptions.Compiled);
    private static readonly Regex NhieuDongTrong = new(@"\n{3,}", RegexOptions.Compiled);

    private static string BoTheHtml(string html)
    {
        var s = KhoiScriptStyle.Replace(html, string.Empty);
        s = XuongDongHtml.Replace(s, "\n");
        s = The.Replace(s, string.Empty);
        s = WebUtility.HtmlDecode(s);
        return NhieuDongTrong.Replace(s.Replace("\r\n", "\n"), "\n\n").Trim();
    }

    private const string Wml = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

    private static string DocDocx(ReadOnlyMemory<byte> noiDung)
    {
        using var zip = new ZipArchive(new MemoryStream(noiDung.ToArray()), ZipArchiveMode.Read);
        var entry = zip.GetEntry("word/document.xml")
                    ?? throw new InvalidDataException("File .docx không có word/document.xml — không phải tài liệu Word hợp lệ.");

        if (entry.Length > TranXmlDocx)
        {
            throw new InvalidDataException(
                $"word/document.xml giải nén ra {entry.Length:N0} byte, vượt trần {TranXmlDocx:N0} — từ chối (chống zip bomb).");
        }

        using var luong = entry.Open();
        using var reader = XmlReader.Create(luong, new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            IgnoreComments = true,
        });

        // ⚠ Vòng đọc TỰ tiến con trỏ ở cuối, không dùng `while (reader.Read())`: ReadElementContentAsString đã
        //   đưa con trỏ tới nút KẾ TIẾP, và một Read() nữa sẽ nhảy qua mất nút đó — hai w:t liền nhau trong một run
        //   thì mất đoạn thứ hai, im lặng.
        var sb = new StringBuilder();
        reader.MoveToContent();

        while (!reader.EOF)
        {
            if (reader.NodeType == XmlNodeType.Element && reader.NamespaceURI == Wml && reader.LocalName == "t")
            {
                sb.Append(reader.ReadElementContentAsString());
                continue;
            }

            if (reader.NodeType == XmlNodeType.Element && reader.NamespaceURI == Wml)
            {
                if (reader.LocalName == "tab") sb.Append('\t');
                else if (reader.LocalName == "br") sb.Append('\n');
                else if (reader.LocalName == "p" && reader.IsEmptyElement) sb.Append('\n');
            }
            else if (reader.NodeType == XmlNodeType.EndElement && reader.NamespaceURI == Wml && reader.LocalName == "p")
            {
                sb.Append('\n');
            }

            reader.Read();
        }

        return sb.ToString().Trim();
    }
}

/// <param name="TrangThai"><c>da-trich</c> · <c>chua-ho-tro</c> · <c>khong-giai-ma-duoc</c> · <c>loi</c>.</param>
/// <param name="NoiDungChu">Chữ trích được, hoặc <c>null</c>.</param>
/// <param name="GhiChu">Vì sao không trích được, hoặc ghi chú (ví dụ đã cắt).</param>
public sealed record KetQuaTrichChu(string TrangThai, string? NoiDungChu, string? GhiChu);

using System.IO.Compression;
using System.Text;
using KnowledgePlatform.Domain.Documents;

namespace KnowledgePlatform.Domain.Tests;

/// <summary>
/// Ô `K` — "nội dung đọc được" của tài liệu (P9), KHÔNG thêm thư viện (`N4-1`).
///
/// 🛑 Ba chỗ dễ hỏng im lặng, mỗi chỗ có test: hai đoạn chữ liền nhau trong .docx (mất đoạn sau) · file không phải
/// UTF-8 (chữ vỡ trông như chữ thật nếu đoán bảng mã) · .docx có DTD (XXE / entity bomb).
/// </summary>
public sealed class TrichChuTests
{
    private static byte[] Docx(string documentXml)
    {
        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            var e = zip.CreateEntry("word/document.xml");
            using var w = new StreamWriter(e.Open(), new UTF8Encoding(false));
            w.Write(documentXml);
        }

        return ms.ToArray();
    }

    private const string W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

    [Fact]
    public void Chu_thuan_UTF8_trich_nguyen_van_va_bo_BOM()
    {
        var bom = new byte[] { 0xEF, 0xBB, 0xBF }.Concat(Encoding.UTF8.GetBytes("Kiểm nhật ký kết nối")).ToArray();

        var kq = TrichChu.Trich("ghi-chu.txt", bom);

        Assert.Equal(TrichChu.DaTrich, kq.TrangThai);
        Assert.Equal("Kiểm nhật ký kết nối", kq.NoiDungChu);
    }

    /// <summary>Không phải UTF-8 thì NÓI RA — đoán bảng mã sai là chữ vỡ trông như chữ thật.</summary>
    [Fact]
    public void Khong_phai_UTF8_thi_KHONG_doan()
    {
        var latin1 = Encoding.Latin1.GetBytes("Café crème");

        var kq = TrichChu.Trich("cu.csv", latin1);

        Assert.Equal(TrichChu.KhongGiaiMaDuoc, kq.TrangThai);
        Assert.Null(kq.NoiDungChu);
    }

    [Fact]
    public void Html_bo_the_script_style_va_giai_thuc_the()
    {
        var html = "<html><head><style>p{color:red}</style><script>alert(1)</script></head>" +
                   "<body><p>B&#432;&#7899;c 1</p><p>Xem &amp; ki&#7875;m</p></body></html>";

        var kq = TrichChu.Trich("sop.html", Encoding.UTF8.GetBytes(html));

        Assert.Equal(TrichChu.DaTrich, kq.TrangThai);
        Assert.DoesNotContain("alert", kq.NoiDungChu);
        Assert.DoesNotContain("color", kq.NoiDungChu);
        Assert.Contains("Bước 1", kq.NoiDungChu);
        Assert.Contains("Xem & kiểm", kq.NoiDungChu);
    }

    /// <summary>🛑 Hai w:t LIỀN NHAU trong một run — bản đầu của vòng đọc nhảy qua mất đoạn thứ hai.</summary>
    [Fact]
    public void Docx_doc_DU_cac_doan_ke_ca_hai_w_t_lien_nhau()
    {
        var xml = $"""
            <w:document xmlns:w="{W}"><w:body>
              <w:p><w:r><w:t>Bước 1:</w:t><w:t xml:space="preserve"> xem nhật ký</w:t></w:r></w:p>
              <w:p/>
              <w:p><w:r><w:t>Bước 2</w:t><w:tab/><w:t>phân quyền</w:t></w:r></w:p>
            </w:body></w:document>
            """;

        var kq = TrichChu.Trich("quy-trinh.docx", Docx(xml));

        Assert.Equal(TrichChu.DaTrich, kq.TrangThai);
        Assert.Contains("Bước 1: xem nhật ký", kq.NoiDungChu);
        Assert.Contains("Bước 2\tphân quyền", kq.NoiDungChu);
    }

    /// <summary>File do người NGOÀI nạp: DTD bị chặn (XXE / entity bomb) — báo lỗi chứ không làm sập tiến trình.</summary>
    [Fact]
    public void Docx_co_DTD_thi_TU_CHOI()
    {
        var xml = $"""<?xml version="1.0"?><!DOCTYPE d [<!ENTITY x "boom">]><w:document xmlns:w="{W}"><w:body><w:p><w:r><w:t>&x;</w:t></w:r></w:p></w:body></w:document>""";

        var kq = TrichChu.Trich("doc.docx", Docx(xml));

        Assert.Equal(TrichChu.Loi, kq.TrangThai);
    }

    [Fact]
    public void Docx_khong_co_document_xml_thi_bao_loi()
    {
        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true)) zip.CreateEntry("rac.txt");

        var kq = TrichChu.Trich("hong.docx", ms.ToArray());

        Assert.Equal(TrichChu.Loi, kq.TrangThai);
    }

    [Theory]
    [InlineData("ban-scan.pdf")]
    [InlineData("anh-chup.png")]
    public void Pdf_va_anh_noi_thang_la_CHUA_HO_TRO(string ten)
    {
        var kq = TrichChu.Trich(ten, new byte[] { 1, 2, 3 });

        Assert.Equal(TrichChu.ChuaHoTro, kq.TrangThai);
        Assert.Contains("N4-1", kq.GhiChu);
    }
}

using KnowledgePlatform.Domain.Evidence;
using KnowledgePlatform.Domain.Knowledge;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace KnowledgePlatform.Infrastructure.Tests;

/// <summary>
/// Liên kết dẫn chứng KHÔNG được trỏ qua ranh giới tenant — và chỗ chặn là database, không phải code.
///
/// 🛑 Vì sao RLS không đủ ở đây: policy <c>WITH CHECK</c> chỉ kiểm <c>TenantId</c> của CHÍNH dòng đang
/// ghi. Một dòng <c>assertion_evidence</c> mang TenantId của khách A thì qua cổng, kể cả khi
/// <c>EvidenceItemId</c> của nó là id một mẩu của khách B. Khoá ngoại một cột cũng không chặn — mẩu đó
/// TỒN TẠI. Chỉ khoá ngoại GHÉP <c>(TenantId, EvidenceItemId)</c> mới chặn (P3, 2026-10-10).
/// </summary>
public sealed class DanChungChoTenantTests(TestDatabaseFixture db) : IClassFixture<TestDatabaseFixture>
{
    private static EvidenceItem Mau(Guid tenantId) => new()
    {
        TenantId = tenantId,
        Content = "Da kiem room mapping",
        SourceReference = $"test:{Guid.CreateVersion7()}",
    };

    private async Task<Guid> TaoMauAsync(Guid tenantId)
    {
        await using var ctx = db.NewContext(new FixedTenantContext(tenantId));
        var mau = Mau(tenantId);
        ctx.EvidenceItems.Add(mau);
        await ctx.SaveChangesAsync();
        return mau.Id;
    }

    private static KnowledgeRecord RecordTroToi(Guid tenantId, Guid mauId)
    {
        var record = new KnowledgeRecord { TenantId = tenantId, CauseName = "Thử", Type = KnowledgeType.Diagnostic };
        var a = new Assertion
        {
            TenantId = tenantId,
            KnowledgeRecordId = record.Id,
            Kind = AssertionKind.CauseExists,
            Content = "Thử",
            Origin = Origin.AiInference,
            Verification = VerificationLevel.Speculative,
        };
        a.EvidenceLinks.Add(new AssertionEvidence
        {
            TenantId = tenantId,
            AssertionId = a.Id,
            EvidenceItemId = mauId,
            Relation = EvidenceRelation.Support,
        });
        record.Assertions.Add(a);
        return record;
    }

    /// <summary>Đối chứng: trỏ vào mẩu CỦA MÌNH thì ghi được — để test dưới không đỏ vì một lý do khác.</summary>
    [Fact]
    public async Task Tro_vao_mau_cua_CHINH_MINH_thi_ghi_duoc()
    {
        var a = Guid.CreateVersion7();
        var mauA = await TaoMauAsync(a);

        await using var ctx = db.NewContext(new FixedTenantContext(a));
        ctx.KnowledgeRecords.Add(RecordTroToi(a, mauA));
        await ctx.SaveChangesAsync();
    }

    [Fact]
    public async Task Tro_vao_mau_cua_KHACH_KHAC_thi_database_TU_CHOI()
    {
        var a = Guid.CreateVersion7();
        var b = Guid.CreateVersion7();
        var mauCuaB = await TaoMauAsync(b);

        await using var ctx = db.NewContext(new FixedTenantContext(a));
        ctx.KnowledgeRecords.Add(RecordTroToi(a, mauCuaB));

        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => ctx.SaveChangesAsync());
        var pg = Assert.IsType<PostgresException>(ex.InnerException);
        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, pg.SqlState);
    }

    /// <summary>
    /// Restrict: xoá một mẩu đang được trích thì database TỪ CHỐI. Trước P3 bảng này không có khoá
    /// ngoại nào sang evidence_item, nên thực hành "xoá evidence rồi nạp lại" (`IM-36`) sẽ để lại liên
    /// kết mồ côi — im lặng — ngay khi bảng bắt đầu có dòng.
    /// </summary>
    [Fact]
    public async Task Xoa_mau_dang_duoc_trich_thi_database_TU_CHOI()
    {
        var a = Guid.CreateVersion7();
        var mauA = await TaoMauAsync(a);

        await using (var ctx = db.NewContext(new FixedTenantContext(a)))
        {
            ctx.KnowledgeRecords.Add(RecordTroToi(a, mauA));
            await ctx.SaveChangesAsync();
        }

        await using var xoa = db.NewContext(new FixedTenantContext(a));
        var ex = await Assert.ThrowsAsync<PostgresException>(() =>
            xoa.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM kp.evidence_item WHERE \"Id\" = {mauA}"));

        // ⚠ RESTRICT báo 23001 (restrict_violation), KHÔNG phải 23503 (foreign_key_violation) — bản
        //   đầu của test này đòi 23503 và đỏ, trong khi database đang làm đúng việc của nó.
        Assert.Equal(PostgresErrorCodes.RestrictViolation, ex.SqlState);
    }
}

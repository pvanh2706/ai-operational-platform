using KnowledgePlatform.Domain.Knowledge;

namespace KnowledgePlatform.Domain.Tests;

/// <summary>
/// Vòng đời của một KnowledgeRecord và hành động DUYỆT — `V2`, `V4`, `S7`, `D4`.
///
/// Vì sao bộ test này tồn tại: trước nó, 100% test của dự án cắm vào PostgreSQL.
/// Nghĩa là luật domain thuần — thứ sinh ra từ 23 quyết định của Workstream 04 và
/// KHÔNG có một dòng SQL nào — chưa từng được kiểm lấy một lần. Nó chỉ được đọc.
///
/// `S7` là quyết định dễ mất nhất trong nhóm này: "duyệt nội dung và mở quyền xem
/// là MỘT hành động, và log CẢ HAI". Một lần refactor vô tình tách đôi hai việc đó
/// sẽ không làm hỏng bất cứ thứ gì nhìn thấy được — chỉ làm mất dấu vết ai đã mở
/// quyền xem cho tri thức nào. Đó đúng là loại hỏng mà chỉ test mới bắt được.
/// </summary>
public sealed class KnowledgeRecordLifecycleTests
{
    // =========================================================================
    //  Trạng thái khởi đầu
    // =========================================================================

    /// <summary>
    /// `V2`: record sinh ra ở Draft. `S7`: hệ thống KHÔNG BAO GIỜ tự mở quyền xem —
    /// nên trước khi có người duyệt, phạm vi xem phải là TRỐNG, không phải một
    /// giá trị mặc định nào đó.
    /// </summary>
    [Fact]
    public void Record_moi_nam_o_Draft_va_chua_co_pham_vi_xem()
    {
        var record = KnowledgeBuilder.NewRecord();

        Assert.Equal(StoredLifecycleState.Draft, record.Lifecycle);
        Assert.Null(record.VisibilityScope);
        Assert.Null(record.LastApproval);
        Assert.Equal("DRAFT", record.DisplayState);
    }

    // =========================================================================
    //  Duyệt — S7
    // =========================================================================

    /// <summary>
    /// `S7` đòi MỘT hành động làm cả hai việc. Test này ghim cả hai vế cùng lúc:
    /// sau một lời gọi Approve, record vừa Active vừa có phạm vi xem — không có
    /// trạng thái trung gian nào mà nội dung đã duyệt còn quyền xem thì chưa.
    /// </summary>
    [Fact]
    public void Duyet_lan_dau_vua_dat_Active_vua_mo_quyen_xem_trong_mot_hanh_dong()
    {
        var record = KnowledgeBuilder.NewRecord();

        record.Approve(approvedByActor: "an.pham", visibilityScope: VisibilityScope.NoiBo, xacNhanDuLieuThuongMai: true);

        Assert.Equal(StoredLifecycleState.Active, record.Lifecycle);
        Assert.Equal(VisibilityScope.NoiBo, record.VisibilityScope);
        Assert.Equal("ACTIVE", record.DisplayState);
    }

    /// <summary>
    /// `S7` chỗ chứa thứ ba: ai mở rộng quyền xem, khi nào, TỪ ĐÂU TỚI ĐÂU.
    /// Lần duyệt đầu thì "từ đâu" là NULL — record còn ở Draft, chưa ai xem được.
    /// Ghi NULL chứ không ghi chuỗi rỗng: hai thứ đó nghĩa khác nhau khi sau này
    /// đọc lại log duyệt.
    /// </summary>
    [Fact]
    public void Lan_duyet_dau_ghi_pham_vi_truoc_la_NULL()
    {
        var record = KnowledgeBuilder.NewRecord();

        record.Approve("an.pham", VisibilityScope.NoiBo, xacNhanDuLieuThuongMai: true, reason: "Đã đối chiếu với source code.");

        var approval = Assert.IsType<KnowledgeApproval>(record.LastApproval);
        Assert.Equal("an.pham", approval.ApprovedByActor);
        Assert.Null(approval.VisibilityScopeBefore);
        Assert.Equal(VisibilityScope.NoiBo, approval.VisibilityScopeAfter);
        Assert.Equal("Đã đối chiếu với source code.", approval.Reason);
    }

    /// <summary>
    /// Lần duyệt thứ hai phải ghi được ĐƯỜNG ĐI của quyền xem, không chỉ đích đến.
    /// `S7` nói mở rộng phạm vi phải là hành vi tường minh của người thấy được tất
    /// cả nguồn — muốn kiểm lại điều đó về sau thì phải biết nó đã mở TỪ đâu.
    /// </summary>
    [Fact]
    public void Duyet_lan_hai_ghi_lai_duong_di_cua_quyen_xem()
    {
        var record = KnowledgeBuilder.NewRecord();

        record.Approve("an.pham", VisibilityScope.NoiBo, xacNhanDuLieuThuongMai: true);
        record.Approve("linh.tran", VisibilityScope.ToanCongTy, xacNhanDuLieuThuongMai: true, reason: "Không còn chi tiết nội bộ.");

        var approval = Assert.IsType<KnowledgeApproval>(record.LastApproval);
        Assert.Equal("linh.tran", approval.ApprovedByActor);
        Assert.Equal(VisibilityScope.NoiBo, approval.VisibilityScopeBefore);
        Assert.Equal(VisibilityScope.ToanCongTy, approval.VisibilityScopeAfter);
        Assert.Equal(VisibilityScope.ToanCongTy, record.VisibilityScope);
    }

    /// <summary>
    /// `D4`: người CÔNG NHẬN, không phải AI. Một lời duyệt không có tên người duyệt
    /// thì không phải là duyệt — nó là một dòng log vô chủ. Ném ngay tại chỗ, đừng
    /// để nó lặng lẽ nằm trong DB rồi vài tháng sau mới phát hiện không truy được ai.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Duyet_ma_khong_co_ten_nguoi_duyet_thi_nem(string? actor)
    {
        var record = KnowledgeBuilder.NewRecord();

        Assert.ThrowsAny<ArgumentException>(() => record.Approve(actor!, VisibilityScope.NoiBo, xacNhanDuLieuThuongMai: true));
    }

    /// <summary>
    /// `S7`: mặc định là HẸP NHẤT, và hệ thống không bao giờ tự chọn hộ.
    ///
    /// ⚠ **Test này ĐÃ ĐỔI ĐỐI TƯỢNG 2026-09-15, không phải bị nới lỏng.** Bản cũ nhận
    /// `string?` và kiểm ba ca `null` / `""` / `"   "`. Sau khi `AR-a` khoá tập giá trị và
    /// tham số siết sang <see cref="VisibilityScope"/>, **cả ba ca đó không còn viết ra
    /// được** — trình biên dịch chặn trước khi chạy, và chặn lúc biên dịch mạnh hơn test.
    ///
    /// Nhưng việc siết kiểu KHÔNG đóng hết lỗ: `(VisibilityScope)99` vẫn ép kiểu được và
    /// enum không tự chặn. Đó là lỗ CÒN LẠI, và là thứ test này kiểm bây giờ — nếu không
    /// thì việc siết kiểu chỉ đổi một lỗ hổng dễ thấy thành một lỗ hổng khó thấy hơn.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(99)]
    [InlineData(-1)]
    public void Duyet_voi_pham_vi_xem_ngoai_tap_da_chot_thi_nem(int giaTriLa)
    {
        var record = KnowledgeBuilder.NewRecord();

        Assert.ThrowsAny<ArgumentException>(
            () => record.Approve("an.pham", (VisibilityScope)giaTriLa, xacNhanDuLieuThuongMai: true));
    }

    /// <summary>
    /// Ba giá trị đã chốt thì PHẢI đi qua được — kẻo phép kiểm ở trên siết quá tay mà
    /// không ai biết, và cả ba test còn lại vẫn xanh vì chúng chỉ dùng một giá trị.
    /// </summary>
    [Theory]
    [InlineData(VisibilityScope.RiengTu)]
    [InlineData(VisibilityScope.NoiBo)]
    [InlineData(VisibilityScope.ToanCongTy)]
    public void Ba_gia_tri_da_chot_deu_duyet_duoc(VisibilityScope scope)
    {
        var record = KnowledgeBuilder.NewRecord();

        record.Approve("an.pham", scope, xacNhanDuLieuThuongMai: true);

        Assert.Equal(scope, record.VisibilityScope);
    }

    /// <summary>
    /// Duyệt hỏng thì KHÔNG được để lại nửa hành động. `S7` gọi đây là một hành
    /// động — một hành động thất bại phải không đổi gì cả.
    /// </summary>
    [Fact]
    public void Duyet_that_bai_thi_khong_doi_gi_ca()
    {
        var record = KnowledgeBuilder.NewRecord();
        record.Approve("an.pham", VisibilityScope.NoiBo, xacNhanDuLieuThuongMai: true);

        // Giá trị ngoài tập đã chốt — ca duy nhất còn làm `Approve` ném sau khi siết kiểu.
        Assert.ThrowsAny<ArgumentException>(() => record.Approve("linh.tran", (VisibilityScope)99, xacNhanDuLieuThuongMai: true));

        Assert.Equal(VisibilityScope.NoiBo, record.VisibilityScope);
        Assert.Equal("an.pham", record.LastApproval!.ApprovedByActor);
    }

    // =========================================================================
    //  Rút tri thức — V4 ca (a)
    // =========================================================================

    /// <summary>
    /// `V4` ca (a) là lý do tồn tại của việc TÁCH HAI TRỤC (lifecycle ở mức record,
    /// verification ở mức assertion). Tri thức vẫn ĐÚNG nhưng không còn ai gặp —
    /// ví dụ không còn khách nào chạy bản dưới 2.3. Nếu chỉ có một trục, ta buộc
    /// phải gắn INVALIDATED cho một phát biểu vẫn đúng, tức là nói dối trong dữ liệu.
    ///
    /// Test này ghim đúng chỗ đó: Deprecate KHÔNG được đụng vào verification.
    /// </summary>
    [Fact]
    public void Rut_tri_thuc_khong_lam_doi_muc_xac_minh_cua_assertion()
    {
        var record = KnowledgeBuilder.NewRecord();
        var assertion = record.NewAssertion(verification: VerificationLevel.Verified);
        record.Approve("an.pham", VisibilityScope.NoiBo, xacNhanDuLieuThuongMai: true);

        record.Deprecate();

        Assert.Equal(StoredLifecycleState.Deprecated, record.Lifecycle);
        Assert.Equal(VerificationLevel.Verified, assertion.Verification);
        Assert.Equal("DEPRECATED", record.DisplayState);
    }

    /// <summary>
    /// Rút tri thức KHÔNG thu hồi quyền xem — đó là hai việc khác nhau, và `S7`
    /// nói việc đổi quyền xem phải là hành vi tường minh của người duyệt. Nếu ngày
    /// nào đó dự án muốn Deprecate kéo theo thu hồi quyền xem thì đó là một QUYẾT
    /// ĐỊNH DOMAIN mới, và test này sẽ đỏ để bắt phải ghi quyết định đó xuống.
    /// </summary>
    [Fact]
    public void Rut_tri_thuc_khong_tu_dong_thu_hoi_quyen_xem()
    {
        var record = KnowledgeBuilder.NewRecord();
        record.Approve("an.pham", VisibilityScope.NoiBo, xacNhanDuLieuThuongMai: true);

        record.Deprecate();

        Assert.Equal(VisibilityScope.NoiBo, record.VisibilityScope);
    }

    // =========================================================================
    //  Ràng buộc cấu trúc — thứ giữ cho S7/V2 không bị một refactor gỡ mất
    // =========================================================================

    /// <summary>
    /// `S7`: "hệ thống KHÔNG BAO GIỜ tự mở [quyền xem]". Câu đó chỉ đứng vững nếu
    /// KHÔNG CÓ đường nào đặt Lifecycle / VisibilityScope / LastApproval ngoài
    /// Approve và Deprecate. Mở một trong ba setter đó thành public là gỡ mất `S7`
    /// mà không có test nào khác đỏ — nên chính nó phải là một test.
    ///
    /// Đây là test về HÌNH DẠNG của kiểu, cố ý. Nó bắt được thứ mà test hành vi
    /// không bắt được: một cánh cửa vừa được mở ra nhưng chưa ai đi qua.
    /// </summary>
    [Theory]
    [InlineData(nameof(KnowledgeRecord.Lifecycle))]
    [InlineData(nameof(KnowledgeRecord.VisibilityScope))]
    [InlineData(nameof(KnowledgeRecord.LastApproval))]
    public void Khong_co_duong_nao_dat_state_tu_ben_ngoai(string propertyName)
    {
        var property = typeof(KnowledgeRecord).GetProperty(propertyName);
        var setter = property!.SetMethod;

        Assert.False(
            setter is { IsPublic: true },
            $"{propertyName} vừa có setter public. S7 nói duyệt nội dung và mở quyền xem là " +
            "MỘT hành động đi qua Approve(), và hệ thống không bao giờ tự mở quyền xem. " +
            "Một setter public làm câu đó thành không đúng nữa — và không test nào khác " +
            "trong dự án sẽ đỏ vì chuyện đó.");
    }

    // =====================================================================
    //  AR-n (B2b-1) — cổng dữ liệu THƯƠNG MẠI ở khâu duyệt
    // =====================================================================

    /// <summary>
    /// 🛑 Chưa xác nhận đã soát dữ liệu thương mại thì KHÔNG duyệt được, và record **không đổi
    /// trạng thái gì cả** — không có nửa vời.
    ///
    /// Vì sao cổng nằm ở đây chứ không ở khâu nạp: dữ liệu thương mại (giá hợp đồng theo năm,
    /// hạn hợp đồng, ý định ngừng dùng — ca thật `ES-346608#comment-803265`) **không có hình
    /// dạng nào** để bắt bằng luật. Không có hình dạng thì không có luật, và không có luật thì
    /// phải có người — mà chỗ duy nhất chắc chắn có người là lượt DUYỆT.
    /// </summary>
    [Fact]
    public void Duyet_ma_chua_xac_nhan_du_lieu_thuong_mai_thi_nem()
    {
        var record = KnowledgeBuilder.NewRecord();

        Assert.Throws<InvalidOperationException>(
            () => { record.Approve("an.pham", VisibilityScope.NoiBo, xacNhanDuLieuThuongMai: false); });

        Assert.Equal(StoredLifecycleState.Draft, record.Lifecycle);
        Assert.Null(record.LastApproval);
    }

    /// <summary>
    /// Xác nhận rồi thì lời khai được LƯU LẠI cùng lượt duyệt. Không lưu thì nó chỉ là một ô
    /// tích rồi biến mất — và `M2` phần (c) đã dạy đúng điều đó: một lượt duyệt không để lại
    /// bằng chứng thì không phân biệt được với một lượt chưa ai làm.
    /// </summary>
    [Fact]
    public void Xac_nhan_roi_thi_loi_khai_duoc_LUU_cung_luot_duyet()
    {
        var record = KnowledgeBuilder.NewRecord();

        record.Approve("an.pham", VisibilityScope.NoiBo, xacNhanDuLieuThuongMai: true);

        Assert.True(record.LastApproval!.CommercialDataConfirmed);
        Assert.Equal("an.pham", record.LastApproval.ApprovedByActor);
    }
}

namespace KnowledgePlatform.Infrastructure.Sop;

/// <summary>
/// JSON Schema cho <see cref="Domain.Sop.SopDraft"/>, dùng làm `output_config.format`.
///
/// ⚠ VÌ SAO SCHEMA VIẾT TAY CHỨ KHÔNG SINH TỪ TYPE: schema này phải khớp với thứ mà
/// `scripts/jira-export/nhom_sop.py --kiem-cay` đọc được — bộ eval đã có 11 phép đột biến
/// và là thứ đắt nhất đang có. Sinh schema tự động từ C# type sẽ cho ra tên trường theo
/// quy ước của bộ sinh, và lệch một chữ là bộ eval không đọc được. Viết tay thì nó lệch
/// TO và lệch NGAY (test bắt), thay vì lệch nhỏ và lệch âm thầm.
///
/// ⚠ KHÔNG dùng `$defs`/`$ref`, dù nó làm schema gọn hơn. Lý do ĐÃ ĐỔI 2026-09-10
/// (`IM-28`): structured outputs CÓ hỗ trợ `$defs`/`$ref` — tra được trong tài liệu, không
/// còn là ẩn số. Giữ cách viết thẳng năm lần vì một lý do khác và mạnh hơn: từ `AR-q`, năm
/// ô KHÔNG còn giống nhau nữa — mỗi ô mang định nghĩa và ca biên riêng, nên gộp chúng vào
/// một `$ref` chung là làm mất đúng thứ khiến chúng phân biệt được.
///
/// ⚠ `description` của từng trường KHÔNG phải chú thích cho người đọc — model đọc chúng.
/// Mỗi câu ở đây đến từ một chỗ đã đo khi dựng tay hai bản nháp đầu (`docs/11`), và từ
/// 2026-09-11, năm ô của `phanBoBuocKiemDuocGhiLai` mang định nghĩa + ca biên của `AR-q`:
/// chúng viết ra để gỡ BA chỗ mà hai ngữ cảnh đọc đã xếp khác nhau, mỗi chỗ một nhầm lẫn
/// khác nhau — kết luận bị đọc thành bước kiểm · nơi GHI bị đọc thành nơi KIỂM · độ chắc
/// của nguyên nhân rò sang một ô nói về việc GHI. Đừng rút gọn chúng cho đẹp.
///
/// 🛑 LUẬT KHI SỬA NĂM Ô ĐÓ: ca biên phải là HÌNH DẠNG, không được là câu nguyên văn của
/// một ticket. Bản đầu ngày 2026-09-11 vi phạm chính điều này và phép đo bắt được: một
/// ngữ cảnh xử ca khó bằng lý do "ví dụ đích danh thắng ca biên tổng quát" — tức nó NHẬN
/// RA VÍ DỤ chứ không ÁP DỤNG LUẬT. Định nghĩa trích corpus sẽ cho điểm khớp cao giả trên
/// đúng nhóm dùng để viết nó, rồi tụt ở 18 nhóm còn lại, và con số đầu tiên là con số
/// trông giống tin tốt. Đây là cùng một cái bẫy mà `SecretShapeScanner` đã ghi: luật sửa
/// theo chính dữ liệu dùng để đo nó thì recall đo được là cận trên, không phải ước lượng.
/// </summary>
/// <remarks>
/// ⚠ Đổi từ `internal` sang `public` ngày 2026-09-10, có lý do và ghi ra kẻo sau này ai đó
/// đóng lại: `SoanNhapRunner --xuat-payload` phải xuất được schema CÙNG payload, vì một
/// ngữ cảnh đọc file đó cần biết hình dạng đầu ra — và `description` của từng trường là
/// một phần của prompt thật, không phải chú thích. Đây là một hằng số chuỗi công khai
/// (chính nó được gửi ra API mỗi lượt gọi), nên mở ra không mất gì.
/// </remarks>
public static class SopDraftSchema
{
    public const string Json = """
        {
          "type": "object",
          "additionalProperties": false,
          "required": ["nhom", "soCaseDungSau", "trieuChungVao", "buocKiem", "buocSua",
                       "buocXacNhan", "phanBoBuocKiemDuocGhiLai", "khoangTrongPhaiBiet"],
          "properties": {
            "nhom": {
              "type": "string",
              "description": "Tên nhóm nguyên nhân, COPY nguyên văn từ đầu vào."
            },
            "soCaseDungSau": {
              "type": "integer",
              "description": "Số ticket của nhóm, bằng đúng số ticket được đưa vào."
            },
            "trieuChungVao": {
              "type": "array",
              "description": "Các câu khách nói hoặc màn hình báo, viết bằng lời người dùng.",
              "items": {
                "type": "object",
                "additionalProperties": false,
                "required": ["moTa", "case"],
                "properties": {
                  "moTa": { "type": "string" },
                  "case": { "type": "array", "items": { "type": "string" } }
                }
              }
            },
            "buocKiem": {
              "type": "array",
              "description": "Các bước kiểm, xếp theo thứ tự nên làm. Rẻ và phủ nhiều thì trước.",
              "items": {
                "type": "object",
                "additionalProperties": false,
                "required": ["ma", "cauHoi", "noiXem", "quyenCan", "nhanh", "nguyenVan",
                             "bayDaVapThat"],
                "properties": {
                  "ma": {
                    "type": "string",
                    "description": "Mã ngắn, ví dụ K1, K2. Nhánh của bước khác trỏ tới mã này."
                  },
                  "cauHoi": {
                    "type": "string",
                    "description": "Câu hỏi hoặc phép thử cụ thể. KHÔNG phải một cái nhãn như 'kiểm tra phân quyền'."
                  },
                  "noiXem": {
                    "type": "string",
                    "description": "Mở màn hình NÀO. Hai nguyên nhân có thể cùng triệu chứng mà khác màn quản trị."
                  },
                  "quyenCan": {
                    "type": ["string", "null"],
                    "description": "Quyền cần có để LÀM được bước này, nếu nó có thể bị chặn bởi phân quyền. null nếu ai cũng làm được."
                  },
                  "nhanh": {
                    "type": "array",
                    "items": {
                      "type": "object",
                      "additionalProperties": false,
                      "required": ["quanSat", "diToi", "case", "chungCu"],
                      "properties": {
                        "quanSat": { "type": "string", "description": "Thấy gì." },
                        "diToi": {
                          "type": "string",
                          "description": "Mã bước tiếp: một mã trong buocKiem hoặc buocSua, hoặc BANG (sang bảng tra), K-XN (sang bước xác nhận), NGOAI-PHAM-VI."
                        },
                        "case": {
                          "type": "array",
                          "items": { "type": "string" },
                          "description": "Mã ticket chống lưng nhánh này. RỖNG LÀ HỢP LỆ và là thông tin — đừng điền cho đủ."
                        },
                        "chungCu": {
                          "type": "string",
                          "enum": ["evidence-noi-ro", "toi-suy-ra"],
                          "description": "evidence-noi-ro chỉ khi ticket GHI RÕ bước kiểm. Chỉ ghi kết luận thì là toi-suy-ra."
                        }
                      }
                    }
                  },
                  "nguyenVan": {
                    "type": ["string", "null"],
                    "description": "Câu nguyên văn trong ticket mà bước kiểm này rút ra từ đó. null nếu không ticket nào ghi."
                  },
                  "bayDaVapThat": {
                    "type": ["string", "null"],
                    "description": "Chỗ dễ đi sai đã thấy trong ticket. null nếu không có."
                  }
                }
              }
            },
            "bangTraMaLoi": {
              "type": ["object", "null"],
              "additionalProperties": false,
              "required": ["dong"],
              "description": "CHỈ dùng khi các nguyên nhân phân biệt nhau bằng MÃ LỖI. null nếu nhóm có hình cây phân nhánh.",
              "properties": {
                "dong": {
                  "type": "array",
                  "items": {
                    "type": "object",
                    "additionalProperties": false,
                    "required": ["ncc", "maLoi", "truongBiTuChoi", "diToi", "case", "chungCu"],
                    "properties": {
                      "ncc": { "type": "string", "description": "Nhà cung cấp. Khoá một dòng là CẶP (ncc, maLoi)." },
                      "maLoi": { "type": "string" },
                      "truongBiTuChoi": { "type": "string" },
                      "diToi": { "type": "string" },
                      "case": { "type": "array", "items": { "type": "string" } },
                      "chungCu": { "type": "string", "enum": ["evidence-noi-ro", "toi-suy-ra"] }
                    }
                  }
                }
              }
            },
            "buocSua": {
              "type": "array",
              "items": {
                "type": "object",
                "additionalProperties": false,
                "required": ["ma", "viec", "case"],
                "properties": {
                  "ma": { "type": "string", "description": "Mã ngắn, ví dụ S1. Nhánh trỏ tới mã này." },
                  "viec": { "type": "string" },
                  "case": { "type": "array", "items": { "type": "string" } }
                }
              }
            },
            "buocXacNhan": {
              "type": ["object", "null"],
              "additionalProperties": false,
              "required": ["viec", "case"],
              "description": "Điều kiện đóng. Phải là thứ QUAN SÁT ĐƯỢC, không phải 'hết báo lỗi'.",
              "properties": {
                "viec": { "type": "string" },
                "case": { "type": "array", "items": { "type": "string" } }
              }
            },
            "phanBoBuocKiemDuocGhiLai": {
              "type": "object",
              "additionalProperties": false,
              "required": ["ghi-ro-buoc-kiem", "suy-ra-duoc-nhung-khong-ghi",
                           "chi-ghi-ket-luan", "buoc-kiem-ngoai-ticket",
                           "khong-phai-chan-doan"],
              "description": "Câu hỏi mà năm ô này trả lời: HỒ SƠ TICKET có ghi lại BƯỚC KIỂM hay không. KHÔNG phải: nguyên nhân có chắc hay không — độ chắc đã có chỗ riêng là `chungCu` của từng nhánh. Một ticket ghi rõ bước kiểm mà kết luận vẫn là phỏng đoán thì VẪN thuộc ô `ghi-ro-buoc-kiem`. MỖI ticket vào ĐÚNG MỘT ô, xét theo THỨ TỰ sau và dừng ở ô khớp đầu tiên (các ô chồng lấn thật, thứ tự là thứ gỡ chồng lấn): 1. khong-phai-chan-doan · 2. buoc-kiem-ngoai-ticket · 3. ghi-ro-buoc-kiem · 4. suy-ra-duoc-nhung-khong-ghi · 5. chi-ghi-ket-luan. PHÉP CỘNG PHẢI KHỚP: tổng `so` của năm ô phải bằng đúng soCaseDungSau, không ticket nào được đếm hai lần, không ticket nào bị bỏ quên. Đây là chỗ dễ sai nhất của cả bản nháp.",
              "properties": {
                "ghi-ro-buoc-kiem": {
                  "type": "object", "additionalProperties": false, "required": ["so", "case"],
                  "description": "Ticket ghi lại ít nhất MỘT bước kiểm LẶP LẠI ĐƯỢC: nói ra mở chỗ nào và xem cái gì, đủ để người CHƯA BIẾT đáp án làm theo được. Dạng ĐẠT: ai đó thuật lại một thao tác đã làm KÈM thứ nhìn thấy — 'làm thao tác X thì hệ thống trả về thông báo Y', 'đăng nhập chỗ Y bằng tài khoản Z thì chỉ thấy được phạm vi cũ'. Dạng KHÔNG ĐẠT: câu nêu KẾT LUẬN ('do chưa làm X nên không chạy được'), dù nó gọi đúng tên trường hay tên màn hình; và câu hỏi lấy thông tin để tra cứu ('mình đang làm cho phòng/đơn nào?'), vì hỏi để biết ĐỐI TƯỢNG không phải là một phép thử. AI làm bước kiểm KHÔNG quan trọng: khách tự làm theo hướng dẫn rồi thuật lại thứ nhìn thấy vẫn tính, miễn ticket ghi được hỏi gì / xem ở đâu / thấy gì. NHƯNG bước kiểm là bước ĐỂ CHẨN ĐOÁN, không phải bước XÁC NHẬN SAU KHI SỬA: 'làm theo hướng dẫn rồi thấy đã chạy được' là bước xác nhận — schema có chỗ riêng cho nó ở `buocXacNhan` — và nó KHÔNG đưa ticket vào ô này. Ô này nói về việc GHI, nên một bước kiểm được ghi rõ mà kết luận cuối vẫn là phỏng đoán thì VẪN thuộc ô này.",
                  "properties": { "so": { "type": "integer" }, "case": { "type": "array", "items": { "type": "string" } } }
                },
                "suy-ra-duoc-nhung-khong-ghi": {
                  "type": "object", "additionalProperties": false, "required": ["so", "case"],
                  "description": "Không bước kiểm nào được viết ra, NHƯNG từ thứ có trong ticket suy ra được rằng một bước kiểm BẮT BUỘC đã xảy ra, VÀ suy ra được nó là gì. CA BIÊN: một câu kết luận kèm ảnh chụp CHỖ CẦN XEM để chẩn đoán (màn cấu hình, màn quản trị) thì vào ô NÀY chứ không vào `ghi-ro-buoc-kiem` — tấm ảnh cho thấy ĐÁP ÁN, không cho thấy CÁCH HỎI. Phân biệt với ô `chi-ghi-ket-luan`: ảnh TRIỆU CHỨNG do khách gửi (màn báo lỗi, thông báo đỏ) KHÔNG nâng ticket lên ô này, vì nó cho thấy thứ đã hỏng chứ không cho thấy chỗ đã tra. Và điều kiện 'suy ra được NÓ LÀ GÌ' phải thoả: đoán rằng chắc chắn đã có ai đó kiểm cái gì đó là chưa đủ.",
                  "properties": { "so": { "type": "integer" }, "case": { "type": "array", "items": { "type": "string" } } }
                },
                "chi-ghi-ket-luan": {
                  "type": "object", "additionalProperties": false, "required": ["so", "case"],
                  "description": "Chỉ có kết luận hoặc kết quả, và KHÔNG suy ra được bước kiểm CỤ THỂ nào từ ticket. Dạng hay gặp: cả ticket là một mệnh đề nguyên nhân vài chữ, không nói quyền nào / bản ghi nào / màn nào, nên không dựng lại được phép thử. CA BIÊN 1: kênh mà khách BÁO sự cố (chat, hotline, điện thoại, ticket sinh từ một cuộc gọi đến) KHÔNG đẩy ticket sang `buoc-kiem-ngoai-ticket` — nơi BÁO không phải nơi KIỂM; muốn sang ô đó phải có dấu vết rằng chính việc KIỂM diễn ra ở kênh ngoài. CA BIÊN 2: ảnh do khách gửi kèm lúc báo lỗi (màn báo đỏ, thông báo lỗi) không kéo ticket sang `suy-ra-duoc-nhung-khong-ghi`; chỉ ảnh chụp CHỖ ĐÃ TRA mới kéo.",
                  "properties": { "so": { "type": "integer" }, "case": { "type": "array", "items": { "type": "string" } } }
                },
                "buoc-kiem-ngoai-ticket": {
                  "type": "object", "additionalProperties": false, "required": ["so", "case"],
                  "description": "Ticket có DẤU VẾT rằng chính VIỆC KIỂM đã xảy ra ở kênh khác — điều khiển máy từ xa, cuộc gọi do người xử lý hẹn để dò, gặp trực tiếp — nên thao tác kiểm không để lại chữ nào trong hồ sơ. Dấu hiệu nhận: người xử lý xin hoặc nhận quyền truy cập từ xa, hoặc hẹn gọi để kiểm, hoặc thuật lại rằng mình ĐÃ vào máy / đã gọi; rồi kết luận xuất hiện mà không có bước nào ở giữa. CA BIÊN QUAN TRỌNG NHẤT, và là chỗ ô này hay bị làm cho rỗng: KẾT LUẬN được gõ vào ticket KHÔNG kéo ticket ra khỏi ô này — ô này nói về nơi BƯỚC KIỂM xảy ra, không phải nơi KẾT LUẬN được ghi, nên một kết luận gọi đúng tên quyền/trường vẫn để ticket ở lại đây. CA BIÊN NGƯỢC LẠI: ticket chỉ được BÁO hoặc được TẠO qua điện thoại/chat thì KHÔNG thuộc ô này — kênh tiếp nhận không phải kênh chẩn đoán.",
                  "properties": { "so": { "type": "integer" }, "case": { "type": "array", "items": { "type": "string" } } }
                },
                "khong-phai-chan-doan": {
                  "type": "object", "additionalProperties": false, "required": ["so", "case"],
                  "description": "Ticket không có TRIỆU CHỨNG HỎNG nào cần tìm nguyên nhân: hỏi-đáp về cách dùng ('cái này hệ thống tự điền hay mình tự nhập?'), hướng dẫn thao tác, yêu cầu cấu hình hoặc yêu cầu nội bộ thuần tuý. Xét ô này TRƯỚC bốn ô kia, vì không có gì để chẩn đoán thì bốn ô kia không áp dụng. Câu hỏi về CÁCH DÙNG vẫn thuộc ô này dù câu trả lời gọi đúng màn hình cần vào. NGƯỢC LẠI — hai chỗ KHÔNG thuộc ô này: yêu cầu cấu hình mà trong đó CÓ thứ không chạy và phải tìm vì sao; và ticket mà TIÊU ĐỀ đặt là hỏi-đáp nhưng NỘI DUNG ghi lại là một nguyên nhân của một thứ bị chặn. Xét theo nội dung, không xét theo tiêu đề hay theo kênh tiếp nhận.",
                  "properties": { "so": { "type": "integer" }, "case": { "type": "array", "items": { "type": "string" } } }
                }
              }
            },
            "_haiCaseLech": {
              "type": ["object", "null"],
              "description": "Ticket xuat hien trong ban nhap ma KHONG thuoc nhom, kem ly do. Khoa la ma ticket. null neu khong dung ticket ngoai nhom.",
              "additionalProperties": { "type": "string" }
            },
            "khoangTrongPhaiBiet": {
              "type": "array",
              "items": { "type": "string" },
              "description": "Chỗ bạn KHÔNG biết, nói thẳng. Ví dụ: 'không ticket nào ghi thứ tự kiểm'."
            }
          }
        }
        """;
}

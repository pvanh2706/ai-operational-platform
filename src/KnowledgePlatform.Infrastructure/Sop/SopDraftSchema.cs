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
/// ⚠ `description` của từng trường KHÔNG phải chú thích cho người đọc — model đọc chúng.
/// Mỗi câu ở đây đến từ một chỗ đã đo khi dựng tay hai bản nháp đầu (`docs/11`).
/// </summary>
internal static class SopDraftSchema
{
    internal const string Json = """
        {
          "type": "object",
          "additionalProperties": false,
          "required": ["nhom", "soCaseDungSau", "trieuChungVao", "buocKiem", "buocSua",
                       "buocXacNhan", "khoangTrongPhaiBiet"],
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
            "khoangTrongPhaiBiet": {
              "type": "array",
              "items": { "type": "string" },
              "description": "Chỗ bạn KHÔNG biết, nói thẳng. Ví dụ: 'không ticket nào ghi thứ tự kiểm'."
            }
          }
        }
        """;
}

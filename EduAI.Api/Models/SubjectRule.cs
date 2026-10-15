using System.ComponentModel.DataAnnotations;

namespace EduAI.Api.Models
{
    public class SubjectRule
    {
        [Key]
        public int Id { get; set; }
        public int SubjectId { get; set; } // 0: Toán, 1: Tiếng Việt/Ngữ Văn, 2: Tiếng Anh
        [Required]
        public string RuleName { get; set; } = string.Empty; // Tên quy tắc (Ví dụ: Quy tắc chống nhầm lẫn môn Tiếng Việt)
        [Required]
        public string RuleContent { get; set; } = string.Empty; // Nội dung quy tắc bắt buộc áp dụng chung
    }
}
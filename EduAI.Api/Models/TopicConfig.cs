using System.ComponentModel.DataAnnotations;

namespace EduAI.Api.Models
{
    public class TopicConfig
    {
        [Key]
        public int Id { get; set; }
        public int ClassGrade { get; set; } // Khối lớp (1 đến 9)
        public int SubjectId { get; set; }  // 0: Toán, 1: Tiếng Việt/Ngữ Văn, 2: Tiếng Anh
        [Required]
        public string TopicHint { get; set; } = string.Empty; // Nội dung trọng tâm kiến thức / từ khóa
        public string SubjectFocus { get; set; } = string.Empty;
        public int PeriodId { get; set; }
    }
}
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EduAI.Api.Models
{
    public class TestResult
    {
        [Key]
        public int Id { get; set; }

        // Khóa ngoại liên kết tới bảng User
        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public User? User { get; set; }

        public int Subject { get; set; } // 0: Toán, 1: Tiếng Việt, 2: Tiếng Anh
        public int Level { get; set; }   // 0: Cơ bản, 1: Nâng cao
        
        public int Score { get; set; }           // Số câu đúng
        public int TotalQuestions { get; set; }  // Tổng số câu hỏi trong đề thi

        public DateTime CompletedAt { get; set; } = DateTime.Now;
    }
}
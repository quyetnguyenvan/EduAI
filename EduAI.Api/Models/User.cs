using System.ComponentModel.DataAnnotations;

namespace EduAI.Api.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;
        public int GradeId { get; set; }
        public int ClassGrade { get; set; }
        public string Role { get; set; } = "Student";
        public bool CanCreateExam { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Quan hệ 1-N: 1 Học sinh có thể có nhiều Bài kiểm tra
        public ICollection<TestResult> TestResults { get; set; } = new List<TestResult>();
    }
}
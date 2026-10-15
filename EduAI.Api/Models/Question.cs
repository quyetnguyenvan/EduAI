using System.ComponentModel.DataAnnotations;

namespace EduAI.Api.Models
{
    public enum SubjectType { Toan, TiengViet, TiengAnh }
    public enum DifficultyLevel { CoBan, NangCao }
    public enum QuestionType { TracNghiem, TuLuan, DienKhuyet }
    public enum SourceType { WebCrawled, AIGenerated }

    public class Question
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Content { get; set; } 
        public SubjectType Subject { get; set; }
        public DifficultyLevel Level { get; set; }
        public QuestionType Type { get; set; }
        public SourceType Source { get; set; }
        public string? Hint { get; set; } 
        public string? DetailedSolution { get; set; } 
        public int ClassGrade { get; set; }
        public int PeriodId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ICollection<AnswerOption> Options { get; set; }
    }
}
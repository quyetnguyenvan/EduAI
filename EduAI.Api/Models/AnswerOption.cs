using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EduAI.Api.Models
{
    public class AnswerOption
    {
        [Key]
        public int Id { get; set; }
        [ForeignKey("Question")]
        public int QuestionId { get; set; }
        [Required]
        public string Content { get; set; } 
        public bool IsCorrect { get; set; }

        public Question? Question { get; set; }
    }
}
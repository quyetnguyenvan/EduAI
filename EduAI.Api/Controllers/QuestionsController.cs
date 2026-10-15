using EduAI.Api.Data;
using EduAI.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EduAI.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QuestionsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public QuestionsController(AppDbContext context)
        {
            _context = context;
        }

        // Lấy TẤT CẢ câu hỏi (GET: api/Questions)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Question>>> GetQuestions()
        {
            // Dùng Include() để lấy luôn danh sách đáp án A, B, C, D của từng câu
            return await _context.Questions.Include(q => q.Options).ToListAsync();
        }

        // Lấy MỘT câu hỏi theo ID (GET: api/Questions/5)
        [HttpGet("{id}")]
        public async Task<ActionResult<Question>> GetQuestion(int id)
        {
            var question = await _context.Questions
                                         .Include(q => q.Options)
                                         .FirstOrDefaultAsync(q => q.Id == id);

            if (question == null) return NotFound("Không tìm thấy câu hỏi.");
            return question;
        }
        // GET: api/Questions/exam?subject=0&level=0&count=10
[HttpGet("exam")]
public async Task<ActionResult<IEnumerable<Question>>> GenerateExam(
    [FromQuery] SubjectType subject = SubjectType.Toan, 
    [FromQuery] DifficultyLevel level = DifficultyLevel.CoBan, 
    [FromQuery] int count = 10)
{
    var examQuestions = await _context.Questions
        .Include(q => q.Options)
        .Where(q => q.Subject == subject && q.Level == level)
        .OrderBy(q => Guid.NewGuid()) // Thuật toán trộn ngẫu nhiên của SQL Server
        .Take(count)
        .ToListAsync();

    return Ok(examQuestions);
}

        // THÊM MỚI câu hỏi (POST: api/Questions)
        [HttpPost]
        public async Task<ActionResult<Question>> CreateQuestion([FromBody] Question question)
        {
            _context.Questions.Add(question);
            await _context.SaveChangesAsync();

            // Trả về HTTP 201 (Created) và URL để xem câu hỏi vừa tạo
            return CreatedAtAction(nameof(GetQuestion), new { id = question.Id }, question);
        }

        // CẬP NHẬT câu hỏi (PUT: api/Questions/5)
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateQuestion(int id, [FromBody] Question question)
        {
            if (id != question.Id) return BadRequest("ID câu hỏi không khớp.");

            _context.Entry(question).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Questions.Any(e => e.Id == id)) return NotFound();
                else throw;
            }

            return NoContent(); // HTTP 204 (Thành công nhưng không cần trả về Data)
        }

        // XÓA câu hỏi (DELETE: api/Questions/5)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteQuestion(int id)
        {
            var question = await _context.Questions.FindAsync(id);
            if (question == null) return NotFound();

            _context.Questions.Remove(question);
            await _context.SaveChangesAsync();

            return NoContent();
        }
       [HttpGet("generate-test")]
        public async Task<IActionResult> GenerateTest(int subjectId, int levelId, int classGrade, int periodId, int count = 10)
        {
            // 1. Lấy danh sách thô từ SQLite theo đúng 4 điều kiện (Môn, Lớp, Độ khó, Học kỳ)
            var rawQuestions = await _context.Questions
                .Include(q => q.Options)
                .Where(q => (int)q.Subject == subjectId 
                        && (int)q.Level == levelId 
                        && q.ClassGrade == classGrade 
                        && q.PeriodId == periodId)
                .ToListAsync();

            // 2. Trộn ngẫu nhiên bằng C# (tránh lỗi cú pháp Guid.NewGuid() trên SQLite) và giới hạn số lượng
            var questions = rawQuestions
                .OrderBy(q => Guid.NewGuid())
                .Take(count)
                .ToList();

            return Ok(questions);
        }

        // [THỐNG KÊ] GET: api/Questions/statistics
        [HttpGet("statistics")]
        public async Task<IActionResult> GetQuestionStatistics()
        {
            var groupedData = await _context.Questions
                .GroupBy(q => new { q.ClassGrade, q.PeriodId, q.Subject, q.Level })
                .Select(g => new
                {
                    ClassGrade = g.Key.ClassGrade,
                    PeriodId = g.Key.PeriodId,
                    SubjectId = (int)g.Key.Subject,
                    LevelId = (int)g.Key.Level,
                    TotalQuestions = g.Count()
                })
                .ToListAsync();

            var stats = groupedData.Select(g => new
            {
                ClassGrade = g.ClassGrade,
                PeriodId = g.PeriodId,
                PeriodName = g.PeriodId switch
                {
                    0 => "Giữa học kỳ 1",
                    1 => "Cuối học kỳ 1",
                    2 => "Giữa học kỳ 2",
                    _ => "Cuối học kỳ 2"
                },
                SubjectId = g.SubjectId,
                SubjectName = g.SubjectId switch
                {
                    0 => "Toán",
                    1 => g.ClassGrade <= 5 ? "Tiếng Việt" : "Ngữ Văn",
                    2 => "Tiếng Anh",
                    _ => "Khác"
                },
                LevelId = g.LevelId,
                LevelName = g.LevelId == 0 ? "Cơ bản" : "Nâng cao",
                TotalQuestions = g.TotalQuestions
            })
            .OrderBy(s => s.ClassGrade)
            .ThenBy(s => s.PeriodId)
            .ThenBy(s => s.SubjectId)
            .ThenBy(s => s.LevelId);

            return Ok(stats);
        }
    }
}
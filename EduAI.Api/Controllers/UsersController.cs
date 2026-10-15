using EduAI.Api.Data;
using EduAI.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EduAI.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        public class RegisterRequest
        {
            public string Username { get; set; } = string.Empty;
            public string FullName { get; set; } = string.Empty;
            public int GradeId { get; set; }
            public int ClassGrade { get; set; }
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (await _context.Users.AnyAsync(u => u.Username == request.Username))
            {
                return BadRequest("Tên đăng nhập này đã được sử dụng.");
            }

            bool isAdmin = request.Username.ToLower() == "admin";

            var newUser = new User
            {
                Username = request.Username,
                FullName = request.FullName,
                GradeId = request.GradeId,
                ClassGrade = request.ClassGrade,
                Role = isAdmin ? "Admin" : "Student",
                CanCreateExam = isAdmin
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            return Ok(newUser);
        }

        // [LẤY DANH SÁCH TẤT CẢ NGƯỜI DÙNG CHO TRANG ADMIN PHÂN QUYỀN] GET: api/Users
        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _context.Users
                .Select(u => new 
                {
                    u.Id,
                    u.Username,
                    u.FullName,
                    u.ClassGrade,
                    u.Role,
                    u.CanCreateExam
                })
                .ToListAsync();

            return Ok(users);
        }

        [HttpGet("username/{username}")]
        public async Task<IActionResult> GetUserByUsername(string username)
        {
            var user = await _context.Users
                .Where(u => u.Username == username)
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.FullName,
                    u.GradeId,
                    u.ClassGrade,
                    u.Role,
                    u.CanCreateExam
                })
                .FirstOrDefaultAsync();

            if (user == null) return NotFound();
            return Ok(user);
        }

        [HttpPost("submit-result")]
        public async Task<ActionResult<TestResult>> SubmitTestResult([FromBody] TestResult result)
        {
            var userExists = await _context.Users.AnyAsync(u => u.Id == result.UserId);
            if (!userExists) return BadRequest("Lỗi: Không tìm thấy ID học sinh hợp lệ.");

            result.CompletedAt = DateTime.Now;
            _context.TestResults.Add(result);
            await _context.SaveChangesAsync();

            return Ok(result);
        }

        [HttpGet("{userId}/results")]
        public async Task<IActionResult> GetTestResults(int userId)
        {
            var results = await _context.TestResults
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CompletedAt)
                .ToListAsync();

            return Ok(results); 
        }

        // [CẤP QUYỀN TẠO ĐỀ] POST: api/Users/grant-create-exam/{username}
        [HttpPost("grant-create-exam/{username}")]
        public async Task<IActionResult> GrantCreateExamRole(string username)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null) return NotFound("Không tìm thấy tài khoản này!");

            user.CanCreateExam = true;
            await _context.SaveChangesAsync();

            return Ok(new { Message = $"Đã cấp quyền tạo đề cho {username}", CanCreateExam = true });
        }

        // [BẬT/TẮT HOẶC THU HỒI QUYỀN TẠO ĐỀ] POST: api/Users/toggle-create-exam/{username}
        [HttpPost("toggle-create-exam/{username}")]
        public async Task<IActionResult> ToggleCreateExamRole(string username)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null) return NotFound("Không tìm thấy tài khoản này!");

            user.CanCreateExam = !user.CanCreateExam; // Đảo trạng thái (True <-> False)
            await _context.SaveChangesAsync();

            return Ok(new { Message = $"Đã cập nhật quyền tạo đề cho {username}", CanCreateExam = user.CanCreateExam });
        }

        [HttpGet("all-results")]
        public async Task<IActionResult> GetAllTestResults()
        {
            var results = await _context.TestResults
                .Include(t => t.User)
                .OrderByDescending(t => t.CompletedAt)
                .Select(t => new
                {
                    t.Id,
                    Username = t.User != null ? t.User.Username : "Không xác định",
                    FullName = t.User != null ? t.User.FullName : "Không xác định",
                    ClassGrade = t.User != null ? t.User.ClassGrade : 0,
                    t.Subject,
                    t.Level,
                    t.Score,
                    t.TotalQuestions,
                    t.CompletedAt
                })
                .ToListAsync();

            return Ok(results);
        }
    }
}
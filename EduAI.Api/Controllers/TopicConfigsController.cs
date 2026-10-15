using EduAI.Api.Data;
using EduAI.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EduAI.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TopicConfigsController : ControllerBase
    {
        private readonly AppDbContext _context;
        public TopicConfigsController(AppDbContext context) => _context = context;

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TopicConfig>>> GetTopics()
        {
            return await _context.TopicConfigs
                .OrderBy(t => t.ClassGrade).ThenBy(t => t.SubjectId).ThenBy(t => t.PeriodId).ToListAsync();
        }

        [HttpPost]
        public async Task<ActionResult<TopicConfig>> CreateTopic([FromBody] TopicConfig topic)
        {
            // Kiểm tra theo Lớp, Môn và Học kỳ[cite: 12]
            var existing = await _context.TopicConfigs
                .FirstOrDefaultAsync(t => t.ClassGrade == topic.ClassGrade && t.SubjectId == topic.SubjectId && t.PeriodId == topic.PeriodId);
            
            if (existing != null)
            {
                existing.TopicHint = topic.TopicHint;
                await _context.SaveChangesAsync();
                return Ok(existing);
            }

            _context.TopicConfigs.Add(topic);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetTopics), new { id = topic.Id }, topic);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTopic(int id)
        {
            var topic = await _context.TopicConfigs.FindAsync(id);
            if (topic == null) return NotFound();
            _context.TopicConfigs.Remove(topic);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
using EduAI.Api.Data;
using EduAI.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EduAI.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SubjectRulesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SubjectRulesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<SubjectRule>>> GetRules()
        {
            return await _context.SubjectRules.ToListAsync();
        }

        [HttpPost]
        public async Task<ActionResult<SubjectRule>> CreateRule([FromBody] SubjectRule rule)
        {
            _context.SubjectRules.Add(rule);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetRules), new { id = rule.Id }, rule);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRule(int id)
        {
            var rule = await _context.SubjectRules.FindAsync(id);
            if (rule == null) return NotFound();
            
            _context.SubjectRules.Remove(rule);
            await _context.SaveChangesAsync();
            
            return NoContent();
        }
    }
}
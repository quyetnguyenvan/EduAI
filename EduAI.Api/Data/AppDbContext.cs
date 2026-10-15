using EduAI.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EduAI.Api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Question> Questions { get; set; }
        public DbSet<AnswerOption> AnswerOptions { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<TestResult> TestResults { get; set; }
        public DbSet<TopicConfig> TopicConfigs { get; set; }
        public DbSet<SubjectRule> SubjectRules { get; set; }
    }
}
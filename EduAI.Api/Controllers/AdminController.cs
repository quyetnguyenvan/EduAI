using EduAI.Api.Data;
using EduAI.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;

namespace EduAI.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;

        public AdminController(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public class GenerateRequest
        {
            public int SubjectId { get; set; }
            public int LevelId { get; set; }
            public int ClassGrade { get; set; } 
            public int Count { get; set; }      
            public int PeriodId { get; set; }   // Giữ nguyên trường Kỳ học (0, 1, 2, 3)[cite: 17]
        }

        public class AiQuestionResult
        {
            public string content { get; set; } = string.Empty;
            public string hint { get; set; } = string.Empty;
            public string detailedSolution { get; set; } = string.Empty;
            public List<AiOptionResult> options { get; set; } = new();
        }

        public class AiOptionResult
        {
            public string content { get; set; } = string.Empty;
            public bool isCorrect { get; set; }
        }

        [HttpPost("generate")]
        public async Task<IActionResult> GenerateQuestions([FromBody] GenerateRequest req)
        {
            string levelStr = req.LevelId == 0 ? "Cơ bản" : "Nâng cao, yêu cầu tư duy suy luận logic";
            
            string subjectStr = req.SubjectId switch
            {
                1 => req.ClassGrade <= 5 ? "Tiếng Việt" : "Ngữ Văn",
                2 => "Tiếng Anh",
                _ => "Toán"
            };

            // Phân loại giai đoạn kiến thức theo Kỳ học
            string periodStr = req.PeriodId switch
            {
                0 => "Giữa học kỳ 1",
                1 => "Cuối học kỳ 1",
                2 => "Giữa học kỳ 2",
                _ => "Cuối học kỳ 2"
            };

            // Đọc trọng tâm kiến thức từ bảng TopicConfigs theo Lớp, Môn và Kỳ học
            string topicHint = "Kiến thức chuẩn theo độ tuổi bám sát chương trình học.";
            try
            {
                var topicConfig = await _context.TopicConfigs
                    .FirstOrDefaultAsync(t => t.ClassGrade == req.ClassGrade && t.SubjectId == req.SubjectId && t.PeriodId == req.PeriodId);
                if (topicConfig != null && !string.IsNullOrEmpty(topicConfig.TopicHint))
                {
                    topicHint = topicConfig.TopicHint;
                }
            }
            catch { /* An toàn nếu chưa có bảng */ }

            // Đọc các quy tắc chung từ bảng SubjectRules (hỗ trợ -1 cho toàn hệ thống)
            string globalSubjectRules = "Tuân thủ nghiêm ngặt chuẩn kiến thức môn học.";
            try
            {
                var rulesList = await _context.SubjectRules
                    .Where(r => r.SubjectId == req.SubjectId || r.SubjectId == -1)
                    .Select(r => r.RuleContent)
                    .ToListAsync();
                if (rulesList.Any())
                {
                    globalSubjectRules = string.Join(" ", rulesList);
                }
            }
            catch { /* An toàn nếu chưa có bảng */ }

            // Xây dựng Prompt chuẩn ép AI không lan man sang kỳ khác
            string prompt = $"Đóng vai một giáo viên chuyên môn. Hãy tạo ĐÚNG {req.Count} câu hỏi trắc nghiệm môn {subjectStr} dành cho học sinh LỚP {req.ClassGrade}, GIAI ĐOẠN: {periodStr}, độ khó {levelStr}. " +
                            $"TRỌNG TÂM KIẾN THỨC CỦA KỲ NÀY: {topicHint}. " +
                            $"QUY TẮC CỐT LÕI: Kiến thức CHỈ ĐƯỢC PHÉP nằm trong phạm vi của {periodStr}. TUYỆT ĐỐI KHÔNG sử dụng kiến thức của các học kỳ khác để tránh bị lan man. " +
                            $"CÁC QUY TẮC CHUNG: {globalSubjectRules} " +
                            "Trả về CHỈ một mảng JSON (không kèm văn xuôi chào hỏi, không dùng markdown) với cấu trúc: " +
                            "[ { \"content\": \"câu hỏi\", \"hint\": \"gợi ý ngắn gọn giúp học sinh tư duy\", \"detailedSolution\": \"lời giải chi tiết, giải thích rõ ràng\", \"options\": [ { \"content\": \"đáp án A\", \"isCorrect\": true }, { \"content\": \"đáp án B\", \"isCorrect\": false } ] } ]";

            List<AiQuestionResult>? aiQuestions = null;
            string apiError = "";
            System.Net.HttpStatusCode apiStatus = System.Net.HttpStatusCode.OK;

            // =======================================================
            // TẦNG 1: GOOGLE GEMINI (Có cơ chế Retry)[cite: 17]
            // =======================================================
            string? geminiKey = _config["Gemini:ApiKey"];
            string geminiModel = _config["Gemini:ModelName"] ?? "gemini-1.5-flash"; 

            if (!string.IsNullOrWhiteSpace(geminiKey) && !geminiKey.Contains("YOUR_GEMINI_API_KEY"))
            {
                (aiQuestions, apiError, apiStatus) = await CallGeminiAPI(prompt, geminiKey, geminiModel);
            }

            // =======================================================
            // TẦNG 2: OPENAI CHATGPT (NẾU GEMINI LỖI HOẶC HẾT QUOTA)[cite: 17]
            // =======================================================
            if (aiQuestions == null || aiQuestions.Count == 0)
            {
                string? openAiKey = _config["OpenAI:ApiKey"];
                string openAiModel = _config["OpenAI:ModelName"] ?? "gpt-4o-mini"; 

                if (!string.IsNullOrWhiteSpace(openAiKey) && !openAiKey.Contains("YOUR_REAL_OPENAI_API_KEY"))
                {
                    (aiQuestions, apiError, apiStatus) = await CallOpenAIAPI(prompt, openAiKey, openAiModel);
                }
            }

            // =======================================================
            // TẦNG 3: DATABASE FALLBACK (LẤY TỪ SQL NẾU CẢ 2 AI ĐỀU LỖI)[cite: 17]
            // =======================================================
            if (aiQuestions == null || aiQuestions.Count == 0)
            {
                var fallbackList = await _context.Questions
                    .Include(q => q.Options)
                    .Where(q => (int)q.Subject == req.SubjectId && (int)q.Level == req.LevelId && q.ClassGrade == req.ClassGrade && q.PeriodId == req.PeriodId)
                    .OrderBy(q => Guid.NewGuid())
                    .Take(req.Count)
                    .ToListAsync();

                if (fallbackList.Count > 0)
                {
                    string msg = "Các dịch vụ AI hiện đang quá tải. Hệ thống đã tự động lấy dữ liệu dự phòng từ Ngân hàng câu hỏi nội bộ theo đúng kỳ học!";
                    return Ok(new { Message = msg, Count = fallbackList.Count, Data = fallbackList });
                }

                string friendlyError = TranslateApiError(apiError, apiStatus);
                return StatusCode(500, new { message = friendlyError });
            }

            // Lưu danh sách câu hỏi AI tạo được vào CSDL
            int addedCount = 0;
            foreach (var aiQ in aiQuestions)
            {
                var newQuestion = new Question
                {
                    Subject = (SubjectType)req.SubjectId, 
                    Level = (DifficultyLevel)req.LevelId,
                    ClassGrade = req.ClassGrade,
                    PeriodId = req.PeriodId, // Ghi nhận chính xác kỳ học vào CSDL
                    Content = aiQ.content, 
                    Hint = aiQ.hint,
                    DetailedSolution = aiQ.detailedSolution,
                    Source = SourceType.AIGenerated,
                    Options = aiQ.options.Select(o => new AnswerOption 
                    { 
                        Content = o.content, 
                        IsCorrect = o.isCorrect 
                    }).ToList()
                };
                _context.Questions.Add(newQuestion);
                addedCount++;
            }
            
            await _context.SaveChangesAsync();
            return Ok(new { Message = $"Tuyệt vời! Đã dùng AI tạo và lưu thành công {addedCount} câu hỏi lớp {req.ClassGrade} ({periodStr})!", Count = addedCount });
        }

        private async Task<(List<AiQuestionResult>?, string, System.Net.HttpStatusCode)> CallGeminiAPI(string prompt, string apiKey, string modelName)
        {
            string apiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/{modelName}:generateContent?key={apiKey}";
            var requestBody = new { contents = new[] { new { parts = new[] { new { text = prompt } } } } };
            
            int maxRetries = 4;
            int delayMs = 2000;
            HttpResponseMessage? response = null;

            for (int i = 0; i < maxRetries; i++)
            {
                using var requestContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                try
                {
                    response = await _httpClient.PostAsync(apiUrl, requestContent);
                    if (response.IsSuccessStatusCode) break;
                    
                    if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable || response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    {
                        if (i < maxRetries - 1) { await Task.Delay(delayMs); delayMs *= 2; continue; }
                    }
                    else break;
                }
                catch (TaskCanceledException)
                {
                    return (null, "Gemini Request Timeout", System.Net.HttpStatusCode.RequestTimeout);
                }
            }

            if (response == null || !response.IsSuccessStatusCode)
            {
                string error = response != null ? await response.Content.ReadAsStringAsync() : "Timeout";
                return (null, error, response?.StatusCode ?? System.Net.HttpStatusCode.InternalServerError);
            }

            try
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(jsonResponse);
                string textOutput = doc.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString() ?? "";
                
                // Bóc tách chuẩn xác mảng JSON, loại bỏ chữ thừa
                int startIndex = textOutput.IndexOf('[');
                int endIndex = textOutput.LastIndexOf(']');
                if (startIndex != -1 && endIndex != -1 && endIndex > startIndex)
                {
                    textOutput = textOutput.Substring(startIndex, endIndex - startIndex + 1);
                }
                else
                {
                    textOutput = textOutput.Replace("```json", "").Replace("```", "").Trim();
                }
                
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var aiQuestions = JsonSerializer.Deserialize<List<AiQuestionResult>>(textOutput, options);
                return (aiQuestions, "", System.Net.HttpStatusCode.OK);
            }
            catch { return (null, "Lỗi phân tích JSON từ Gemini", System.Net.HttpStatusCode.InternalServerError); }
        }

        private async Task<(List<AiQuestionResult>?, string, System.Net.HttpStatusCode)> CallOpenAIAPI(string prompt, string apiKey, string modelName)
        {
            string apiUrl = "https://api.openai.com/v1/chat/completions";
            var requestBody = new 
            { 
                model = modelName, 
                messages = new[] { new { role = "user", content = prompt } }
            };

            int maxRetries = 2;
            int delayMs = 1500;
            HttpResponseMessage? response = null;

            for (int i = 0; i < maxRetries; i++)
            {
                using var requestMessage = new HttpRequestMessage(HttpMethod.Post, apiUrl);
                requestMessage.Headers.Add("Authorization", $"Bearer {apiKey}");
                requestMessage.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

                try
                {
                    response = await _httpClient.SendAsync(requestMessage);
                    if (response.IsSuccessStatusCode) break;

                    if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    {
                        if (i < maxRetries - 1) { await Task.Delay(delayMs); continue; }
                    }
                    else break;
                }
                catch (TaskCanceledException)
                {
                    return (null, "OpenAI Request Timeout", System.Net.HttpStatusCode.RequestTimeout);
                }
            }
            
            if (response == null || !response.IsSuccessStatusCode)
            {
                string error = response != null ? await response.Content.ReadAsStringAsync() : "Timeout";
                return (null, error, response?.StatusCode ?? System.Net.HttpStatusCode.InternalServerError);
            }

            try
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(jsonResponse);
                string textOutput = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";
                
                // Bóc tách chuẩn xác mảng JSON từ OpenAI
                int startIndex = textOutput.IndexOf('[');
                int endIndex = textOutput.LastIndexOf(']');
                if (startIndex != -1 && endIndex != -1 && endIndex > startIndex)
                {
                    textOutput = textOutput.Substring(startIndex, endIndex - startIndex + 1);
                }
                else
                {
                    textOutput = textOutput.Replace("```json", "").Replace("```", "").Trim();
                }
                
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var aiQuestions = JsonSerializer.Deserialize<List<AiQuestionResult>>(textOutput, options);
                return (aiQuestions, "", System.Net.HttpStatusCode.OK);
            }
            catch { return (null, "Lỗi phân tích JSON từ OpenAI", System.Net.HttpStatusCode.InternalServerError); }
        }

        private string TranslateApiError(string rawError, System.Net.HttpStatusCode statusCode)
        {
            if (statusCode == System.Net.HttpStatusCode.TooManyRequests || rawError.Contains("RESOURCE_EXHAUSTED") || rawError.Contains("quota") || rawError.Contains("insufficient_quota"))
            {
                return "Cả Gemini và ChatGPT đều đang hết hạn mức hoặc tài khoản OpenAI chưa nạp tiền (insufficient_quota). Vui lòng kiểm tra lại tài khoản.";
            }

            if (statusCode == System.Net.HttpStatusCode.Unauthorized || rawError.Contains("API_KEY_INVALID"))
                return "Lỗi xác thực: Khóa API (API Key) không hợp lệ.";
                
            if (statusCode == System.Net.HttpStatusCode.NotFound || rawError.Contains("NOT_FOUND"))
                return "Lỗi cấu hình: Không tìm thấy phiên bản mô hình AI này trên máy chủ.";
            
            return $"Sự cố kết nối AI: {rawError}";
        }

        // Endpoint thống kê kết quả học sinh phục vụ Tab 4 của Admin[cite: 20]
        [HttpGet("all-results")]
        public async Task<IActionResult> GetAllTestResults()
        {
            try
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
            catch
            {
                return Ok(new List<object>()); 
            }
        }
    }
}
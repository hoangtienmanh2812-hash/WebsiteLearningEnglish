using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EnglishLearning.Application.DTOs;
using EnglishLearning.Domain.Entities;
using EnglishLearning.Infrastructure.Data;

namespace EnglishLearning.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GamificationController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;

        public GamificationController(ApplicationDbContext context, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
        }

        // 1. API ĐỌC TỪ VỰNG TỪ FILE JSON CỦA NGÀY HỌC
        [HttpGet("daily-vocab-json/{userId}/{dayNumber}")]
        public async Task<IActionResult> GetDailyVocabFromJson(Guid userId, int dayNumber)
        {
            var pack = await _context.DailyVocabPacks
                .FirstOrDefaultAsync(p => p.DayNumber == dayNumber);

            if (pack == null)
            {
                return NotFound(new { message = $"Chưa có gói từ vựng cho Ngày {dayNumber}" });
            }

            try
            {
                var client = _httpClientFactory.CreateClient();
                var jsonString = await client.GetStringAsync(pack.JsonUrl);

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var vocabList = JsonSerializer.Deserialize<List<object>>(jsonString, options);

                var learnedIds = await _context.UserLearnedVocabs
                    .Where(uv => uv.UserId == userId && uv.DayNumber == dayNumber)
                    .Select(uv => uv.VocabId)
                    .ToListAsync();

                return Ok(new
                {
                    dayNumber = pack.DayNumber,
                    title = pack.Title,
                    learnedVocabIds = learnedIds,
                    vocabularies = vocabList
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi tải file JSON từ vựng", detail = ex.Message });
            }
        }

        // 2. API ĐÁNH DẤU THUỘC TỪ VỰNG (+10 EXP)
        [HttpPost("mark-vocab-learned")]
        public async Task<IActionResult> MarkVocabLearned([FromBody] MarkVocabLearnedRequest req)
        {
            var today = DateTime.UtcNow.Date;

            var existing = await _context.UserLearnedVocabs
                .FirstOrDefaultAsync(uv => uv.UserId == req.UserId && uv.DayNumber == req.DayNumber && uv.VocabId == req.VocabId);

            if (existing == null)
            {
                _context.UserLearnedVocabs.Add(new UserLearnedVocab
                {
                    UserId = req.UserId,
                    DayNumber = req.DayNumber,
                    VocabId = req.VocabId,
                    LearnedDate = today
                });

                var user = await _context.Users.FindAsync(req.UserId);
                if (user != null)
                {
                    user.Exp += 10;
                }

                await _context.SaveChangesAsync();
            }

            return Ok(new { message = "Đã đánh dấu thuộc từ vựng! +10 EXP" });
        }

        // 3. API BẢNG XẾP HẠNG TOP 10 EXP
        [HttpGet("leaderboard")]
        public async Task<IActionResult> GetLeaderboard()
        {
            var leaderboard = await _context.Users
                .OrderByDescending(u => u.Exp)
                .Take(10)
                .Select(u => new LeaderboardUserDto
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Exp = u.Exp,
                    StreakCount = u.StreakCount
                })
                .ToListAsync();

            return Ok(leaderboard);
        }

        // 4. API ĐIỂM DANH STREAK HÀNG NGÀY
        [HttpPost("checkin-streak/{userId}")]
        public async Task<IActionResult> CheckInStreak(Guid userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            var today = DateTime.UtcNow.Date;

            if (user.LastLoginDate == null)
            {
                user.StreakCount = 1;
            }
            else if (user.LastLoginDate.Value.Date == today.AddDays(-1))
            {
                user.StreakCount += 1;
            }
            else if (user.LastLoginDate.Value.Date < today.AddDays(-1))
            {
                user.StreakCount = 1;
            }

            user.LastLoginDate = today;
            await _context.SaveChangesAsync();

            return Ok(new { user.StreakCount, user.Exp });
        }
    }
}
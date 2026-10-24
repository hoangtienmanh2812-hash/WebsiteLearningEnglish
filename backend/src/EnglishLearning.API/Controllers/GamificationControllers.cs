using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using EnglishLearning.Application.DTOs;
using EnglishLearning.Application.Features.Admin.Interfaces;
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
        private readonly ISystemSettingsService _systemSettingsService;

        public GamificationController(
            ApplicationDbContext context,
            IHttpClientFactory httpClientFactory,
            ISystemSettingsService systemSettingsService)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _systemSettingsService = systemSettingsService;
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
                    vocabularies = vocabList,
                    xpPerVocabulary = await _systemSettingsService.GetXpRewardAsync(
                        SystemSettingsService.VocabularyLearnedXpKey)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi tải file JSON từ vựng", detail = ex.Message });
            }
        }

        // 2. API ĐÁNH DẤU THUỘC TỪ VỰNG
        [Authorize]
        [HttpPost("mark-vocab-learned")]
        public async Task<IActionResult> MarkVocabLearned(
            [FromBody] MarkVocabLearnedRequest req,
            CancellationToken cancellationToken)
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (!Guid.TryParse(userIdValue, out var authenticatedUserId))
            {
                return Unauthorized(new { message = "Không xác định được tài khoản hiện tại." });
            }
            if (req.UserId != authenticatedUserId || req.DayNumber <= 0 || req.VocabId <= 0)
            {
                return BadRequest(new { message = "Dữ liệu từ vựng không hợp lệ." });
            }

            var today = DateTime.UtcNow.Date;

            var existing = await _context.UserLearnedVocabs
                .FirstOrDefaultAsync(uv => uv.UserId == authenticatedUserId && uv.DayNumber == req.DayNumber && uv.VocabId == req.VocabId, cancellationToken);
            var xpReward = 0;

            if (existing == null)
            {
                var user = await _context.Users.FindAsync(new object?[] { authenticatedUserId }, cancellationToken);
                if (user is null)
                {
                    return NotFound(new { message = "Không tìm thấy người dùng." });
                }

                _context.UserLearnedVocabs.Add(new UserLearnedVocab
                {
                    UserId = authenticatedUserId,
                    DayNumber = req.DayNumber,
                    VocabId = req.VocabId,
                    LearnedDate = today
                });

                xpReward = await _systemSettingsService.GetXpRewardAsync(
                    SystemSettingsService.VocabularyLearnedXpKey,
                    cancellationToken);
                user.Exp += xpReward;

                await _context.SaveChangesAsync(cancellationToken);
            }

            return Ok(new
            {
                message = existing is null
                    ? $"Đã đánh dấu thuộc từ vựng! +{xpReward} EXP"
                    : "Từ vựng này đã được ghi nhận trước đó.",
                xpReward
            });
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
        [Authorize]
        [HttpPost("checkin-streak/{userId}")]
        public async Task<IActionResult> CheckInStreak(Guid userId)
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (!Guid.TryParse(userIdValue, out var authenticatedUserId) || authenticatedUserId != userId)
            {
                return Forbid();
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            var today = DateTime.UtcNow.Date;
            var xpReward = 0;
            var checkedInToday = user.LastLoginDate?.Date == today;

            if (!checkedInToday && user.LastLoginDate == null)
            {
                user.StreakCount = 1;
            }
            else if (!checkedInToday && user.LastLoginDate!.Value.Date == today.AddDays(-1))
            {
                user.StreakCount += 1;
            }
            else if (!checkedInToday && user.LastLoginDate!.Value.Date < today.AddDays(-1))
            {
                user.StreakCount = 1;
            }

            if (!checkedInToday)
            {
                xpReward = await _systemSettingsService.GetXpRewardAsync(
                    SystemSettingsService.DailyStreakXpKey);
                user.Exp += xpReward;
                user.LastLoginDate = today;
            }
            await _context.SaveChangesAsync();

            return Ok(new { user.StreakCount, user.Exp, xpReward });
        }
    }
}
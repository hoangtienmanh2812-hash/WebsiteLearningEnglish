using Microsoft.AspNetCore.Mvc;

namespace EnglishLearning.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QuizController : ControllerBase
    {
        [HttpGet("{id}")]
        public IActionResult GetQuizQuestions(int id)
        {
            // Trả về dữ liệu mẫu dựa theo ID bài học mà bạn click
            var questions = new[]
            {
                new {
                    id = 1,
                    question = $"Bài {id}: Chọn nghĩa đúng của từ 'Shine':",
                    options = new[] { "Tỏa sáng", "Bóng tối", "Mặt trăng", "Ngủ" },
                    correctAnswer = "Tỏa sáng"
                },
                new {
                    id = 2,
                    question = $"Bài {id}: Dịch câu 'I eat an apple':",
                    options = new[] { "Tôi ăn một quả táo", "Tôi uống nước", "Tôi đi ngủ", "Tôi chạy" },
                    correctAnswer = "Tôi ăn một quả táo"
                },
                new {
                    id = 3,
                    question = $"Bài {id}: Từ nào đồng nghĩa với 'Beautiful'?",
                    options = new[] { "Ugly", "Pretty", "Angry", "Sad" },
                    correctAnswer = "Pretty"
                }
            };

            return Ok(questions);
        }
    }
}
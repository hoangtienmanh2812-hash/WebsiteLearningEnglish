SELECT TOP (1000) [Id]
      ,[ExerciseId]
      ,[Prompt]
      ,[AudioUrl]
      ,[Explanation]
      ,[Type]
      ,[Order]
  FROM [EnglishLearningDb].[dbo].[Questions]
CREATE TABLE Questions (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    LessonId INT NOT NULL,              -- ID của bài học (1, 2, 3...)
    QuestionText NVARCHAR(500) NOT NULL, -- Nội dung câu hỏi
    Option1 NVARCHAR(200) NOT NULL,     -- Đáp án 1
    Option2 NVARCHAR(200) NOT NULL,     -- Đáp án 2
    Option3 NVARCHAR(200) NOT NULL,     -- Đáp án 3
    Option4 NVARCHAR(200) NOT NULL,     -- Đáp án 4
    CorrectAnswer NVARCHAR(200) NOT NULL -- Đáp án đúng
);

-- Thêm thử dữ liệu mẫu cho Bài 1 và Bài 2
INSERT INTO Questions (LessonId, QuestionText, Option1, Option2, Option3, Option4, CorrectAnswer) 
VALUES 
(1, N'Chọn nghĩa đúng của từ "Shine":', N'Tỏa sáng', N'Bóng tối', N'Mặt trăng', N'Ngủ', N'Tỏa sáng'),
(1, N'Dịch câu "I eat an apple" sang tiếng Việt:', N'Tôi ăn một quả táo', N'Tôi uống nước', N'Tôi đi ngủ', N'Tôi chạy', N'Tôi ăn một quả táo'),
(2, N'Từ nào đồng nghĩa với "Beautiful"?', N'Ugly', N'Pretty', N'Angry', N'Sad', N'Pretty');

SELECT COLUMN_NAME 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = N'Questions';

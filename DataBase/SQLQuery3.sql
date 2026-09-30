SELECT TOP (1000) [Id]
      ,[LessonId]
      ,[QuestionText]
      ,[Option1]
      ,[Option2]
      ,[Option3]
      ,[Option4]
      ,[CorrectAnswer]
  FROM [EnglishLearningDb].[dbo].[Questions]
INSERT INTO Questions (LessonId, QuestionText, Option1, Option2, Option3, Option4, CorrectAnswer)
VALUES 
(1, N'Chọn nghĩa đúng của từ "Shine":', N'Tỏa sáng', N'Bóng tối', N'Mặt trăng', N'Ngủ', N'Tỏa sáng'),
(1, N'Dịch câu "I eat an apple":', N'Tôi ăn một quả táo', N'Tôi uống nước', N'Tôi đi ngủ', N'Tôi chạy', N'Tôi ăn một quả táo');
INSERT INTO Questions (LessonId, QuestionText, Option1, Option2, Option3, Option4, CorrectAnswer)
VALUES 
-- Câu 1
(1, N'What does “expensive” mean?', N'A. Rẻ', N'B. Đắt', N'C. Đẹp', N'D. Nhanh', N'B. Đắt'),

-- Câu 2
(1, N'She ___ to school every day.', N'A. go', N'B. goes', N'C. going', N'D. gone', N'B. goes'),

-- Câu 3
(1, N'He don''t like coffee. (Tìm lỗi sai ở phần nào?)', N'A. He', N'B. don''t', N'C. like', N'D. coffee', N'B. don''t'),

-- Câu 4
(1, N'I have lived in Hanoi ___ 2022.', N'A. for', N'B. since', N'C. during', N'D. from', N'B. since'),

-- Câu 5
(1, N'If it rains tomorrow, we ___ at home.', N'A. stay', N'B. stayed', N'C. will stay', N'D. staying', N'C. will stay'),

-- Câu 6
(1, N'Choose the correct sentence:', N'A. Usually / breakfast / I / have / at 7 a.m.', N'B. I usually have breakfast at 7 a.m.', N'C. I have usually breakfast at 7 a.m.', N'D. I breakfast usually have at 7 a.m.', N'B. I usually have breakfast at 7 a.m.'),

-- Câu 7
(1, N'A: “Would you like some coffee?” - B: “___”', N'A. Yes, I''d love some.', N'B. Yes, I like coffee yesterday.', N'C. No, I don''t like.', N'D. I''m drinking yesterday.', N'A. Yes, I''d love some.'),

-- Câu 8
(1, N'Anna gets up at 6:30 every morning. She has breakfast and goes to school at 7:15. She usually goes to school by bus. How does Anna usually go to school?', N'A. By car', N'B. By bike', N'C. By bus', N'D. On foot', N'C. By bus'),

-- Câu 9
(1, N'Which word has the closest meaning to “begin”?', N'A. Finish', N'B. Start', N'C. Stop', N'D. Continue', N'B. Start'),

-- Câu 10
(1, N'Which sentence is grammatically correct?', N'A. She can sings very well.', N'B. She can singing very well.', N'C. She can sing very well.', N'D. She cans sing very well.', N'C. She can sing very well.');
INSERT INTO Questions (LessonId, QuestionText, Option1, Option2, Option3, Option4, CorrectAnswer)
VALUES 
-- Đổi LessonId thành 2 cho các câu hỏi thuộc Bài 2
(2, N'What is the capital of France?', N'London', N'Paris', N'Berlin', N'Madrid', N'Paris'),
(2, N'Which planet is known as the Red Planet?', N'Earth', N'Mars', N'Jupiter', N'Saturn', N'Mars');
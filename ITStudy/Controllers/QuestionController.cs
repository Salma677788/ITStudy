using ITStudy.DTOs;
using ITStudy.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ITStudy.Data;

namespace ITStudy.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class QuestionsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public QuestionsController(AppDbContext context)
        {
            _context = context;
        }

        
        [HttpGet("lesson/{lessonId}")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<QuestionDto>>> GetQuestionsByLesson(int lessonId)
        {
            var rawQuestions = await _context.Questions
                .Where(q => q.LessonID == lessonId)
                .ToListAsync();

            var questionsDto = rawQuestions.Select(q => new QuestionDto
            {
                QuestionID = q.QuestionID,
                QuestionText = q.QuestionText,
                Options = new List<string> { q.OptionA, q.OptionB, q.OptionC, q.OptionD }
                    .Where(o => !string.IsNullOrWhiteSpace(o))
                    .ToList(),
                LessonID = q.LessonID
            }).ToList();

            return Ok(questionsDto);
        }
       
        [HttpPost("submit")]
        [Authorize]
        public async Task<IActionResult> SubmitAnswer([FromBody] SubmitAnswerDto dto)
        {
            var question = await _context.Questions.FindAsync(dto.QuestionID);
            if (question == null)
                return NotFound(new { message = "Question not found." });

            bool isCorrect = string.Equals(
                question.CorrectAnswer.Trim(),
                dto.SelectedAnswer.Trim(),
                StringComparison.OrdinalIgnoreCase
            );

            return Ok(new
            {
                QuestionID = question.QuestionID,
                IsCorrect = isCorrect,
                CorrectAnswer = isCorrect ? null : question.CorrectAnswer
            });
        }

        
        [HttpPost]
        [Authorize(Roles = "Admin,Instructor")]
        public async Task<IActionResult> CreateQuestion([FromBody] CreateQuestionDto dto)
        {
            var lessonExists = await _context.Lessons.AnyAsync(l => l.LessonID == dto.LessonID);
            if (!lessonExists)
                return BadRequest(new { message = "The specified lesson does not exist." });

            var question = new Question
            {
                QuestionText = dto.QuestionText,
                OptionA = dto.OptionA,
                OptionB = dto.OptionB,
                OptionC = dto.OptionC ?? string.Empty,
                OptionD = dto.OptionD ?? string.Empty,
                CorrectAnswer = dto.CorrectAnswer,
                LessonID = dto.LessonID
            };

            _context.Questions.Add(question);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetQuestionsByLesson), new { lessonId = question.LessonID }, question);
        }

        
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Instructor")]
        public async Task<IActionResult> UpdateQuestion(int id, [FromBody] CreateQuestionDto dto)
        {
            var question = await _context.Questions.FindAsync(id);
            if (question == null)
                return NotFound(new { message = "Question not found." });

            var lessonExists = await _context.Lessons.AnyAsync(l => l.LessonID == dto.LessonID);
            if (!lessonExists)
                return BadRequest(new { message = "The specified lesson does not exist." });

            question.QuestionText = dto.QuestionText;
            question.OptionA = dto.OptionA;
            question.OptionB = dto.OptionB;
            question.OptionC = dto.OptionC ?? string.Empty;
            question.OptionD = dto.OptionD ?? string.Empty;
            question.CorrectAnswer = dto.CorrectAnswer;
            question.LessonID = dto.LessonID;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Question updated successfully." });
        }

        
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Instructor")]
        public async Task<IActionResult> DeleteQuestion(int id)
        {
            var question = await _context.Questions.FindAsync(id);
            if (question == null)
                return NotFound(new { message = "Question not found." });

            _context.Questions.Remove(question);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Question deleted successfully." });
        }
    }
}
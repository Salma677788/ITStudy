using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using ITStudy.Data;
using ITStudy.Models;
using ITStudy.DTOs;

namespace ITStudy.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LessonsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public LessonsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<ActionResult<IEnumerable<LessonDto>>> GetLessons()
        {
            var lessons = await _context.Lessons
                .OrderBy(l => l.CourseID)
                .ThenBy(l => l.LessonOrder)
                .Select(l => new LessonDto
                {
                    LessonID = l.LessonID,
                    LessonTitle = l.LessonTitle,
                    Content = l.Content,
                    CourseID = l.CourseID,
                    LessonOrder = l.LessonOrder
                })
                .ToListAsync();

            return Ok(lessons);
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<ActionResult<LessonDto>> GetLesson(int id)
        {
            var lesson = await _context.Lessons.FindAsync(id);

            if (lesson == null)
            {
                return NotFound(new { message = $"Lesson with ID {id} was not found." });
            }

            return Ok(new LessonDto
            {
                LessonID = lesson.LessonID,
                LessonTitle = lesson.LessonTitle,
                Content = lesson.Content,
                CourseID = lesson.CourseID,
                LessonOrder = lesson.LessonOrder
            });
        }

        [HttpGet("course/{courseId}")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<LessonDto>>> GetLessonsByCourse(int courseId)
        {
            var courseExists = await _context.Courses.AnyAsync(c => c.CourseId == courseId);
            if (!courseExists)
            {
                return NotFound(new { message = $"Course with ID {courseId} was not found." });
            }

            var lessons = await _context.Lessons
                .Where(l => l.CourseID == courseId)
                .OrderBy(l => l.LessonOrder)   
                .Select(l => new LessonDto
                {
                    LessonID = l.LessonID,
                    LessonTitle = l.LessonTitle,
                    Content = l.Content,
                    CourseID = l.CourseID,
                    LessonOrder = l.LessonOrder
                })
                .ToListAsync();

            return Ok(lessons);
        }

        [HttpGet("search")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<LessonDto>>> SearchLessons([FromQuery] string title, [FromQuery] int? courseId)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                return BadRequest(new { message = "Search query cannot be empty." });
            }

            var searchTitle = title.Trim().ToLower();
            var query = _context.Lessons.AsQueryable();

            if (courseId.HasValue)
            {
                query = query.Where(l => l.CourseID == courseId.Value);
            }

            var lessons = await query
                .Where(l => l.LessonTitle.ToLower().Contains(searchTitle))
                .OrderBy(l => l.LessonOrder)
                .Select(l => new LessonDto
                {
                    LessonID = l.LessonID,
                    LessonTitle = l.LessonTitle,
                    Content = l.Content,
                    CourseID = l.CourseID,
                    LessonOrder = l.LessonOrder
                })
                .ToListAsync();

            if (!lessons.Any())
            {
                return NotFound(new { message = $"No lessons found matching '{title}'." });
            }

            return Ok(lessons);
        }

        [HttpGet("suggestions")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<string>>> GetLessonSuggestions([FromQuery] string query, [FromQuery] int? courseId)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2)
            {
                return Ok(new List<string>());
            }

            var searchTitle = query.Trim().ToLower();
            var lessonsQuery = _context.Lessons.AsQueryable();

            if (courseId.HasValue)
            {
                lessonsQuery = lessonsQuery.Where(l => l.CourseID == courseId.Value);
            }

            var suggestions = await lessonsQuery
                .Where(l => l.LessonTitle.ToLower().Contains(searchTitle))
                .OrderBy(l => l.LessonOrder)
                .Select(l => l.LessonTitle)
                .Distinct()
                .Take(5)
                .ToListAsync();

            return Ok(suggestions);
        }

        [HttpGet("{currentLessonId}/next")]
        [Authorize]
        public async Task<ActionResult<LessonDto>> GetNextLesson(int currentLessonId)
        {
            var current = await _context.Lessons.FindAsync(currentLessonId);
            if (current == null)
            {
                return NotFound(new { message = $"Lesson with ID {currentLessonId} was not found." });
            }

            var nextLesson = await _context.Lessons
                .Where(l => l.CourseID == current.CourseID && l.LessonOrder > current.LessonOrder)
                .OrderBy(l => l.LessonOrder)
                .FirstOrDefaultAsync();

            if (nextLesson == null)
            {
                return NotFound(new { message = "You have reached the last lesson in this course." });
            }

            return Ok(new LessonDto
            {
                LessonID = nextLesson.LessonID,
                LessonTitle = nextLesson.LessonTitle,
                Content = nextLesson.Content,
                CourseID = nextLesson.CourseID,
                LessonOrder = nextLesson.LessonOrder
            });
        }

        [HttpGet("{currentLessonId}/previous")]
        [Authorize]
        public async Task<ActionResult<LessonDto>> GetPreviousLesson(int currentLessonId)
        {
            var current = await _context.Lessons.FindAsync(currentLessonId);
            if (current == null)
            {
                return NotFound(new { message = $"Lesson with ID {currentLessonId} was not found." });
            }

            var prevLesson = await _context.Lessons
                .Where(l => l.CourseID == current.CourseID && l.LessonOrder < current.LessonOrder)
                .OrderByDescending(l => l.LessonOrder)
                .FirstOrDefaultAsync();

            if (prevLesson == null)
            {
                return NotFound(new { message = "You are currently at the first lesson in this course." });
            }

            return Ok(new LessonDto
            {
                LessonID = prevLesson.LessonID,
                LessonTitle = prevLesson.LessonTitle,
                Content = prevLesson.Content,
                CourseID = prevLesson.CourseID,
                LessonOrder = prevLesson.LessonOrder
            });
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<ActionResult<LessonDto>> CreateLesson([FromBody] LessonCreateDto dto)
        {
            var courseExists = await _context.Courses.AnyAsync(c => c.CourseId == dto.CourseID);
            if (!courseExists)
            {
                return BadRequest(new { message = $"Cannot create lesson. Course with ID {dto.CourseID} does not exist." });
            }

            var existingLessons = await _context.Lessons
                .Where(l => l.CourseID == dto.CourseID && l.LessonOrder >= dto.LessonOrder)
                .ToListAsync();

            foreach (var l in existingLessons)
            {
                l.LessonOrder += 1;
            }

            var lesson = new Lesson
            {
                LessonTitle = dto.LessonTitle,
                Content = dto.Content,
                CourseID = dto.CourseID,
                LessonOrder = dto.LessonOrder
            };

            _context.Lessons.Add(lesson);
            await _context.SaveChangesAsync();

            var resultDto = new LessonDto
            {
                LessonID = lesson.LessonID,
                LessonTitle = lesson.LessonTitle,
                Content = lesson.Content,
                CourseID = lesson.CourseID,
                LessonOrder = lesson.LessonOrder
            };

            return CreatedAtAction(nameof(GetLesson), new { id = lesson.LessonID }, resultDto);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> UpdateLesson(int id, [FromBody] LessonCreateDto dto)
        {
            var lesson = await _context.Lessons.FindAsync(id);

            if (lesson == null)
            {
                return NotFound(new { message = $"Lesson with ID {id} to update was not found." });
            }

            var courseExists = await _context.Courses.AnyAsync(c => c.CourseId == dto.CourseID);
            if (!courseExists)
            {
                return BadRequest(new { message = $"Cannot update lesson. Course with ID {dto.CourseID} does not exist." });
            }

            lesson.LessonTitle = dto.LessonTitle;
            lesson.Content = dto.Content;
            lesson.CourseID = dto.CourseID;
            lesson.LessonOrder = dto.LessonOrder;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Lesson updated successfully." });
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> DeleteLesson(int id)
        {
            var lesson = await _context.Lessons.FindAsync(id);

            if (lesson == null)
            {
                return NotFound(new { message = $"Lesson with ID {id} to delete was not found." });
            }

            _context.Lessons.Remove(lesson);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Lesson deleted successfully." });
        }
    }
}
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
    public class CoursesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CoursesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [AllowAnonymous] 
        public async Task<ActionResult<IEnumerable<CourseDto>>> GetCourses()
        {
            var courses = await _context.Courses
                .Select(c => new CourseDto
                {
                    CourseId = c.CourseId,
                    CourseName = c.CourseName,
                    Description = c.Description
                })
                .ToListAsync();

            return Ok(courses);
        }

        [HttpGet("search")]
        [Authorize] 
        public async Task<ActionResult<IEnumerable<CourseDto>>> SearchCourses([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest(new { message = "Search query cannot be empty." });
            }

            var searchQuery = query.Trim().ToLower();

            var courses = await _context.Courses
                .Where(c => c.CourseName.ToLower().Contains(searchQuery) ||
                            c.Description.ToLower().Contains(searchQuery))
                .Select(c => new CourseDto
                {
                    CourseId = c.CourseId,
                    CourseName = c.CourseName,
                    Description = c.Description
                })
                .ToListAsync();

            if (!courses.Any())
            {
                return NotFound(new { message = $"No courses found matching '{query}'." });
            }

            return Ok(courses);
        }

        [HttpGet("suggestions")]
        [Authorize] 
        public async Task<ActionResult<IEnumerable<string>>> GetCourseSuggestions([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2)
            {
                return Ok(new List<string>());
            }

            var searchQuery = query.Trim().ToLower();

            var suggestions = await _context.Courses
                .Where(c => c.CourseName.ToLower().Contains(searchQuery))
                .Select(c => c.CourseName)
                .Distinct()
                .Take(5)
                .ToListAsync();

            return Ok(suggestions);
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Teacher")] 
        public async Task<ActionResult<CourseDto>> GetCourse(int id)
        {
            var course = await _context.Courses.FindAsync(id);

            if (course == null)
            {
                return NotFound(new { message = $"Course with ID {id} was not found." });
            }

            var courseDto = new CourseDto
            {
                CourseId = course.CourseId,
                CourseName = course.CourseName,
                Description = course.Description
            };

            return Ok(courseDto);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<CourseDto>> CreateCourse(CourseCreateDto courseDto)
        {
            var course = new Course
            {
                CourseName = courseDto.CourseName,
                Description = courseDto.Description
            };

            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            var resultDto = new CourseDto
            {
                CourseId = course.CourseId,
                CourseName = course.CourseName,
                Description = course.Description
            };

            return CreatedAtAction(nameof(GetCourse), new { id = course.CourseId }, resultDto);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateCourse(int id, CourseCreateDto courseDto)
        {
            var course = await _context.Courses.FindAsync(id);

            if (course == null)
            {
                return NotFound(new { message = $"Course with ID {id} to update was not found." });
            }

            course.CourseName = courseDto.CourseName;
            course.Description = courseDto.Description;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Course updated successfully." });
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteCourse(int id)
        {
            var course = await _context.Courses.FindAsync(id);

            if (course == null)
            {
                return NotFound(new { message = $"Course with ID {id} to delete was not found." });
            }

            _context.Courses.Remove(course);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Course deleted successfully." });
        }
    }
}
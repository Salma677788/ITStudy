using System.ComponentModel.DataAnnotations;

namespace ITStudy.DTOs
{
    public class LessonCreateDto
    {
        [Required(ErrorMessage = "Lesson title is required.")]
        [StringLength(150, ErrorMessage = "Lesson title cannot exceed 150 characters.")]
        public string LessonTitle { get; set; } = string.Empty;

        [Required(ErrorMessage = "Content is required.")]
        public string Content { get; set; } = string.Empty;

        [Required(ErrorMessage = "Course ID is required.")]
        public int CourseID { get; set; }
        public int LessonOrder { get; set; } = 1;
    }

    public class LessonDto
    {
        public int LessonID { get; set; }
        public string LessonTitle { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int CourseID { get; set; }
        public int LessonOrder { get; set; } = 1;
    }
}
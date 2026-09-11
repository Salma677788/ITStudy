namespace ITStudy.DTOs
{
    public class CourseCreateDto
    {
        public string CourseName { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class CourseDto
    {
        public int CourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
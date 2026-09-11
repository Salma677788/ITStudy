namespace ITStudy.Models
{
    public class Lesson
    {
        public int LessonID { get; set; }
        public string LessonTitle { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int CourseID { get; set; }
        public int LessonOrder { get; set; } = 1;
    }
}
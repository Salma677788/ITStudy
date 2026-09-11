using System.ComponentModel.DataAnnotations;

namespace ITStudy.DTOs
{
    
    public class QuestionDto
    {
        public int QuestionID { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public List<string> Options { get; set; } = new();
        public int LessonID { get; set; }
    }

    
    public class CreateQuestionDto
    {
        [Required(ErrorMessage = "Question text is required.")]
        public string QuestionText { get; set; } = string.Empty;

        [Required(ErrorMessage = "Option A is required.")]
        public string OptionA { get; set; } = string.Empty;

        [Required(ErrorMessage = "Option B is required.")]
        public string OptionB { get; set; } = string.Empty;

        public string? OptionC { get; set; }
        public string? OptionD { get; set; }

        [Required(ErrorMessage = "Correct answer is required.")]
        public string CorrectAnswer { get; set; } = string.Empty;

        [Required(ErrorMessage = "Lesson ID is required.")]
        public int LessonID { get; set; }
    }
    
   
    public class SubmitAnswerDto
    {
        [Required(ErrorMessage = "Question ID is required.")]
        public int QuestionID { get; set; }

        [Required(ErrorMessage = "Selected answer is required.")]
        public string SelectedAnswer { get; set; } = string.Empty;
    }
}
using MathTestAssessment.MathEngine.Evaluation;
using MathTestAssessment.Modules.Examinations.Domain.Entities;
using MathTestAssessment.Modules.Examinations.Features;

namespace MathTestAssessment.UnitTests.Examinations;

public sealed class EvaluateExamServiceTests
{
    [Fact]
    public void EvaluateExam_MixedTasks_GradesEveryTaskIndependently()
    {
        var examId = Guid.NewGuid();
        var exam = new Exam
        {
            Id = examId,
            StudentId = Guid.NewGuid(),
            TeacherId = Guid.NewGuid(),
            ExternalExamId = "EXAM-1",
            Tasks =
            [
                Task(examId, "1", "2+3*4", 14m),
                Task(examId, "2", "10/2", 6m),
                Task(examId, "3", "8/0", 2m),
                Task(examId, "4", "2+3", 5m)
            ]
        };
        var service = new EvaluateExamService(new ExpressionEvaluator());

        service.Evaluate(exam);

        Assert.True(exam.Tasks[0].IsCorrect);
        Assert.Equal(14m, exam.Tasks[0].CorrectAnswer);
        Assert.False(exam.Tasks[1].IsCorrect);
        Assert.Equal(5m, exam.Tasks[1].CorrectAnswer);
        Assert.Null(exam.Tasks[2].IsCorrect);
        Assert.Equal(nameof(EvaluationErrorCode.DivisionByZero), exam.Tasks[2].EvaluationErrorCode);
        Assert.True(exam.Tasks[3].IsCorrect);
    }

    private static ExamTask Task(
        Guid examId,
        string code,
        string expression,
        decimal submittedAnswer) => new()
        {
            ExamId = examId,
            ExternalTaskId = code,
            Expression = expression,
            SubmittedAnswer = submittedAnswer
        };
}

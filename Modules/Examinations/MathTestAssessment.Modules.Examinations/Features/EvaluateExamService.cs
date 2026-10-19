using System.Text;
using MathTestAssessment.MathEngine.Evaluation;
using MathTestAssessment.Modules.Examinations.Domain.Entities;

namespace MathTestAssessment.Modules.Examinations.Features;

public class EvaluateExamService
{
    // decimal(38,18) reserves 20 digits for the integer part.
    private const decimal AnswerMagnitudeLimit = 1e20m;

    private readonly IExpressionEvaluator _evaluator;

    public EvaluateExamService(IExpressionEvaluator evaluator)
    {
        _evaluator = evaluator;
    }

    public void Evaluate(Exam exam)
    {
        ArgumentNullException.ThrowIfNull(exam);

        foreach (var task in exam.Tasks)
        {
            var result = _evaluator.Evaluate(task.Expression);

            if (!result.IsSuccess)
            {
                task.CorrectAnswer = null;
                task.IsCorrect = null;
                task.EvaluationErrorCode = result.Error?.Code.ToString();
                continue;
            }

            // Match the decimal(38,18) storage scale for repeatable comparisons.
            // Midpoints use banker's rounding; no implicit broad tolerance.
            if (result.Value is not decimal value || value <= -AnswerMagnitudeLimit || value >= AnswerMagnitudeLimit)
            {
                task.CorrectAnswer = null;
                task.IsCorrect = null;
                task.EvaluationErrorCode = "Overflow";
                continue;
            }
            task.CorrectAnswer = decimal.Round(value, 18, MidpointRounding.ToEven);
            task.SubmittedAnswer = decimal.Round(task.SubmittedAnswer, 18, MidpointRounding.ToEven);
            task.IsCorrect = task.CorrectAnswer == task.SubmittedAnswer;
            task.EvaluationErrorCode = null;
        }
    }
}



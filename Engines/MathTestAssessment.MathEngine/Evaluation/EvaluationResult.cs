using System.Text;

namespace MathTestAssessment.MathEngine.Evaluation;

public sealed record EvaluationResult
{
    public bool IsSuccess { get; init; }

    public decimal? Value { get; init; }

    public EvaluationError? Error { get; init; }

    public static EvaluationResult Success(decimal value) => new()
        {
            IsSuccess = true,
            Value = value
        };

    public static EvaluationResult Failure(EvaluationError error) => new()
        {
            IsSuccess = false,
            Error = error
        };
}

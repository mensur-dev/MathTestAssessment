using System.Text;

namespace MathTestAssessment.MathEngine.Evaluation;

public sealed record EvaluationError(
    EvaluationErrorCode Code,
    string Message
);

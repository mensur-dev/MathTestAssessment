using MathTestAssessment.MathEngine.Evaluation;

namespace MathTestAssessment.MathEngine.Exceptions;

internal sealed class ExpressionException : Exception
{
    public EvaluationErrorCode ErrorCode { get; }

    public int Position { get; }

    public ExpressionException(
        EvaluationErrorCode errorCode,
        string message,
        int position)
        : base(message)
    {
        ErrorCode = errorCode;
        Position = position;
    }
}
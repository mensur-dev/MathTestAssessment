using System.Text;

namespace MathTestAssessment.MathEngine.Evaluation;
public enum EvaluationErrorCode
{
    EmptyExpression,
    InvalidCharacter,
    InvalidNumber,
    UnexpectedToken,
    DivisionByZero,
    Overflow,
    ExpressionTooLong,
    ExpressionTooDeep
}

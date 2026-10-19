using MathTestAssessment.MathEngine.Exceptions;
using MathTestAssessment.MathEngine.Parsing;
using MathTestAssessment.MathEngine.Tokenization;

namespace MathTestAssessment.MathEngine.Evaluation;

public sealed class ExpressionEvaluator : IExpressionEvaluator
{
    private const int MaxExpressionLength = 4000;

    public EvaluationResult Evaluate(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            return Failure(EvaluationErrorCode.EmptyExpression, 
            "Expression cannot be empty.");
        }

        if (expression.Length > MaxExpressionLength)
        {
            return Failure(EvaluationErrorCode.ExpressionTooLong, 
            $"Expression cannot exceed {MaxExpressionLength} characters.");
        }

        try
        {
            var tokenizer = new ExpressionTokenizer();
            var tokens = tokenizer.Tokenize(expression);

            var parser = new ExpressionParser(tokens);
            var result = parser.Parse();

            return EvaluationResult.Success(result);
        }
        catch (ExpressionException ex)
        {
            return Failure(ex.ErrorCode, ex.Message);
        }
        catch (DivideByZeroException)
        {
            return Failure(EvaluationErrorCode.DivisionByZero, 
            "Division by zero is not allowed.");
        }
        catch (OverflowException)
        {
            return Failure(EvaluationErrorCode.Overflow, 
            "Arithmetic overflow occurred.");
        }
    }

    private static EvaluationResult Failure(
        EvaluationErrorCode code, string message)
    {
        return EvaluationResult.Failure(
            new EvaluationError(code, message));
    }
}


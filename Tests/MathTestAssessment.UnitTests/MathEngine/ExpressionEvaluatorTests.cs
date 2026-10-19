using System.Globalization;
using MathTestAssessment.MathEngine.Evaluation;

namespace MathTestAssessment.UnitTests.MathEngine;

public sealed class ExpressionEvaluatorTests
{
    private readonly ExpressionEvaluator evaluator = new();

    [Theory]
    [InlineData("2+3", "5")]
    [InlineData("10-4", "6")]
    [InlineData("3*5", "15")]
    [InlineData("8/2", "4")]
    [InlineData("2+3*4", "14")]
    [InlineData("(2+3)*4", "20")]
    [InlineData("-5+3", "-2")]
    [InlineData("10/4", "2.5")]
    public void Evaluate_ValidExpressions_ReturnsExpectedResult(
        string expression,
        string expected)
    {
        // Act
        var result = evaluator.Evaluate(expression);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(
            decimal.Parse(expected, CultureInfo.InvariantCulture),
            result.Value);
        Assert.Null(result.Error);
    }

    [Theory]
    [InlineData("10/0", EvaluationErrorCode.DivisionByZero)]
    [InlineData("2+*3", EvaluationErrorCode.UnexpectedToken)]
    [InlineData("2@3", EvaluationErrorCode.InvalidCharacter)]
    [InlineData("12.3.4", EvaluationErrorCode.InvalidNumber)]
    [InlineData("79228162514264337593543950335+1", EvaluationErrorCode.Overflow)]
    public void Evaluate_InvalidExpressions_ReturnsExpectedError(
        string expression,
        EvaluationErrorCode expectedError)
    {
        // Act
        var result = evaluator.Evaluate(expression);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);
        Assert.Equal(expectedError, result.Error?.Code);
    }

    public static TheoryData<string, EvaluationErrorCode> ResourceLimits => new()
    {
        { new string('1', 4001), EvaluationErrorCode.ExpressionTooLong },
        { new string('(', 65) + "1" + new string(')', 65), EvaluationErrorCode.ExpressionTooDeep }
    };

    [Theory]
    [MemberData(nameof(ResourceLimits))]
    public void Evaluate_ResourceLimits_ReturnsExpectedError(
        string expression,
        EvaluationErrorCode expectedError)
    {
        var result = evaluator.Evaluate(expression);

        Assert.False(result.IsSuccess);
        Assert.Equal(expectedError, result.Error?.Code);
    }
}

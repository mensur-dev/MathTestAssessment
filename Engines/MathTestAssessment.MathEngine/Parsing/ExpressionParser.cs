using System.Text;

using System.Globalization;
using MathTestAssessment.MathEngine.Tokenization;
using MathTestAssessment.MathEngine.Evaluation;
using MathTestAssessment.MathEngine.Exceptions;

namespace MathTestAssessment.MathEngine.Parsing;

internal sealed class ExpressionParser
{
    private const int MaxDepth = 64;

    private readonly IReadOnlyList<Token> _tokens;
    private int _position;
    private int _depth;

    public ExpressionParser(IReadOnlyList<Token> tokens)
    {
        ArgumentNullException.ThrowIfNull(tokens);
        _tokens = tokens;
    }

    public decimal Parse()
    {
        var result = ParseExpression();

        if (Current.Type != TokenType.EndOfInput)
        {
            throw new ExpressionException(EvaluationErrorCode.UnexpectedToken,
                $"Unexpected token '{Current.Text}' at position {Current.Position}.",
                Current.Position);
        }

        return result;
    }

    private decimal ParseExpression()
    {
        var result = ParseTerm();

        while (Current.Type is TokenType.Plus or TokenType.Minus)
        {
            var operation = Consume().Type;
            var right = ParseTerm();

            result = operation == TokenType.Plus
                ? checked(result + right)
                : checked(result - right);
        }

        return result;
    }

    private decimal ParseTerm()
    {
        var result = ParseUnary();

        while (Current.Type is TokenType.Multiply or TokenType.Divide)
        {
            var operation = Consume().Type;
            var right = ParseUnary();

            result = operation == TokenType.Multiply
                ? checked(result * right)
                : checked(result / right);
        }

        return result;
    }

    private decimal ParseUnary()
    {
        if (Current.Type is TokenType.Plus or TokenType.Minus)
        {
            var operation = Consume().Type;

            EnterDepth();

            try
            {
                var value = ParseUnary();

                return operation == TokenType.Minus
                    ? checked(-value)
                    : value;
            }
            finally
            {
                _depth--;
            }
        }

        return ParsePrimary();
    }

    private decimal ParsePrimary()
    {
        if (Current.Type == TokenType.Number)
        {
            var token = Consume();

            return decimal.Parse(
                token.Text,
                NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture);
        }

        if (Current.Type == TokenType.LeftParenthesis)
        {
            Consume();
            EnterDepth();

            try
            {
                var value = ParseExpression();

                if (Current.Type != TokenType.RightParenthesis)
                {
                    throw new ExpressionException(
                        EvaluationErrorCode.UnexpectedToken,
                        $"Missing closing parenthesis at position {Current.Position}.",
                        Current.Position);
                }

                Consume();
                return value;
            }
            finally
            {
                _depth--;
            }
        }

        throw new ExpressionException(EvaluationErrorCode.UnexpectedToken,
                    $"Unexpected token '{Current.Text}' at position {Current.Position}.",
                    Current.Position);
                    }

    private void EnterDepth()
    {
        if (_depth >= MaxDepth)
        {
            throw new ExpressionException(EvaluationErrorCode.ExpressionTooDeep,
                "Expression nesting depth exceeded.",
                Current.Position);
        }

        _depth++;
    }

    private Token Current => _tokens[_position];

    private Token Consume() => _tokens[_position++];
}

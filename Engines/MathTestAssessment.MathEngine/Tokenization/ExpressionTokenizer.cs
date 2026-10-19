using System.Globalization;
using MathTestAssessment.MathEngine.Evaluation;
using MathTestAssessment.MathEngine.Exceptions;

namespace MathTestAssessment.MathEngine.Tokenization;

internal sealed class ExpressionTokenizer
{
    public IReadOnlyList<Token> Tokenize(string expression)
    {
        ArgumentNullException.ThrowIfNull(expression);

        var tokens = new List<Token>();
        var position = 0;

        while (position < expression.Length)
        {
            var current = expression[position];

            if (char.IsWhiteSpace(current))
            {
                position++;
                continue;
            }

            if (char.IsAsciiDigit(current) || current == '.')
            {
                var start = position;
                var hasDecimalPoint = false;

                while (position < expression.Length)
                {
                    var character = expression[position];

                    if (character == '.')
                    {
                        if (hasDecimalPoint)
                        {
                            position++;

                            while (position < expression.Length &&
                                   (char.IsAsciiDigit(expression[position]) ||
                                    expression[position] == '.'))
                            {
                                position++;
                            }

                            var invalidText = expression[start..position];

                            throw new ExpressionException(EvaluationErrorCode.InvalidNumber,
                                $"Invalid number '{invalidText}' at position {start}.",
                                start);
                        }

                        hasDecimalPoint = true;
                    }
                    else if (!char.IsAsciiDigit(character))
                    {
                        break;
                    }

                    position++;
                }

                var text = expression[start..position];

                if (!decimal.TryParse(
                        text,
                        NumberStyles.AllowDecimalPoint,
                        CultureInfo.InvariantCulture,
                        out _))
                {
                    throw new ExpressionException(EvaluationErrorCode.InvalidNumber,
                        $"Invalid number '{text}' at position {start}.",
                        start);
                }

                tokens.Add(new Token(TokenType.Number, text, start));
                continue;
            }

            var type = current switch
            {
                '+' => TokenType.Plus,
                '-' => TokenType.Minus,
                '*' => TokenType.Multiply,
                '/' => TokenType.Divide,
                '(' => TokenType.LeftParenthesis,
                ')' => TokenType.RightParenthesis,

                _ => throw new ExpressionException(
                    EvaluationErrorCode.InvalidCharacter,
                    $"Invalid character '{current}' at position {position}.",
                    position)
            };

            tokens.Add(new Token(type, current.ToString(), position));
            position++;
        }

        tokens.Add(new Token(
            TokenType.EndOfInput,
            string.Empty,
            position));

        return tokens;
    }
}
using System.Text;

namespace MathTestAssessment.MathEngine.Tokenization;

internal enum TokenType
{
    Number,
    Plus,
    Minus,
    Multiply,
    Divide,
    LeftParenthesis,
    RightParenthesis,
    EndOfInput
}

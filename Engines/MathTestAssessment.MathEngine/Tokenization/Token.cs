using System.Text;

namespace MathTestAssessment.MathEngine.Tokenization;

internal readonly record struct Token(
    TokenType Type,
    string Text,
    int Position
);

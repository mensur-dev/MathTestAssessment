
namespace MathTestAssessment.Web.Models;

public static class ExamPresentation
{
    public static int Correct(ExamDto exam) =>
        exam.Tasks.Count(t => t.IsCorrect == true);

    public static int Incorrect(ExamDto exam) =>
        exam.Tasks.Count(t => t.IsCorrect == false);

    public static int Pending(ExamDto exam) =>
        exam.Tasks.Count(t => t.IsCorrect is null);

    public static decimal Score(ExamDto exam)
    {
        var evaluated = Correct(exam) + Incorrect(exam);

        return evaluated == 0
            ? 0
            : Math.Round(100m * Correct(exam) / evaluated, 2);
    }

    public static string Number(decimal? value) =>
        value?.ToString("0.############################") ?? "—";

    public static IEnumerable<ExamTaskDto> OrderedTasks(ExamDto exam) =>
        exam.Tasks
            .OrderBy(t => int.TryParse(t.ExternalTaskId, out var n) ? n : int.MaxValue)
            .ThenBy(t => t.ExternalTaskId, StringComparer.Ordinal);

    public static string Error(string? code) => code switch
    {
        "DivisionByZero" => "Division by zero",
        "Overflow" => "Number exceeds the supported range",
        "InvalidCharacter" => "Unsupported symbol in expression",
        "InvalidNumber" => "Invalid number",
        "UnexpectedToken" => "Invalid expression or unmatched parentheses",
        "EmptyExpression" => "Expression is empty",
        "ExpressionTooLong" => "Expression is too long",
        "ExpressionTooDeep" => "Too many nested parentheses",
        _ => "Not evaluated"
    };
}

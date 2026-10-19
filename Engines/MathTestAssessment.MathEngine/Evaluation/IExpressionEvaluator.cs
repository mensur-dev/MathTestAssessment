using System.Text;

namespace MathTestAssessment.MathEngine.Evaluation;

public interface IExpressionEvaluator
{
    EvaluationResult Evaluate(string expression);
}

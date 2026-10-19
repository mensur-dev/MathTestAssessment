using System.Text;
using MathTestAssessment.Modules.Imports.Features;

namespace MathTestAssessment.UnitTests.Imports;

public sealed class XmlExamParserTests
{
    private readonly XmlExamParser parser = new();

    [Fact]
    public void Parse_ValidBulkXml_ReturnsCompleteStructure()
    {
        const string xml = """
            <Teacher ID="T001">
              <Students>
                <Student ID="S001">
                  <Exam Id="EXAM-1">
                    <Task id="1">2+3 = 5</Task>
                    <Task id="2">8/2 = 4</Task>
                  </Exam>
                  <Exam Id="EXAM-2">
                    <Task id="1">3*4 = 12</Task>
                  </Exam>
                </Student>
                <Student ID="S002">
                  <Exam Id="EXAM-3">
                    <Task id="1">10-3 = 7</Task>
                  </Exam>
                </Student>
              </Students>
            </Teacher>
            """;
        using var stream = Stream(xml);

        var result = parser.Parse(stream);

        Assert.Equal("T001", result.TeacherId);
        Assert.Equal(2, result.Students.Count);
        Assert.Equal(3, result.Students.Sum(x => x.Exams.Count));
        Assert.Equal(4, result.Students.SelectMany(x => x.Exams).Sum(x => x.Tasks.Count));
        Assert.Equal("2+3 = 5", result.Students[0].Exams[0].Tasks[0].Content.Trim());
    }

    [Theory]
    [InlineData("<Teacher><Students>")]
    [InlineData("<!DOCTYPE Teacher [<!ENTITY xxe SYSTEM 'file:///tmp/test'>]><Teacher ID='T001'>&xxe;</Teacher>")]
    [InlineData("<NotTeacher />")]
    public void Parse_InvalidOrUnsafeXml_ThrowsInvalidDataException(string xml)
    {
        using var stream = Stream(xml);

        Assert.Throws<InvalidDataException>(() => parser.Parse(stream));
    }

    private static MemoryStream Stream(string value) =>
        new(Encoding.UTF8.GetBytes(value));
}

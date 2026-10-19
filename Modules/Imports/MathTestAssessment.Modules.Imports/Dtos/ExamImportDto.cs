using System.Text;
using System.Xml.Serialization;
namespace MathTestAssessment.Modules.Imports.Dtos;

[XmlRoot("Teacher")]
public class ExamImportDto
{
    [XmlAttribute("ID")]
    public string TeacherId { get; set; } = string.Empty;

    [XmlArray("Students")]
    [XmlArrayItem("Student")]
    public List<StudentImportDto> Students { get; set; } = new();
}
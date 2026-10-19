using System.Text;
using System.Xml.Serialization;

namespace MathTestAssessment.Modules.Imports.Dtos;

public class StudentImportDto
{
    [XmlAttribute("ID")]
    public string StudentId { get; set; } = string.Empty;

    [XmlElement("Exam")]
    public List<ImportedExamDto> Exams { get; set; } = new();
}

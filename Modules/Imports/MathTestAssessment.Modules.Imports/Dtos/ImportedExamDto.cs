using System.Text;
using System.Xml.Serialization;

namespace MathTestAssessment.Modules.Imports.Dtos;
public class ImportedExamDto
{
    [XmlAttribute("Id")]
    public string ExamId { get; set; } = string.Empty;

    [XmlElement("Task")]
    public List<ImportedTaskDto> Tasks { get; set; } = new();
}

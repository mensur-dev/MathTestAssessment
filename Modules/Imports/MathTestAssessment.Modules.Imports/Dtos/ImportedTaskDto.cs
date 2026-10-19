using System.Text;
using System.Xml.Serialization;

namespace MathTestAssessment.Modules.Imports.Dtos;
public class ImportedTaskDto
{
    [XmlAttribute("id")]
    public string TaskId { get; set; } = string.Empty;

    [XmlText]
    public string Content { get; set; } = string.Empty;
}
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using MathTestAssessment.Modules.Imports.Dtos;

namespace MathTestAssessment.Modules.Imports.Features;

public class XmlExamParser
{
    public ExamImportDto Parse(Stream xmlStream)
    {
        ArgumentNullException.ThrowIfNull(xmlStream);

        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersInDocument = 10_000_000
        };

        try
        {
            using var reader = XmlReader.Create(
                xmlStream, settings);

            var serializer = new XmlSerializer(
                typeof(ExamImportDto));

            var result = serializer.Deserialize(reader)
                as ExamImportDto;

            return result ?? throw new InvalidDataException(
                "Invalid XML document.");
        }
        catch (InvalidOperationException ex)
        {
            throw new InvalidDataException(
                "Invalid XML document.", ex);
        }
        catch (XmlException ex)
        {
            throw new InvalidDataException(
                "Malformed XML document.", ex);
        }
    }
}



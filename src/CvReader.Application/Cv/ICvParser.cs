namespace CvReader.Application.Cv;

public interface ICvParser
{
    ParsedCv Parse(Stream stream);
}

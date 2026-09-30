namespace CvReader.Application.Cv;

public interface ICvParser
{
    /// <exception cref="InvalidCvFileException">The stream is not a readable CV document.</exception>
    ParsedCv Parse(Stream stream);
}

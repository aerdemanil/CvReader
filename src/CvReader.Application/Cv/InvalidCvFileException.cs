namespace CvReader.Application.Cv;

public class InvalidCvFileException : Exception
{
    public InvalidCvFileException(string message, Exception? innerException = null)
        : base(message, innerException) { }
}

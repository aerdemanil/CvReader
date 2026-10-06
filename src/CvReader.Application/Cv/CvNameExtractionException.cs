namespace CvReader.Application.Cv;

public class CvNameExtractionException : Exception
{
    public CvNameExtractionException(string message, Exception? innerException = null)
        : base(message, innerException) { }
}

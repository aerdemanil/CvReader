namespace CvReader.Application.Cv;

public class ProfileSaveException : Exception
{
    public ProfileSaveException(string message, Exception? innerException = null)
        : base(message, innerException) { }
}

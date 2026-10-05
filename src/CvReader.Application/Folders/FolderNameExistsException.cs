namespace CvReader.Application.Folders;

public class FolderNameExistsException : Exception
{
    public FolderNameExistsException(string name)
        : base($"A folder named '{name}' already exists.") { }
}

namespace QuickPersistr.Tests.Trickier.Model;

public class InvalidCourseNameException : DomainException
{
    public InvalidCourseNameException() : base("Invalid course name.") { }
}
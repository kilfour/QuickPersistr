namespace QuickPersistr.Tests.Trickier.Model;

public class CourseWithoutScheduleException : DomainException
{
    public CourseWithoutScheduleException() : base("At least one day must be selected.") { }
}
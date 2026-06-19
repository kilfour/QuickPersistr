using TCMS.Domain.ValueObjects.CourseValueObjects;

namespace QuickPersistr.Tests.Trickier.Model;

public class Course : DomainEntity<Course>
{
    // (CourseName, DateRange and TimeRange) zijn ValueObjects
    public CourseName Name { get; private set; }
    public DateRange DateRange { get; private set; }
    public TimeRange TimeRange { get; private set; }
    public List<CourseDay> Days { get; private set; }
    public bool IsDeleted { get; private set; } // SOFT DELETE

    private Course() { } // Voor EF Core ?? BOX

    public Course(CourseName name, DateRange dateRange, TimeRange timeRange, List<CourseDay> days)
    {
        if (days is null || days.Count == 0)
            throw new CourseWithoutScheduleException(); // van Exception file

        Id = new Id<Course>(Guid.NewGuid()); // ID van DomainEntity file
        Name = name;
        DateRange = dateRange;
        TimeRange = timeRange;
        Days = days;
    }

    public void Delete() => IsDeleted = true; // Voor Service file (SOFT DELETE)

}


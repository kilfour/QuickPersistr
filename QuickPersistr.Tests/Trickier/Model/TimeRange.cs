namespace QuickPersistr.Tests.Trickier.Model;

public record TimeRange
{
    public TimeOnly StartTime { get; }
    public TimeOnly EndTime { get; }
    public TimeRange(TimeOnly startTime, TimeOnly endTime)
    {
        if (startTime >= endTime)
            throw new InvalidCourseTimeRangeException();

        StartTime = startTime;
        EndTime = endTime;
    }
}
namespace QuickPersistr.Tests.Trickier.Model;

public record DateRange
{
    public DateOnly StartDate { get; }
    public DateOnly EndDate { get; }

    public DateRange(DateOnly startDate, DateOnly endDate)
    {
        if (startDate > endDate)
            throw new InvalidCourseDateRangeException();

        StartDate = startDate;
        EndDate = endDate;
    }
}

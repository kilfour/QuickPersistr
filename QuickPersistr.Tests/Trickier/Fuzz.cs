using QuickCheckr;
using QuickFuzzr;
using QuickPersistr.Tests.Trickier.Model;

namespace QuickPersistr.Tests.Trickier;

public class Fuzz
{
    private static readonly FuzzrOf<Intent> CourseNames =
         Configr<CourseName>.Construct(Fuzzr.String(3, 100));

    private static readonly FuzzrOf<Intent> DateRanges =
        Configr<DateRange>.Construct(
            from start in Fuzzr.Constant(new DateOnly(2026, 1, 1))
            from duration in Fuzzr.Int(1, 30)
            select (Start: start, End: start.AddDays(duration)),
            arg => new DateRange(arg.Start, arg.End));

    private static readonly FuzzrOf<Intent> TimeRanges =
        Configr<TimeRange>.Construct(
            from start in Fuzzr.TimeOnly(new TimeOnly(8, 0), new TimeOnly(17, 0))
            from end in Fuzzr.TimeOnly(start.AddMinutes(30), new TimeOnly(23, 0))
            select (Start: start, End: end),
            arg => new TimeRange(arg.Start, arg.End));

    private static readonly FuzzrOf<Intent> CourseDays =
        Configr<CourseDay>.Construct(Fuzzr.Enum<CourseWeekDay>(), Fuzzr.Enum<LearningMode>());

    private static readonly FuzzrOf<Intent> Courses =
        Configr<Course>.Construct(
            Fuzzr.One<CourseName>(),
            Fuzzr.One<DateRange>(),
            Fuzzr.One<TimeRange>(),
            Fuzzr.One<CourseDay>().Many(1, 5).ToList());

    public readonly static FuzzrOf<Intent> TheDomain =
        Configr.Combine(
            Configr.Primitive(Fuzzr.Constant(true)),
            CourseNames,
            DateRanges,
            TimeRanges,
            CourseDays,
            Courses);
}

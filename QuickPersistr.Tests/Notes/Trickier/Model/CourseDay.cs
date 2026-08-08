namespace QuickPersistr.Tests.Trickier.Model;

/*
IN COURSE Entity
{
Monday : OnCampus 
Wednesday : RemotLearning
}
new CourseDay(CourseWeekDay.Monday, LearningMode.OnCampus); 
*/
public record CourseDay
//  RECORD >> Monday + OnCampus >>>> zijn niet Entity (Zonder ID)
{
    public CourseWeekDay Day { get; }
    public LearningMode Mode { get; }

    public CourseDay(CourseWeekDay day, LearningMode mode)
    {
        Day = day;
        Mode = mode;
    }
}
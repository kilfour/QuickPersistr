namespace QuickPersistr.Tests.Notes.BackToSchool.Model;

public class Course
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public List<Student> Students { get; set; } = [];
}

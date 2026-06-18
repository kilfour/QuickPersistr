namespace QuickPersistr.Tests.BackToSchool.Model;

public class Course
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;

    public List<Student> Students { get; set; } = [];
}

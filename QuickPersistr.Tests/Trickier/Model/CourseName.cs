namespace QuickPersistr.Tests.Trickier.Model;

// RECORD >> Because the record compares values ​​(Value Equality), not the reference.
public record CourseName
{
    public string Value { get; }
    public CourseName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidCourseNameException();

        value = value.Trim();

        if (value.Length < 3)
            throw new InvalidCourseNameException();

        if (value.Length > 100)
            throw new InvalidCourseNameException();

        Value = value;
    }

    // Console.WriteLine(courseName.Value); OLD
    public override string ToString() => Value;
    // Console.WriteLine(courseName); NEW

}
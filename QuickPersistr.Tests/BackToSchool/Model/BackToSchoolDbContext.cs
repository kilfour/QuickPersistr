using Microsoft.EntityFrameworkCore;

namespace QuickPersistr.Tests.BackToSchool.Model;

public class BackToSchoolDbContext(DbContextOptions<BackToSchoolDbContext> options)
    : DbContext(options)
{
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Student> Students => Set<Student>();
}
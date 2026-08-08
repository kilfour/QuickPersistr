using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuickPersistr.Tests.Trickier.Model;

namespace QuickPersistr.Tests.Trickier;

public class TrickierDbContext(DbContextOptions<TrickierDbContext> options)
    : DbContext(options)
{
    public DbSet<Course> Courses => Set<Course>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new CourseConfiguration());
    }
}



public class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> builder)
    {
        /*
        courses 
        id ...
        Name ...
        StartDate ...
        EndDate ....
        StartTime ...
        EndTime ....
        IsDeleted ....
        CourseDays [
        id {
        CourseId ...
        Day ...
        LearningMode .... 
        }
        ]
        */
        // Table name in DB
        builder.ToTable("Courses");
        // Primary key.
        builder.HasKey(x => x.Id);
        // Convert custom Id to Guid and back
        builder.Property(x => x.Id).HasConversion(id => id.Value, value => new Id<Course>(value));
        /*
        id<Course> is VALUE OBJECT
        id => id.Value >> Save... DB: Id<Course> -> Guid
        value => new Id<Course>(Value) Read... DB: Guid -> Id<Cours>
        */

        // Required soft delete
        builder.Property(x => x.IsDeleted).IsRequired();
        // Config CourseName value object
        builder.OwnsOne(x => x.Name, name =>
        {
            name.Property(x => x.Value).HasColumnName("Name").HasMaxLength(100).IsRequired(); // Save Name.Value in column Name.
        });

        // Config DateRange value object.
        builder.OwnsOne(x => x.DateRange, dates =>
        {
            dates.Property(x => x.StartDate).HasColumnName("StartDate"); // Start date column
            dates.Property(x => x.EndDate).HasColumnName("EndDate"); // End date column
        });

        // Config TimeRange value object.
        builder.OwnsOne(x => x.TimeRange, times =>
            {
                times.Property(x => x.StartTime).HasColumnName("StartTime"); // Start time column
                times.Property(x => x.EndTime).HasColumnName("EndTime"); // End time column
            });

        // Config collection (LIST) of course days.
        builder.OwnsMany(x => x.Days, days =>
            {
                days.ToTable("CourseDays"); // Separate table for course days.
                days.WithOwner().HasForeignKey("CourseId"); // FOREIGN KEY to COURSE table.
                days.Property<int>("Id"); // PRIMARY KEY for CourseDays table.
                days.HasKey("Id");
                days.Property(x => x.Day).HasColumnName("Day"); // Store weekday.
                days.Property(x => x.Mode).HasColumnName("LearningMode"); // Store learning mode.
            });
    }
}

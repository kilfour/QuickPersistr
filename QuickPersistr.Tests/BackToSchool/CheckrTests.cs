using Microsoft.EntityFrameworkCore;
using QuickCheckr;
using QuickFuzzr;
using QuickPersistr.Tests.BackToSchool.Model;

namespace QuickPersistr.Tests.BackToSchool;

public class CheckrTests
{
    [Fact]
    public void TestIt() => (
        from db in Trackr.Stashed(() => new BackToSchoolPersistenceScope())
        from _ in Checkr.Sequence(
            CreateCheckr(db),
            Trackr.OneOfPool<Course>("Course Read", info => ReadCheckr(info, db)),
            Trackr.OneOfPool<Course>("Course Update", info => UpdateCheckr(db, info)),
            Trackr.OneOfPool<Course>("Course Delete", info => DeleteCheckr(db, info)))
        select Case.Closed)
            .Configure(a => a with { FileAs = "test" })
            .Run(4.ExecutionsPerRun());


    private static readonly FuzzrOf<Course> Creator =
        from ignore in Configr.Ignore(a => a.Name == "Id")
        from entity in Fuzzr.One<Course>()
        select entity;

    private static FuzzrOf<Course> Modifier(Course course) =>
        from ignore in Configr.Ignore(a => a.Name == "Id")
        from entity in Fuzzr.One(() => course)
        select entity;

    private static CheckrOf<Case> CreateCheckr(BackToSchoolPersistenceScope db) =>
        from entity in Checkr.Input("Entity", Creator)
        from create in Checkr.Act("Create", () =>
        {
            db.Context.Courses.Add(entity);
            db.Commit();
            return entity;
        })
        from canCreate in Checkr.Expect("Can Create", () => create.Id != default)
        from stored in Trackr.ToPool("Entity", () => create)
        select Case.Closed;

    private static CheckrOf<Case> ReadCheckr(PoolElement<Course> info, BackToSchoolPersistenceScope db) =>
        from course in Checkr.Act("Read", () => db.Context.Courses.AsNoTracking().Single(a => info.Value.Id == a.Id))
        from canRead in Checkr.Expect("Can Read", () => info.Value.Title == course.Title)
        select Case.Closed;

    private static CheckrOf<Case> UpdateCheckr(BackToSchoolPersistenceScope db, PoolElement<Course> info) =>
        from course in Checkr.Capture(() => db.Context.Courses.Single(a => a.Id == info.Value.Id))
        from updatedCourse in Checkr.Input("Updated Course", Modifier(course))
        from updated in Checkr.Act("Update", db.Commit)
        from reloaded in Checkr.Capture(
            () => db.Context.Courses.AsNoTracking().Single(a => info.Value.Id == a.Id))
        from canUpdate in Checkr.Expect("Can Update", () => updatedCourse.Title == reloaded.Title)
        from stored in info.Replace(reloaded)
        select Case.Closed;

    private static CheckrOf<Case> DeleteCheckr(BackToSchoolPersistenceScope db, PoolElement<Course> info) =>
        from delete in Checkr.Act("Delete",
            () =>
            {
                db.Context.Courses.Where(a => info.Value.Id == a.Id).ExecuteDelete();
                db.Commit();
            })
        from reloaded in Checkr.Capture(
            () => db.Context.Courses.SingleOrDefault(a => info.Value.Id == a.Id))
        from canDelete in Checkr.Expect("Can Delete", () => reloaded is null)
        from stored in info.Remove()
        select Case.Closed;
}
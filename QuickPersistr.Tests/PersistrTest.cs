using System.Diagnostics;
using System.Runtime.CompilerServices;
using QuickCheckr.Authoring;
using QuickCheckr.Authoring.ThePress;
using QuickCheckr.Authoring.ThePress.Printing;
using QuickCheckr.UnderTheHood;

namespace QuickPersistr.Tests;

public abstract class PersistrTest<T> : QuickCheckrTest<T>
{
    protected override bool WriteAllReportsToDisk { get; } = false;

    protected class DocPersistrHeaderAttribute() :
        DocBoldHeaderAttribute("The Persistr");

    protected Journalist TheJournalist = new();

    [StackTraceHidden]
    protected void Document(
        Action run,
        Action<Article> verifier,
        [CallerFilePath] string callerPath = "")
    {
        try { run(); } catch (FalsifiableException) { }
        var article = TheJournalist.GetArticle();
        ProcessArticle(article, callerPath);
        verifier(article);
    }

    // [StackTraceHidden]
    // protected void Document(
    //     ConfiguredCheckr checkr,
    //     Action<ConfiguredCheckr> runCheckr,
    //     Action<Article> verifier,
    //     [CallerFilePath] string callerPath = "")
    // {
    //     var article = Journalist.Publish(checkr, runCheckr);
    //     ProcessArticle(article, callerPath);
    //     verifier(article);
    // }
}
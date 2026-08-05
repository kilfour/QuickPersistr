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

    public abstract void Example();
    protected abstract void Verify(Article article);

    protected abstract void GetPersistr(Journalist journalist);

    protected void Document([CallerFilePath] string callerPath = "")
    {
        var journalist = new Journalist();
        try { GetPersistr(journalist); }
        catch (FalsifiableException) { }
        var article = journalist.GetArticle();
        ProcessArticle(article, callerPath);
        Verify(article);
    }
}
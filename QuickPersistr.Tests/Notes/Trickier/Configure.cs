using QuickCheckr;
using QuickFuzzr;

namespace QuickPersistr.Tests.Trickier;

public static class Configure
{
    public static FuzzrOf<Intent> This(this FuzzrOf<Intent> current) => current;
    public static FuzzrOf<Intent> And(this FuzzrOf<Intent> current, FuzzrOf<Intent> next) =>
        from a in current
        from b in next
        select Intent.Fixed;

    public static FuzzrOf<T> One<T>(this FuzzrOf<Intent> cfg) =>
        from _ in cfg
        from one in Fuzzr.One<T>()
        select one;
}

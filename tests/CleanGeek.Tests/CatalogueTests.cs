using CleanGeek.Core.Models;
using CleanGeek.Core.Services;

namespace CleanGeek.Tests;

public static class CatalogueTests
{
    public static void Run()
    {
        Check.Section("The catalogue");

        Check.That("has targets", Catalogue.All.Count > 0);
        Check.That("gives every target a unique id",
            Catalogue.All.Select(t => t.Id).Distinct(StringComparer.Ordinal).Count() == Catalogue.All.Count);
        Check.That("gives every target a title", Catalogue.All.All(t => t.Title.Length > 0));
        Check.That("says what every target is", Catalogue.All.All(t => t.What.Length > 0));

        Check.That("has no registry target",
            !Catalogue.All.Any(t => t.Id.Contains("registry", StringComparison.OrdinalIgnoreCase)
                                 || t.Title.Contains("registry", StringComparison.OrdinalIgnoreCase)));
        Check.That("has no password target",
            !Catalogue.All.Any(t => t.Title.Contains("password", StringComparison.OrdinalIgnoreCase)
                                 || t.What.Contains("password", StringComparison.OrdinalIgnoreCase)));

        Check.Section("Defaults - the most important rule in the application");

        foreach (var t in Catalogue.All.Where(t => t.TickedByDefault))
        {
            Check.That($"'{t.Title}' is ticked by default and only rebuilds or is disposable",
                t.Risk is CleanupRisk.Rebuilds or CleanupRisk.Disposable);
        }

        Check.That("nothing Costly is ticked by default",
            !Catalogue.All.Any(t => t.TickedByDefault && t.Risk == CleanupRisk.Costly));
        Check.That("nothing Irreversible is ticked by default",
            !Catalogue.All.Any(t => t.TickedByDefault && t.Risk == CleanupRisk.Irreversible));
        Check.That("the Recycle Bin is not ticked by default",
            Catalogue.ById(Catalogue.RecycleBinId)!.TickedByDefault == false);
        Check.That("cookies are not ticked by default",
            Catalogue.ById("browser-cookies")!.TickedByDefault == false);
        Check.That("the browser cache IS ticked by default",
            Catalogue.ById("browser-cache")!.TickedByDefault);

        Check.Section("Every target with a cost says what it is");

        foreach (var t in Catalogue.All.Where(t => t.Risk is CleanupRisk.Costly or CleanupRisk.Irreversible))
            Check.That($"'{t.Title}' spells out what it costs", t.Cost.Length > 20);

        Check.Section("Looking targets up");

        Check.That("finds a known target", Catalogue.ById("temp-user") is not null);
        Check.That("returns null for an unknown target", Catalogue.ById("no-such-target") is null);
        Check.That("is case sensitive about ids", Catalogue.ById("TEMP-USER") is null);
        Check.That("groups by category",
            Catalogue.InCategory(CleanupCategory.Browsers).All(t => t.Category == CleanupCategory.Browsers));
        Check.That("puts the bin in its own category",
            Catalogue.InCategory(CleanupCategory.Bin).Count == 1);

        Check.Section("Resolving what was saved");

        Check.That("no saved selection means the defaults",
            Catalogue.Resolve(null).SequenceEqual(Catalogue.DefaultSelection()));
        Check.Equal("an empty saved selection stays empty", 0, Catalogue.Resolve([]).Count);
        Check.Equal("a saved selection is honoured", 1, Catalogue.Resolve(["browser-cookies"]).Count);
        Check.That("an unknown id is dropped rather than carried",
            !Catalogue.Resolve(["browser-cookies", "target-from-the-future"]).Contains("target-from-the-future"));
        Check.Equal("duplicates collapse", 1, Catalogue.Resolve(["temp-user", "temp-user"]).Count);

        Check.Section("The refusals are documented in the code, not just the README");

        Check.That("there are refusals recorded", Catalogue.NeverCleaned.Count >= 5);
        Check.That("each refusal explains itself", Catalogue.NeverCleaned.All(n => n.Why.Length > 40));
        Check.That("the registry is one of them",
            Catalogue.NeverCleaned.Any(n => n.Thing.Contains("registry", StringComparison.OrdinalIgnoreCase)));
        Check.That("saved passwords are one of them",
            Catalogue.NeverCleaned.Any(n => n.Thing.Contains("password", StringComparison.OrdinalIgnoreCase)));
        Check.That("Prefetch is one of them",
            Catalogue.NeverCleaned.Any(n => n.Thing.Contains("Prefetch", StringComparison.OrdinalIgnoreCase)));
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Indexing.for_AuthoringIndex.given;

public static class event_target_applications
{
    internal static readonly (string Slice, string Event, string Projection)[] Catalog =
    [
        ("PublishProduct", "ProductPublished", "ProductPublisher"),
        ("PublishPriceChange", "ProductPriceChanged", "PriceChangePublisher"),
        ("PublishRetirement", "ProductRetired", "RetirementPublisher")
    ];

    internal static Dictionary<string, string> CatalogSources() => new()
    {
        ["a-targets.play"] = string.Join('\n', Catalog.Select(value => $"module Merchandising\n  feature Publication\n    slice Translate {value.Slice}\n      direction outbound\n      projection {value.Projection} => {value.Event}\n        from Input")),
        ["z-events.play"] = string.Join('\n', Catalog.Select(value => $"module Merchandising\n  feature Publication\n    slice Translate {value.Slice}\n      public event {value.Event}\n        value String\n      event Input\n        value String"))
    };

    internal static IEnumerable<(string Name, Dictionary<string, string> Sources, string Target, string[] Candidates, bool Implicit, bool Builder)> Boundaries()
    {
        const string Local = "      public event E\n";
        const string Projection = "      projection P => E\n        from Input\n";
        const string OtherSlice = "    slice StateView Other\n      readmodel E\n";
        yield return Case("same slice readmodel", Local + "      readmodel E\n" + Projection, candidates: ["M.F.Publish.E"]);
        yield return Case("later sibling readmodel", Local + Projection, candidates: ["M.F.Other.E"], suffix: OtherSlice);
        yield return Case("nested feature readmodel", Local + Projection, candidates: ["M.F.Nested.Other.E"], suffix: "    feature Nested\n      slice StateView Other\n        readmodel E\n");
        yield return Case("other module readmodel", Local + Projection, candidates: ["N.G.Other.E"], suffix: "module N\n  feature G\n    slice StateView Other\n      readmodel E\n");
        var separate = Case("later document readmodel", Local + Projection, candidates: ["N.G.Other.E"]);
        separate.Sources["z-readmodel.play"] = "module N\n  feature G\n    slice StateView Other\n      readmodel E\n";
        yield return separate;
        yield return InferredViewCase("nonlocal event", Projection, "    slice StateChange Other\n      event E\n");
        var qualified = Case("qualified target", Local + Projection.Replace("=> E", "=> M.F.Publish.E", StringComparison.Ordinal));
        qualified.Target = "M.F.Publish.E";
        qualified.Builder = false;
        yield return qualified;
        yield return Case("duplicate local events", Local + "      event E\n" + Projection);
        yield return Case("duplicate nonlocal event", Local + Projection, suffix: "    slice StateChange Other\n      event E\n");
        yield return InferredViewCase("missing target", Projection);
        yield return Case("case only readmodel", Local + "      readmodel e\n" + Projection);
        yield return InferredViewCase("case only event", "      public event e\n" + Projection);
        yield return Case("ordinary explicit view", "      readmodel E\n" + Projection, candidates: ["M.F.Publish.E"]);
        yield return InferredViewCase("inferred view", Projection);
        var fallback = Case("projection name fallback", "      public event P\n      projection P\n        from Input\n");
        fallback.Target = "P";
        yield return fallback;
        yield return InferredViewCase("event reducer", Local + "      reducer P => E\n        on Input\n          value = value\n");
        yield return InferredViewCase("projection variant", Local + "      projection P => E\n        variant V\n          enters on Input\n", target: "V");
    }

    static (string Name, Dictionary<string, string> Sources, string Target, string[] Candidates, bool Implicit, bool Builder) Case(
        string name, string body, string[]? candidates = null, string suffix = "") =>
        (name, new() { ["model.play"] = "module M\n  feature F\n    slice Translate Publish\n      direction outbound\n" + body + "      event Input\n" + suffix }, "E", candidates ?? [], false, true);

    static (string Name, Dictionary<string, string> Sources, string Target, string[] Candidates, bool Implicit, bool Builder) InferredViewCase(string name, string body, string suffix = "", string target = "E")
    {
        var result = Case(name, body, candidates: [$"M.F.Publish.{target}"], suffix: suffix);
        result.Target = target;
        result.Implicit = true;

        return result;
    }
}

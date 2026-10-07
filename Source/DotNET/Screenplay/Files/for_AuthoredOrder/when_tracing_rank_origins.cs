// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Files.for_AuthoredOrder;

public class when_tracing_rank_origins : Specification
{
    [Fact]
    void should_trace_the_first_successful_import_and_each_declaration_ancestor()
    {
        var compiler = new ScreenplayCompiler();
        var source = new InMemoryPlayDocumentSource(new Dictionary<string, string>
        {
            ["application.play"] = "import \"z.play\"\nimport \"*.play\"\n",
            ["a.play"] = "module A\n  feature F\n    slice StateView View\n",
            ["z.play"] = "module Z\n  feature F\n    slice StateChange Write\n"
        });
        var (documents, _) = PlayImports.Resolve(["application.play"], source, compiler.Languages);
        var ranks = AuthoredOrder.Record(["application.play"], documents, compiler.Languages, out var origins);
        ranks.Keys.Order(StringComparer.Ordinal).ShouldEqual(origins.Keys.Order(StringComparer.Ordinal));
        var writer = origins[AuthoredOrder.Key(["Z", "F", "Write"])];
        writer[0].Path.ShouldEqual("application.play");
        writer[0].Location.Line.ShouldEqual(1);
        writer[0].TargetPath.ShouldEqual("z.play");
        writer.Skip(1).Select(step => step.Node.GetType().Name).ShouldEqual(["ModuleSyntax", "FeatureSyntax", "SliceSyntax"]);
        var reader = origins[AuthoredOrder.Key(["A", "F", "View"])];
        reader[0].Location.Line.ShouldEqual(2);
        reader[0].MatchIndex.ShouldEqual(0);
        AuthoredOrder.Record(["application.play"], documents, compiler.Languages).ShouldEqual(ranks);
    }
}

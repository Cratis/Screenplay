// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_indexing_operation_ownership_through_barrel_imports
{
    static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void should_use_only_resolved_physical_placement_for_details_and_all_reference_forms(bool reverse, bool ambiguous)
    {
        var documents = new[]
        {
            Document("application.play", "system Mailer\nmodule M\n  feature F\n    import \"barrel.play\"\n" + (ambiguous ? "  feature G\n    import \"barrel.play\"\n" : string.Empty)),
            Document("barrel.play", "import \"slice.play\"\n"),
            Document("slice.play", "slice StateChange S\n  operation Send\n    uses Mailer\n    compensate\n  command Ask\n    produces Send\n  specification Check\n    given operation Send fails\n    when Ask\n    then operation Send\n    then compensated Send\n")
        };
        var workspace = ScreenplayWorkspace.Create("Example", [.. reverse ? documents.Reverse() : documents], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Example")));
        var analysis = McpWorkspaceAnalysis.For(workspace);
        var operations = analysis.OperationIntents.Entries.Where(entry => entry.Node is OperationSyntax).ToArray();
        if (ambiguous)
        {
            operations.ShouldBeEmpty();
            analysis.Syntax.UnresolvedPlacementDocuments.Select(document => document.Path.Value).Order(StringComparer.Ordinal)
                .SequenceEqual(["barrel.play", "slice.play"]).ShouldBeTrue();
            analysis.Source.Index.Declarations.Where(declaration => declaration.Kind == "Operation").ShouldBeEmpty();
            analysis.OperationIntents.Productions().ShouldBeEmpty();
            return;
        }

        analysis.Source.Compilation.Success.ShouldBeTrue();
        var operation = operations.Single();
        analysis.OperationIntents.AmbiguousOwner(operation).ShouldBeFalse();
        var summary = JsonSerializer.SerializeToElement(analysis.OperationIntents.Summary(operation), Options);
        summary.GetProperty("scope").EnumerateArray().Select(segment => segment.GetString()).SequenceEqual(["M", "F", "S"]).ShouldBeTrue();
        summary.GetProperty("handle").GetProperty("documentId").GetString().ShouldEqual(documents[2].Id.ToString());
        var references = analysis.Source.Index.References.Where(reference => reference.Name == "Send").ToArray();
        references.Length.ShouldEqual(4);
        foreach (var reference in references)
        {
            var edge = new McpReferenceEdge(reference, analysis.Source.Index.Resolve(reference));
            edge.Resolution.ShouldEqual("resolved");
            edge.Targets.Single().Address.ShouldEqual("M.F.S.Send");
            edge.Targets.Single().Location.Path.ShouldEqual("slice.play");
        }
    }

    static WorkspaceDocument Document(string path, string source) => WorkspaceDocument.Create(path, PortablePlayPath.Parse(path), Encoding.UTF8.GetBytes(source));
}

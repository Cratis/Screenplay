// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;
using Cratis.Screenplay.Syntax.Specifications;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_inserting_a_stream_before_a_referenced_composite_stream : given.an_authoring_connection
{
    const string Source = """
        eventsource Account
          identifier String
          stream Ledger
            streamId
              account String
              period String
        module M
          feature F
            slice StateChange S
              event Recorded
              command Record
                account String identifier
                period String
                stream Account.Ledger
                  streamId
                    period = period
                    account = account
                produces Recorded
                  for account
              specification Routed
                when Record
                  account = "a"
                  period = "p"
                then Recorded
                  stream Account.Ledger
                    streamId
                      account = "a"
                      period = "p"
        """;

    [Theory]
    [InlineData("propose-source")]
    [InlineData("propose-ast")]
    public void should_accept_the_insertion_without_changing_existing_references(string tool)
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), Source);
        Initialize();
        var opened = Open();
        var revision = opened.GetProperty("revision").GetString();
        var arguments = new Dictionary<string, object?>
        {
            ["expectedRevision"] = revision,
            ["expectedCatalogRevision"] = opened.GetProperty("catalogRevision").GetString(),
            ["formatting"] = "CanonicalizeTouchedDocuments"
        };
        if (tool == "propose-source")
        {
            arguments["documents"] = new[] { new
            {
                operation = "replace-document",
                documentId = Page("documents", revision)[0].GetProperty("documentId").GetString(),
                source = Source.Replace("  stream Ledger", "  stream Earlier\n    streamId String\n  stream Ledger", StringComparison.Ordinal)
            } };
        }
        else
        {
            arguments["operations"] = new[] { new
            {
                operation = "add",
                parent = Node("EventSourceSyntax", revision).GetProperty("handle"),
                member = "streams",
                index = 0,
                node = new { kind = "EventStreamSyntax", name = "Earlier", streamId = new { kind = "TypeRefSyntax", name = "String", isOptional = false, isCollection = false } }
            } };
        }
        var proposal = Result(tool, arguments);
        proposal.GetProperty("success").GetBoolean().ShouldBeTrue();
        proposal.GetProperty("introducedExecutableErrors").GetArrayLength().ShouldEqual(0);
        proposal.GetProperty("authoringDiagnosticSeverities").GetProperty("codes").EnumerateArray()
            .All(code => code.GetString() == "PLAY0288").ShouldBeTrue();
        var candidate = Candidate(proposal);
        candidate.Compilation.Success.ShouldBeTrue();
        var after = WorkspaceSyntaxIndex.Create(candidate).Entries;
        after.Select(entry => entry.Node).OfType<EventStreamSyntax>().Select(stream => stream.Name).ShouldEqual(["Earlier", "Ledger"]);
        var original = ScreenplayWorkspace.Create("A", [WorkspaceDocument.Create("application", PortablePlayPath.Parse("application.play"), Encoding.UTF8.GetBytes(Source))], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("A")));
        var before = WorkspaceSyntaxIndex.Create(original).Entries.Select(entry => entry.Node).ToArray();
        var originalCommand = before.OfType<CommandStreamSyntax>().Single();
        var originalRoute = before.OfType<SpecificationStreamSyntax>().Single();
        SyntaxJson.StructurallyEqual(originalCommand, after.Select(entry => entry.Node).OfType<CommandStreamSyntax>().Single()).ShouldBeTrue();
        SyntaxJson.StructurallyEqual(originalRoute, after.Select(entry => entry.Node).OfType<SpecificationStreamSyntax>().Single()).ShouldBeTrue();
        File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldEqual(Source);
    }
}

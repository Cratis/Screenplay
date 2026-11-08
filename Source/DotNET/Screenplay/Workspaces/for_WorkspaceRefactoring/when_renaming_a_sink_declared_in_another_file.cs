// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring;

public class when_renaming_a_sink_declared_in_another_file : given.a_refactoring_workspace
{
    const string Root = """
        sink Reporting for readmodels
        import "reports.play"
        """;

    const string Reports = """
        concept Customer : String
        module M
          feature F
            slice StateView S
              readmodel Row
                sink Reporting
                id String key
              readmodel Customers
                sink Customer
                id String key
              projection BuildRow => Row
                from Recorded
                  id = $eventSourceId
              event Recorded
                value String
        """;

    WorkspaceAuthoringResult _result = null!;

    void Establish() => Workspace = ScreenplayWorkspace.Create(
        "Records",
        [Document("root", "application.play", Root), Document("reports", "reports.play", Reports)],
        SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Records")));

    void Because() => _result = Workspace.ProposeRename(Rename<SinkSyntax>("Reporting", "Analytics"));

    [Fact] void should_accept() => string.Join(" | ", _result.Conflicts.Select(conflict => conflict.Message)).ShouldEqual(string.Empty);
    [Fact] void should_rename_the_declaration() => Text("application.play").ShouldEqual(Root.Replace("Reporting", "Analytics", StringComparison.Ordinal));
    [Fact] void should_rename_the_directive_in_the_other_file() => Text("reports.play").ShouldContain("        sink Analytics\n");
    [Fact] void should_leave_a_property_named_sink_untouched() => Text("reports.play").ShouldEqual(Reports.Replace("sink Reporting", "sink Analytics", StringComparison.Ordinal));
    [Fact] void should_resolve_the_renamed_directive_in_the_index() => WorkspaceSyntaxIndex.Create(_result.Workspace).Entries.Select(entry => entry.Node).OfType<ReadModelSyntax>().Single(model => model.Name == "Row").Sink.ShouldEqual("Analytics");
    [Fact] void should_keep_every_sink_reference_resolved() => new WorkspaceReferenceBindings(WorkspaceSyntaxIndex.Create(_result.Workspace)).Bindings.Where(binding => binding.Reference.Domain == WorkspaceReferenceDomain.Sink).All(binding => binding.Target is not null).ShouldBeTrue();

    [Fact]
    void should_still_store_the_read_model_in_the_renamed_sink()
    {
        string.Join(" | ", _result.Workspace.Compilation.Diagnostics.Where(diagnostic => diagnostic.Severity == Diagnostics.DiagnosticSeverity.Error).Select(diagnostic => $"{diagnostic.Code} {diagnostic.Message}")).ShouldEqual(string.Empty);
        var compilation = _result.Workspace.Compilation.Value!;
        var row = compilation.Model.Application.Modules.Single().Features.Single().Slices.Single().ReadModels.Single(model => model.Name == "Row");
        compilation.ReadModelSinks[row.Id].ShouldEqual("Analytics");
    }

    string Text(string path) => _result.Workspace.Documents.Single(document => document.Path.Value == path).Text;
}

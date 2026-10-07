// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring;

public class when_renaming_specification_examples : given.a_refactoring_workspace
{
    const string Source = """
        module Records
          feature Entries
            slice StateChange Record
              command RecordItem
                count Int
                produces ItemRecorded
                  count = count
              event ItemRecorded
                count Int
              readmodel Item
                count Int
              example Recorded : ItemRecorded
                count = 1
              example Input : RecordItem
                count = 1
              example View : Item
                count = 1
              specification Recording
                given Recorded
                given readmodel View
                when Input count = 2
                then Recorded count = 2
                then readmodel View count = 2
              specification Appending
                when append Recorded count = 2
                then Recorded count = 2
        """;
    WorkspaceAuthoringResult _result = null!;

    void Establish() => Workspace = ScreenplayWorkspace.Create("Records", [Document("records", "application.play", Source)], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Records")));

    void Because() => _result = Workspace.ProposeRename(Rename<SpecificationExampleSyntax>("Recorded", "Existing"));

    [Fact] void should_accept() => _result.Accepted.ShouldBeTrue();
    [Fact] void should_rename_each_reference() => _result.Workspace.Documents.Single().Text.ShouldEqual(Source.Replace("Recorded :", "Existing :", StringComparison.Ordinal).Replace("given Recorded", "given Existing", StringComparison.Ordinal).Replace("then Recorded", "then Existing", StringComparison.Ordinal).Replace("append Recorded", "append Existing", StringComparison.Ordinal));
    [Fact] void should_not_allocate_semantic_example_identities() => WorkspaceSyntaxIndex.Create(_result.Workspace).Entries.Single(entry => entry.Node is SpecificationExampleSyntax example && example.Name == "Existing").SemanticId.ShouldBeNull();

    [Theory]
    [InlineData("command", "RecordItem", "RecordEntry")]
    [InlineData("event", "ItemRecorded", "EntryRecorded")]
    [InlineData("readmodel", "Item", "Entry")]
    [InlineData("example", "Input", "DefaultInput")]
    [InlineData("example", "View", "DefaultView")]
    [InlineData("specification", "Recording", "RecordingAnItem")]
    void should_preserve_underlying_type_and_example_bindings(string kind, string before, string after)
    {
        var index = WorkspaceSyntaxIndex.Create(Workspace);
        var target = index.Entries.Single(entry => WorkspaceReferenceBindings.Name(entry.Node) == before && kind switch
        {
            "command" => entry.Node is CommandSyntax,
            "event" => entry.Node is EventSyntax,
            "readmodel" => entry.Node is ReadModelSyntax,
            "example" => entry.Node is SpecificationExampleSyntax,
            _ => entry.Node is SpecificationSyntax
        });
        var result = Workspace.ProposeRename(new()
        {
            ExpectedRevision = Workspace.Revision,
            ExpectedCatalogRevision = Workspace.IdentityCatalog.Revision,
            Target = target.Handle,
            ExpectedName = before,
            NewName = after,
            EventNeverPersisted = true
        });
        Assert.True(result.Accepted, string.Join("; ", result.Conflicts.Select(conflict => conflict.Message)));
        result.Workspace.Documents.Single().Text.ShouldContain(after);
        new WorkspaceReferenceBindings(WorkspaceSyntaxIndex.Create(result.Workspace)).Bindings.Where(binding => binding.Reference.Domain == WorkspaceReferenceDomain.Fixture).All(binding => binding.Target is not null).ShouldBeTrue();
    }
}

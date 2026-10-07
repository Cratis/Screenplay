// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring;

public class when_a_reaction_names_an_event_and_trigger : given.a_refactoring_workspace
{
    [Theory]
    [InlineData("event Created", "EventSyntax")]
    [InlineData("import External.Created", null)]
    void should_bind_the_event_before_the_declared_trigger(string declaration, string? targetKind)
    {
        var source = "trigger Created\n" + (declaration.StartsWith("import", StringComparison.Ordinal) ? declaration + "\n" : string.Empty) +
            "module App\n  feature F\n    slice Automation S\n" + (targetKind is null ? string.Empty : "      " + declaration + "\n") + "      reaction React\n        when Created";
        Workspace = ScreenplayWorkspace.Create("App", [Document("model", "model.play", source)], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("App")));
        var binding = new WorkspaceReferenceBindings(WorkspaceSyntaxIndex.Create(Workspace)).Bindings.Single(binding => binding.Reference.Entry.Node is NamedTriggerSourceSyntax);

        binding.Outcome.ShouldEqual("resolved");
        binding.Target!.Entry?.Kind.ShouldEqual(targetKind);
        binding.Target.Key.StartsWith("import:", StringComparison.Ordinal).ShouldEqual(targetKind is null);
    }

    [Fact]
    void should_preserve_the_event_reference_when_renaming_the_trigger_with_a_typed_edit()
    {
        Workspace = ScreenplayWorkspace.Create("App", [Document("model", "model.play", "trigger Created\nmodule App\n  feature F\n    slice Automation S\n      event Created\n      reaction React\n        when Created")], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("App")));
        var entry = WorkspaceSyntaxIndex.Create(Workspace).Entries.Single(entry => entry.Node is TriggerSyntax);
        var trigger = (TriggerSyntax)entry.Node;
        var result = Workspace.ProposeAuthoring(new WorkspaceAuthoringRequest
        {
            ExpectedRevision = Workspace.Revision,
            ExpectedCatalogRevision = Workspace.IdentityCatalog.Revision,
            Validation = WorkspaceAuthoringValidation.Executable,
            SemanticRenames = [new(SemanticAddress.ForTrigger(Workspace.IdentityCatalog.Application, "Created"), SemanticAddress.ForTrigger(Workspace.IdentityCatalog.Application, "Signaled"))],
            Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments,
            Operations = [new ReplaceWorkspaceNode(entry.Handle, trigger, trigger with { Name = "Signaled" })]
        });

        Assert.True(result.Accepted, string.Join("; ", result.Conflicts.Select(conflict => conflict.Message)));
        WorkspaceSyntaxIndex.Create(result.Workspace!).Entries.Select(entry => entry.Node).OfType<NamedTriggerSourceSyntax>().Single().Name.ShouldEqual("Created");
    }
}

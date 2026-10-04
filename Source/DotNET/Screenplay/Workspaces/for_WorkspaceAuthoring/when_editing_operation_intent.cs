// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring;

public class when_editing_operation_intent
{
    const string Source = "system Mailer\nmodule M\n  feature F\n    slice StateChange S\n      operation Send\n        uses Mailer\n        recipient String optional\n        execute // phase\n          implementation // wrapper\n            hint \"Before\" // hint\n            file Send.cs // file\n        compensate\n          description \"Undo\"\n      command C\n        produces Send\n";

    [Fact]
    public void should_edit_phase_hints_and_preserve_comments_without_executable_admission()
    {
        var workspace = Workspace();
        var hint = WorkspaceSyntaxIndex.Create(workspace).Entries.Single(entry => entry.Node is ImplementationHintSyntax);
        var result = Propose(workspace, new ReplaceWorkspaceNode(hint.Handle, hint.Node, ((ImplementationHintSyntax)hint.Node) with { Text = "After" }));
        result.Accepted.ShouldBeTrue();
        result.ExecutableReady.ShouldBeFalse();
        result.Workspace!.Compilation.Diagnostics.Any(diagnostic => diagnostic.Code == "PLAY0268").ShouldBeTrue();
        result.Workspace.IdentityCatalog.Semantics.SequenceEqual(workspace.IdentityCatalog.Semantics).ShouldBeTrue();
        result.Workspace.IdentityCatalog.EventContracts.SequenceEqual(workspace.IdentityCatalog.EventContracts).ShouldBeTrue();
        foreach (var comment in new[] { "// phase", "// wrapper", "// hint", "// file" }) result.Workspace.Documents.Single().Text.Split(comment, StringSplitOptions.None).Length.ShouldEqual(2);
    }

    [Fact]
    public void should_support_typed_add_replace_and_remove_and_refuse_stale_handles()
    {
        var workspace = Workspace();
        var phase = WorkspaceSyntaxIndex.Create(workspace).Entries.Single(entry => entry.Node is OperationPhaseSyntax { File: not null });
        var added = Propose(workspace, new AddWorkspaceNode(phase.Handle, phase.Node, "implementation", new ImplementationSyntax([new("New guidance", phase.Location)], phase.Location)));
        added.Accepted.ShouldBeFalse(); // occupied singular member is not silently overwritten
        var replaced = Propose(workspace, new ReplaceWorkspaceNode(phase.Handle, phase.Node, ((OperationPhaseSyntax)phase.Node) with { File = new("Other.cs", phase.Location) }));
        replaced.Accepted.ShouldBeTrue();
        Propose(replaced.Workspace!, new RemoveWorkspaceNode(phase.Handle, phase.Node)).Accepted.ShouldBeFalse();
        var index = WorkspaceSyntaxIndex.Create(replaced.Workspace!);
        var wrapper = index.Entries.Single(entry => entry.Node is ImplementationSyntax);
        var removed = Propose(replaced.Workspace!, new RemoveWorkspaceNode(wrapper.Handle, wrapper.Node));
        removed.Accepted.ShouldBeTrue();
        var removedIndex = WorkspaceSyntaxIndex.Create(removed.Workspace!);
        var operation = removedIndex.Entries.Single(entry => entry.Node is OperationSyntax);
        var emptyPhase = ((OperationSyntax)operation.Node).Compensate!;
        var compensation = removedIndex.Entries.Single(entry => ReferenceEquals(entry.Node, emptyPhase));
        Propose(removed.Workspace!, new AddWorkspaceNode(compensation.Handle, compensation.Node, "implementation", new ImplementationSyntax([new("Compensate explicitly", compensation.Location)], compensation.Location))).Accepted.ShouldBeTrue();
    }

    [Fact]
    public void should_refuse_executable_validation()
    {
        var workspace = Workspace();
        var hint = WorkspaceSyntaxIndex.Create(workspace).Entries.Single(entry => entry.Node is ImplementationHintSyntax);
        var result = Propose(workspace, new ReplaceWorkspaceNode(hint.Handle, hint.Node, ((ImplementationHintSyntax)hint.Node) with { Text = "After" }), WorkspaceAuthoringValidation.Executable);
        result.Accepted.ShouldBeFalse();
        result.ExecutableDiagnostics.Any(diagnostic => diagnostic.Code == "PLAY0268").ShouldBeTrue();
    }

    static ScreenplayWorkspace Workspace() => ScreenplayWorkspace.Create("A", [WorkspaceDocument.Create("model", PortablePlayPath.Parse("model.play"), Encoding.UTF8.GetBytes(Source))], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("A")));

    static WorkspaceAuthoringResult Propose(ScreenplayWorkspace workspace, WorkspaceAstOperation operation, WorkspaceAuthoringValidation validation = WorkspaceAuthoringValidation.Authoring) => workspace.ProposeAuthoring(new()
    {
        ExpectedRevision = workspace.Revision,
        ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
        Validation = validation,
        Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments,
        Operations = [operation]
    });
}

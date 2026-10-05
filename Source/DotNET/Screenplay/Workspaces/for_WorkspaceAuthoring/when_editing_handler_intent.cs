// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring;

public class when_editing_handler_intent
{
    const string Prefix = "module M\n  feature F\n    slice StateChange S\n      command C\n        handler\n";

    [Fact]
    public void should_keep_pending_attachment_and_unwrap_identity()
    {
        var workspace = Workspace("          implementation\n            hint \"Keep the cutoff\"");
        var pending = WorkspaceImplementationInventory.Create(workspace).Entries.Single();
        pending.State.ShouldEqual("pending");
        pending.IdentityOrigin.ShouldEqual(SemanticIdentityOrigin.LegacyBootstrap);
        workspace.Compilation.Success.ShouldBeFalse();
        workspace.Compilation.ImplementationRequirements.ShouldBeEmpty();
        var index = WorkspaceSyntaxIndex.Create(workspace);
        var handler = index.Entries.Single(entry => entry.Node is HandlerSyntax);
        var attached = Propose(workspace, new ReplaceWorkspaceNode(handler.Handle, handler.Node, ((HandlerSyntax)handler.Node) with { File = new("C.cs", handler.Location) }));
        attached.Accepted.ShouldBeTrue();
        var entry = WorkspaceImplementationInventory.Create(attached.Workspace!).Entries.Single();
        entry.RequirementId.ShouldEqual(pending.RequirementId);
        entry.State.ShouldEqual("file");
        attached.Workspace!.Compilation.ImplementationRequirements.Single().RequirementId.ShouldEqual(pending.RequirementId);
        var wrapper = WorkspaceSyntaxIndex.Create(attached.Workspace).Entries.Single(value => value.Node is ImplementationSyntax);
        var unwrapped = Propose(attached.Workspace, new RemoveWorkspaceNode(wrapper.Handle, wrapper.Node));
        unwrapped.Accepted.ShouldBeTrue();
        WorkspaceImplementationInventory.Create(unwrapped.Workspace!).Entries.Single().RequirementId.ShouldEqual(pending.RequirementId);
        WorkspaceImplementationInventory.Create(unwrapped.Workspace!).Entries.Single().File.ShouldEqual("C.cs");
    }

    [Theory]
    [InlineData(WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments)]
    [InlineData(WorkspaceAuthoringFormatting.PreserveTrivia)]
    public void should_refuse_programmatic_invalid_combinations_at_admission(WorkspaceAuthoringFormatting formatting)
    {
        var workspace = Workspace("          implementation\n            hint \"Keep\"\n            file C.cs");
        var entry = WorkspaceSyntaxIndex.Create(workspace).Entries.Single(value => value.Node is HandlerSyntax);
        var handler = (HandlerSyntax)entry.Node;
        foreach (var invalid in new[]
        {
            handler with { Code = new("csharp", "return null;", entry.Location) },
            handler with { Implementation = new([new(" ", entry.Location)], entry.Location) },
            handler with { Implementation = null, File = null },
            handler with { Implementation = null, Code = new("csharp", "return null;", entry.Location) }
        })
        {
            workspace.ProposeAuthoring(new()
            {
                ExpectedRevision = workspace.Revision,
                ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
                Validation = WorkspaceAuthoringValidation.Authoring,
                Formatting = formatting,
                Operations = [new ReplaceWorkspaceNode(entry.Handle, entry.Node, invalid)]
            }).Accepted.ShouldBeFalse();
        }
    }

    [Fact]
    public void should_switch_payloads_atomically_without_changing_intent_identity()
    {
        var workspace = Workspace("          implementation\n            hint \"Keep\"\n            file C.cs");
        var before = WorkspaceImplementationInventory.Create(workspace).Entries.Single();
        var handler = WorkspaceSyntaxIndex.Create(workspace).Entries.Single(value => value.Node is HandlerSyntax);
        var result = workspace.ProposeAuthoring(new()
        {
            ExpectedRevision = workspace.Revision,
            ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
            Validation = WorkspaceAuthoringValidation.Authoring,
            Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments,
            Operations = [
                new ReplaceWorkspaceNode(handler.Handle, handler.Node, ((HandlerSyntax)handler.Node) with
                {
                    File = null,
                    Code = new("csharp", "return null;", handler.Location)
                })]
        });
        result.Accepted.ShouldBeTrue();
        var after = WorkspaceImplementationInventory.Create(result.Workspace!).Entries.Single();
        after.RequirementId.ShouldEqual(before.RequirementId);
        after.State.ShouldEqual("inline");
        after.Language.ShouldEqual("csharp");
        after.Hints.SequenceEqual(before.Hints).ShouldBeTrue();
        after.File.ShouldBeNull();
    }

    [Fact]
    public void should_refuse_removing_pending_wrapper_and_stale_edits()
    {
        var workspace = Workspace("          implementation");
        var wrapper = WorkspaceSyntaxIndex.Create(workspace).Entries.Single(value => value.Node is ImplementationSyntax);
        Propose(workspace, new RemoveWorkspaceNode(wrapper.Handle, wrapper.Node)).Accepted.ShouldBeFalse();
        var handler = WorkspaceSyntaxIndex.Create(workspace).Entries.Single(value => value.Node is HandlerSyntax);
        var attached = Propose(workspace, new ReplaceWorkspaceNode(handler.Handle, handler.Node, ((HandlerSyntax)handler.Node) with { File = new("C.cs", handler.Location) }));
        attached.Accepted.ShouldBeTrue();
        Propose(attached.Workspace!, new ReplaceWorkspaceNode(handler.Handle, handler.Node, ((HandlerSyntax)handler.Node) with { File = new("D.cs", handler.Location) })).Accepted.ShouldBeFalse();
    }

    [Fact]
    public void should_preserve_identity_through_catalog_rename_and_transport_restart()
    {
        var workspace = Workspace("          implementation");
        var provisional = WorkspaceImplementationInventory.Create(workspace).Entries.Single();
        var catalog = SemanticIdentityCatalog.Create(workspace.IdentityCatalog.Application, [], [new(provisional.Owner, provisional.OwnerId, SemanticIdentityOrigin.Persisted)], []);
        workspace = ScreenplayWorkspace.Create("A", workspace.Documents, catalog);
        var command = WorkspaceSyntaxIndex.Create(workspace).Entries.Single(entry => entry.Node is CommandSyntax);
        var before = WorkspaceImplementationInventory.Create(workspace).Entries.Single();
        var renamed = workspace.ProposeRename(new()
        {
            ExpectedRevision = workspace.Revision,
            ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
            Target = command.Handle,
            ExpectedName = "C",
            NewName = "Renamed",
            Validation = WorkspaceAuthoringValidation.Authoring
        });
        renamed.Accepted.ShouldBeTrue();
        var after = WorkspaceImplementationInventory.Create(renamed.Workspace!).Entries.Single();
        after.RequirementId.ShouldEqual(before.RequirementId);
        after.Owner.Name.ShouldEqual("Renamed");
        var restarted = ScreenplayWorkspaceSerializer.Deserialize(ScreenplayWorkspaceSerializer.Serialize(renamed.Workspace!));
        WorkspaceImplementationInventory.Create(restarted).Entries.Single().RequirementId.ShouldEqual(before.RequirementId);
    }

    [Fact]
    public void should_inventory_before_response_admission_and_not_allocate_empty_attachments()
    {
        var workspace = Workspace("          implementation\n            hint \"Pending\"\n        value String\n        returns value");
        workspace.Compilation.Success.ShouldBeFalse();
        workspace.Compilation.ImplementationRequirements.ShouldBeEmpty();
        WorkspaceImplementationInventory.Create(workspace).Entries.Single().State.ShouldEqual("pending");
    }

    [Fact]
    public void should_preserve_hint_wrapper_and_file_comments()
    {
        var workspace = Workspace("          implementation // wrapper\n            // guidance\n            hint \"Before\" // hint\n            file C.cs // file");
        var hint = WorkspaceSyntaxIndex.Create(workspace).Entries.Single(entry => entry.Node is ImplementationHintSyntax);
        var result = Propose(workspace, new ReplaceWorkspaceNode(hint.Handle, hint.Node, ((ImplementationHintSyntax)hint.Node) with { Text = "After" }));
        result.Accepted.ShouldBeTrue();
        var text = result.Workspace!.Documents.Single().Text;
        foreach (var comment in new[] { "// wrapper", "// guidance", "// hint", "// file" }) text.Split(comment, StringSplitOptions.None).Length.ShouldEqual(2);
        text.Contains("hint \"After\"", StringComparison.Ordinal).ShouldBeTrue();
    }

    static ScreenplayWorkspace Workspace(string body) => ScreenplayWorkspace.Create("A", [WorkspaceDocument.Create("model", PortablePlayPath.Parse("model.play"), Encoding.UTF8.GetBytes(Prefix + body))], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("A")));

    static WorkspaceAuthoringResult Propose(ScreenplayWorkspace workspace, WorkspaceAstOperation operation) => workspace.ProposeAuthoring(new()
    {
        ExpectedRevision = workspace.Revision,
        ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
        Validation = WorkspaceAuthoringValidation.Authoring,
        Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments,
        Operations = [operation]
    });
}

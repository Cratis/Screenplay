// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring;

public class when_editing_named_rule_intent
{
    const string Prefix = "module M\n  feature F\n    slice StateChange S\n      command C\n        label String\n        validate\n          label rule Check\n";

    [Fact]
    public void should_select_pending_by_occurrence_not_an_asserted_requirement_identity()
    {
        var workspace = Workspace(Prefix + "            implementation\n              hint \"Pending\"\n        validate\n          label rule Check\n            file A.cs\n          label rule Check\n            implementation\n              file B.cs");
        var intents = WorkspaceNamedRuleIntentInventory.Create(workspace).Entries;
        intents.Length.ShouldEqual(3);
        intents[0].RequirementId.ShouldBeNull();
        intents[0].State.ShouldEqual("pending");
        intents[0].IsProvisional.ShouldBeTrue();
        intents[1].Member.ShouldEqual("label/Check");
        intents[2].Member.ShouldEqual("label/Check#1");
        intents.Select(entry => entry.Handle).Distinct().Count().ShouldEqual(3);
        intents.Where(entry => entry.State != "pending").Select(entry => entry.RequirementId).ShouldEqual(workspace.Compilation.ImplementationRequirements.Select(requirement => requirement.RequirementId));
        workspace.Compilation.Success.ShouldBeFalse();
    }

    [Theory]
    [InlineData(WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments)]
    [InlineData(WorkspaceAuthoringFormatting.PreserveTrivia)]
    public void should_edit_guidance_without_changing_payload_identity_and_preserve_comments(WorkspaceAuthoringFormatting formatting)
    {
        var workspace = Workspace(Prefix + "            implementation // wrapper\n              // guidance\n              hint \"Before\" // hint\n              file A.cs // source");
        var hint = WorkspaceSyntaxIndex.Create(workspace).Entries.Single(entry => entry.Node is ImplementationHintSyntax);
        var before = WorkspaceNamedRuleIntentInventory.Create(workspace).Entries.Single();
        var proposed = Propose(workspace, formatting, new ReplaceWorkspaceNode(hint.Handle, hint.Node, ((ImplementationHintSyntax)hint.Node) with { Text = "After" }));
        proposed.Accepted.ShouldBeTrue();
        var after = WorkspaceNamedRuleIntentInventory.Create(proposed.Workspace!).Entries.Single();
        after.RequirementId.ShouldEqual(before.RequirementId);
        after.Hints.SequenceEqual(["After"]).ShouldBeTrue();
        after.File.ShouldEqual("A.cs");
        foreach (var comment in new[] { "// wrapper", "// guidance", "// hint", "// source" }) proposed.Workspace!.Documents.Single().Text.Split(comment, StringSplitOptions.None).Length.ShouldEqual(2);
        var wrapper = WorkspaceSyntaxIndex.Create(proposed.Workspace!).Entries.Single(entry => entry.Node is ImplementationSyntax);
        if (formatting == WorkspaceAuthoringFormatting.PreserveTrivia)
        {
            Propose(proposed.Workspace!, formatting, new RemoveWorkspaceNode(wrapper.Handle, wrapper.Node)).Accepted.ShouldBeFalse();
        }

        var unwrapped = Propose(proposed.Workspace!, WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments, new RemoveWorkspaceNode(wrapper.Handle, wrapper.Node));
        unwrapped.Accepted.ShouldBeTrue();
        WorkspaceNamedRuleIntentInventory.Create(unwrapped.Workspace!).Entries.Single().RequirementId.ShouldEqual(before.RequirementId);
        unwrapped.Workspace!.Compilation.ImplementationRequirements.Single().File.ShouldEqual("A.cs");
        Propose(proposed.Workspace!, formatting, new ReplaceWorkspaceNode(hint.Handle, hint.Node, ((ImplementationHintSyntax)hint.Node) with { Text = "Stale" })).Accepted.ShouldBeFalse();
    }

    [Fact]
    public void should_refuse_unwrapping_pending_intent_but_keep_bare_legacy_authoring()
    {
        var workspace = Workspace(Prefix + "            implementation");
        var index = WorkspaceSyntaxIndex.Create(workspace);
        var wrapper = index.Entries.Single(entry => entry.Node is ImplementationSyntax);
        Propose(workspace, WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments, new RemoveWorkspaceNode(wrapper.Handle, wrapper.Node)).Accepted.ShouldBeFalse();
        var rule = index.Entries.Single(entry => entry.Node is ValidationRuleSyntax);
        Propose(workspace, WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments, new ReplaceWorkspaceNode(rule.Handle, rule.Node, ((ValidationRuleSyntax)rule.Node) with { Implementation = null })).Accepted.ShouldBeFalse();
        WorkspaceSyntaxIndex.Create(Workspace(Prefix)).Diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public void should_preserve_persisted_owner_identity_across_rename_and_restart()
    {
        var workspace = Workspace(Prefix + "            implementation\n              hint \"Keep\"");
        var provisional = WorkspaceNamedRuleIntentInventory.Create(workspace).Entries.Single();
        var catalog = SemanticIdentityCatalog.Create(workspace.IdentityCatalog.Application, [], [new(provisional.Owner, provisional.OwnerId, SemanticIdentityOrigin.Persisted)], []);
        workspace = ScreenplayWorkspace.Create("A", workspace.Documents, catalog);
        var command = WorkspaceSyntaxIndex.Create(workspace).Entries.Single(entry => entry.Node is CommandSyntax);
        var renamed = workspace.ProposeRename(new()
        {
            ExpectedRevision = workspace.Revision,
            ExpectedCatalogRevision = catalog.Revision,
            Target = command.Handle,
            ExpectedName = "C",
            NewName = "Renamed",
            Validation = WorkspaceAuthoringValidation.Authoring
        });
        renamed.Accepted.ShouldBeTrue();
        var restarted = ScreenplayWorkspaceSerializer.Deserialize(ScreenplayWorkspaceSerializer.Serialize(renamed.Workspace!));
        var after = WorkspaceNamedRuleIntentInventory.Create(restarted).Entries.Single();
        after.OwnerId.ShouldEqual(provisional.OwnerId);
        after.RequirementId.ShouldBeNull();
        after.IsProvisional.ShouldBeFalse();
        after.Owner.Name.ShouldEqual("Renamed");
    }

    [Fact]
    public void should_resolve_physical_import_placement_without_reading_predicate_files()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("A"));
        var workspace = ScreenplayWorkspace.Create("A", [Document("root.play", "module M\n  feature F\n    import \"elsewhere/slice.play\""), Document("elsewhere/slice.play", "slice StateChange S\n  command C\n    label String\n    validate\n      label rule Check\n        implementation\n          file DoesNotExist.cs")], catalog);
        var intent = WorkspaceNamedRuleIntentInventory.Create(workspace).Entries.Single();
        intent.Owner.ShouldEqual(SemanticAddress.ForCommand(SemanticAddress.ForSlice(catalog.Application, "M", ["F"], "S"), "C"));
        var occurrence = WorkspaceSyntaxIndex.Create(workspace).Find(intent.Handle)!;
        occurrence.Location.Path.ShouldEqual("elsewhere/slice.play");
        occurrence.Location.Line.ShouldEqual(5);
        occurrence.SemanticId.ShouldBeNull();
        var conflicting = ScreenplayWorkspace.Create("A", [.. workspace.Documents, Document("conflict.play", "module Other\n  feature F\n    import \"elsewhere/slice.play\"")], catalog);
        WorkspaceNamedRuleIntentInventory.Create(conflicting).Entries.ShouldBeEmpty();
        WorkspaceNamedRuleIntentInventory.Create(conflicting).UnresolvedPlacementDocuments.ShouldNotBeEmpty();
    }

    static ScreenplayWorkspace Workspace(string source) => ScreenplayWorkspace.Create("A", [Document("model.play", source)], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("A")));
    static WorkspaceDocument Document(string path, string source) => WorkspaceDocument.Create(path.Replace('/', '-'), PortablePlayPath.Parse(path), Encoding.UTF8.GetBytes(source));
    static WorkspaceAuthoringResult Propose(ScreenplayWorkspace workspace, WorkspaceAuthoringFormatting formatting, WorkspaceAstOperation operation) => workspace.ProposeAuthoring(new()
    {
        ExpectedRevision = workspace.Revision,
        ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
        Validation = WorkspaceAuthoringValidation.Authoring,
        Formatting = formatting,
        Operations = [operation]
    });
}

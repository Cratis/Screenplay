// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

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

    [Theory]
    [InlineData("validate")]
    [InlineData("command")]
    [InlineData("root")]
    [InlineData("document")]
    public void should_refuse_pending_stripping_through_ancestor_replacements(string scope)
    {
        var workspace = Workspace(Prefix + "            implementation\n              hint \"Pending\"");
        var stripped = new ScreenplayCompiler().Parse(Prefix).Value!;
        var index = WorkspaceSyntaxIndex.Create(workspace);
        var target = index.Entries.Single(entry => scope switch
        {
            "validate" => entry.Node is DeclarativeValidateSyntax,
            "command" => entry.Node is CommandSyntax,
            _ => entry.Parent is null
        });
        var replacement = scope switch
        {
            "validate" => (SyntaxNode)stripped.Modules.Single().Features.Single().Slices.Single().Commands.Single().Validations.Single(),
            "command" => stripped.Modules.Single().Features.Single().Slices.Single().Commands.Single(),
            _ => stripped
        };
        var request = Request(workspace) with
        {
            Operations = scope == "document" ? [] : [new ReplaceWorkspaceNode(target.Handle, target.Node, replacement)],
            Documents = scope == "document" ? [new ReplaceWorkspaceSyntaxDocument(target.Handle.Document, stripped)] : []
        };
        workspace.ProposeAuthoring(request).Accepted.ShouldBeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_follow_original_source_correspondence_when_an_ancestor_changes_the_rule_header(bool document)
    {
        var workspace = Workspace(Prefix + "            implementation");
        var index = WorkspaceSyntaxIndex.Create(workspace);
        var root = index.Entries.Single(entry => entry.Parent is null);
        var replacement = new ScreenplayCompiler().Parse(Prefix.Replace("rule Check", "rule Renamed", StringComparison.Ordinal)).Value!;
        workspace.ProposeAuthoring(Request(workspace) with
        {
            Operations = document ? [] : [new ReplaceWorkspaceNode(root.Handle, root.Node, replacement)],
            Documents = document ? [new ReplaceWorkspaceSyntaxDocument(root.Handle.Document, replacement)] : []
        }).Accepted.ShouldBeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_distinguish_whole_rule_deletion_from_stripping_a_duplicate_pending_rule(bool removePending)
    {
        var workspace = Workspace(Prefix + "            implementation\n          label rule Check");
        var index = WorkspaceSyntaxIndex.Create(workspace);
        var block = index.Entries.Single(entry => entry.Node is DeclarativeValidateSyntax);
        var rules = ((DeclarativeValidateSyntax)block.Node).Rules.ToArray();
        var replacement = (DeclarativeValidateSyntax)block.Node with { Rules = [removePending ? rules[1] : rules[0] with { Implementation = null }] };
        Propose(workspace, WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments, new ReplaceWorkspaceNode(block.Handle, block.Node, replacement)).Accepted.ShouldEqual(removePending);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_validate_atomic_attachment_and_unwrap_against_final_source(bool inline)
    {
        var workspace = Workspace(Prefix + "            implementation // wrapper\n              hint \"Pending\" // guidance\n          label rule Other\n            file Other.cs // other source");
        var index = WorkspaceSyntaxIndex.Create(workspace);
        var pending = index.Entries.Single(entry => entry.Node is ValidationRuleSyntax { Implementation: not null });
        var wrapper = index.Entries.Single(entry => entry.Node is ImplementationSyntax);
        var before = WorkspaceNamedRuleIntentInventory.Create(workspace).Entries.Single(entry => entry.Member == "label/Other");
        var source = inline ? (SyntaxNode)new CodeBlockSyntax("csharp", "return true;", pending.Location) : new FileReferenceSyntax("A.cs", pending.Location);
        var result = workspace.ProposeAuthoring(Request(workspace) with
        {
            Operations = [new RemoveWorkspaceNode(wrapper.Handle, wrapper.Node), new AddWorkspaceNode(pending.Handle, pending.Node, inline ? "code" : "file", source)]
        });
        result.Accepted.ShouldBeTrue();
        var intents = WorkspaceNamedRuleIntentInventory.Create(result.Workspace!).Entries;
        intents.Single(entry => entry.Member == "label/Check").State.ShouldEqual(inline ? "inline" : "file");
        intents.Single(entry => entry.Member == "label/Other").RequirementId.ShouldEqual(before.RequirementId);
        foreach (var comment in new[] { "// wrapper", "// guidance", "// other source" }) result.Workspace!.Documents.Single().Text.Split(comment, StringSplitOptions.None).Length.ShouldEqual(2);
        workspace.ProposeAuthoring(Request(workspace) with { Operations = [new RemoveWorkspaceNode(pending.Handle, pending.Node)] }).Accepted.ShouldBeTrue();
        result.Workspace!.ProposeAuthoring(Request(result.Workspace!) with { Operations = [new RemoveWorkspaceNode(wrapper.Handle, wrapper.Node)] }).Accepted.ShouldBeFalse();
    }

    [Fact]
    public void should_allow_an_atomic_source_switch_but_refuse_invalid_final_sources()
    {
        var workspace = Workspace(Prefix + "            implementation\n              file A.cs");
        var index = WorkspaceSyntaxIndex.Create(workspace);
        var rule = index.Entries.Single(entry => entry.Node is ValidationRuleSyntax);
        var wrapper = index.Entries.Single(entry => entry.Node is ImplementationSyntax);
        var file = index.Entries.Single(entry => entry.Node is FileReferenceSyntax);
        var switched = workspace.ProposeAuthoring(Request(workspace) with
        {
            Operations = [new RemoveWorkspaceNode(wrapper.Handle, wrapper.Node), new RemoveWorkspaceNode(file.Handle, file.Node), new AddWorkspaceNode(rule.Handle, rule.Node, "code", new CodeBlockSyntax("csharp", "return true;", rule.Location))]
        });
        switched.Accepted.ShouldBeTrue();
        var pending = Workspace(Prefix + "            implementation");
        var pendingRule = WorkspaceSyntaxIndex.Create(pending).Entries.Single(entry => entry.Node is ValidationRuleSyntax);
        foreach (var invalid in new[]
        {
            ((ValidationRuleSyntax)pendingRule.Node) with { Implementation = null, File = new("", pendingRule.Location) },
            ((ValidationRuleSyntax)pendingRule.Node) with { Implementation = null, Code = new("unknown", "return true;", pendingRule.Location) },
            ((ValidationRuleSyntax)pendingRule.Node) with { Rule = ValidationRuleKind.NotEmpty }
        })
        {
            Propose(pending, WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments, new ReplaceWorkspaceNode(pendingRule.Handle, pendingRule.Node, invalid)).Accepted.ShouldBeFalse();
        }
    }

    [Fact]
    public void should_not_weaken_reference_guards_when_deleting_the_whole_owner()
    {
        var workspace = Workspace(Prefix + "            implementation\n      specification Reached\n        when C\n          label = \"ok\"\n        then error \"Invalid\"");
        var index = WorkspaceSyntaxIndex.Create(workspace);
        var command = index.Entries.Single(entry => entry.Node is CommandSyntax);
        Propose(workspace, WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments, new RemoveWorkspaceNode(command.Handle, command.Node)).Accepted.ShouldBeFalse();
        var specification = index.Entries.Single(entry => entry.Node is SpecificationSyntax);
        workspace.ProposeAuthoring(Request(workspace) with { Operations = [new RemoveWorkspaceNode(command.Handle, command.Node), new RemoveWorkspaceNode(specification.Handle, specification.Node)] }).Accepted.ShouldBeTrue();
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
    static WorkspaceAuthoringRequest Request(ScreenplayWorkspace workspace) => new()
    {
        ExpectedRevision = workspace.Revision,
        ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
        Validation = WorkspaceAuthoringValidation.Authoring,
        Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments
    };
    static WorkspaceAuthoringResult Propose(ScreenplayWorkspace workspace, WorkspaceAuthoringFormatting formatting, WorkspaceAstOperation operation) => workspace.ProposeAuthoring(Request(workspace) with { Formatting = formatting, Operations = [operation] });
}

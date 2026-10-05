// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;
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

    [Theory]
    [InlineData("validate", false)]
    [InlineData("command", false)]
    [InlineData("root", false)]
    [InlineData("document", false)]
    [InlineData("validate", true)]
    [InlineData("command", true)]
    [InlineData("root", true)]
    [InlineData("document", true)]
    public void should_refuse_decoded_renamed_bare_rules_when_sibling_cardinality_changes(string scope, bool deleteSibling)
    {
        var workspace = Workspace(Prefix + "            implementation\n              hint \"Keep\"" + (deleteSibling ? "\n          label not empty" : ""));
        var replacement = new ScreenplayCompiler().Parse(Prefix.Replace("rule Check", "rule Renamed", StringComparison.Ordinal) + (deleteSibling ? "" : "          label not empty")).Value!;
        var index = WorkspaceSyntaxIndex.Create(workspace);
        var target = index.Entries.Single(entry => scope switch
        {
            "validate" => entry.Node is DeclarativeValidateSyntax,
            "command" => entry.Node is CommandSyntax,
            _ => entry.Parent is null
        });
        SyntaxNode node = scope switch
        {
            "validate" => replacement.Modules.Single().Features.Single().Slices.Single().Commands.Single().Validations.Single(),
            "command" => replacement.Modules.Single().Features.Single().Slices.Single().Commands.Single(),
            _ => replacement
        };
        node = SyntaxJson.Deserialize(SyntaxJson.Serialize(node));
        workspace.ProposeAuthoring(Request(workspace) with
        {
            Operations = scope == "document" ? [] : [new ReplaceWorkspaceNode(target.Handle, target.Node, node)],
            Documents = scope == "document" ? [new ReplaceWorkspaceSyntaxDocument(target.Handle.Document, (ApplicationSyntax)node)] : []
        }).Accepted.ShouldBeFalse();
    }

    [Theory]
    [InlineData("\n")]
    [InlineData("\r\n")]
    public void should_allow_same_path_document_deletion_without_borrowing_the_old_pending_coordinates(string newline)
    {
        const string source = Prefix + "            implementation // wrapper\n              hint \"Keep\" // guidance\n          label not empty // surviving rule";
        var workspace = Workspace(source.Replace("\n", newline, StringComparison.Ordinal));
        var replacement = new ScreenplayCompiler().Parse(Prefix.Replace("label rule Check\n", "label not empty // surviving rule\n", StringComparison.Ordinal).Replace("\n", newline, StringComparison.Ordinal), "model.play").Value!;
        var originalRule = WorkspaceSyntaxIndex.Create(workspace).Entries.First(entry => entry.Node is ValidationRuleSyntax);
        ((DeclarativeValidateSyntax)replacement.Modules.Single().Features.Single().Slices.Single().Commands.Single().Validations.Single()).Rules.Single().Location.ShouldEqual(originalRule.Location);
        var result = workspace.ProposeAuthoring(Request(workspace) with { Documents = [new ReplaceWorkspaceSyntaxDocument(workspace.Documents.Single().Id, replacement)] });
        result.Accepted.ShouldBeTrue();
        var text = result.Workspace!.Documents.Single().Text;
        text.Split("// surviving rule", StringSplitOptions.None).Length.ShouldEqual(2);
        text.Contains("// wrapper", StringComparison.Ordinal).ShouldBeFalse();
        text.Contains("// guidance", StringComparison.Ordinal).ShouldBeFalse();
        WorkspaceSyntaxIndex.Create(result.Workspace!).Entries.Single(entry => entry.Node is ValidationRuleSyntax).Location.Path.ShouldEqual("model.play");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_allow_validated_rule_removal_before_an_ancestor_replacement(bool document)
    {
        var workspace = Workspace(Prefix + "            implementation\n              hint \"Keep\"");
        var index = WorkspaceSyntaxIndex.Create(workspace);
        var rule = index.Entries.Single(entry => entry.Node is ValidationRuleSyntax);
        var root = index.Entries.Single(entry => entry.Parent is null);
        var replacement = (ApplicationSyntax)SyntaxJson.Deserialize(SyntaxJson.Serialize(new ScreenplayCompiler().Parse(Prefix.Replace("rule Check", "rule Renamed", StringComparison.Ordinal) + "          label not empty").Value!));
        var result = workspace.ProposeAuthoring(Request(workspace) with
        {
            Operations = document ? [new RemoveWorkspaceNode(rule.Handle, rule.Node)] : [new RemoveWorkspaceNode(rule.Handle, rule.Node), new ReplaceWorkspaceNode(root.Handle, root.Node, replacement)],
            Documents = document ? [new ReplaceWorkspaceSyntaxDocument(root.Handle.Document, replacement)] : []
        });
        result.Accepted.ShouldBeTrue();
        var stale = rule.Handle with { Path = rule.Handle.Path + "/missing" };
        workspace.ProposeAuthoring(Request(workspace) with { Operations = [new RemoveWorkspaceNode(stale, rule.Node)], Documents = [new ReplaceWorkspaceSyntaxDocument(root.Handle.Document, replacement)] }).Accepted.ShouldBeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_preserve_all_guidance_on_a_renamed_rule_in_a_moved_validation_block(bool renameOwner)
    {
        var workspace = Workspace(Prefix + "            implementation\n              hint \"Keep\"\n              hint \"Everything\"\n        validate\n          label not empty");
        var source = Prefix.Replace("rule Check", "rule Renamed", StringComparison.Ordinal).Replace("        validate\n", "        validate\n          label not empty\n        validate\n", StringComparison.Ordinal) + "            implementation\n              hint \"Keep\"\n              hint \"Everything\"";
        ImmutableArray<SemanticIdentityRename> renames = [];
        if (renameOwner)
        {
            var owner = WorkspaceSyntaxIndex.Create(workspace).Entries.Single(entry => entry.Node is CommandSyntax).Address!;
            var property = SemanticAddress.ForProperty(owner, "label");
            var renamedOwner = SemanticAddress.ForCommand(SemanticAddress.ForSlice(workspace.IdentityCatalog.Application, "M", ["F"], "S"), "Renamed");
            var catalog = SemanticIdentityCatalog.Create(workspace.IdentityCatalog.Application, [],
                [new(owner, SemanticId.Create(owner), SemanticIdentityOrigin.Persisted), new(property, SemanticId.Create(property), SemanticIdentityOrigin.Persisted)], []);
            workspace = ScreenplayWorkspace.Create("A", workspace.Documents, catalog);
            renames = [new(owner, renamedOwner), new(property, SemanticAddress.ForProperty(renamedOwner, "label"))];
            source = source.Replace("command C", "command Renamed", StringComparison.Ordinal) + "\n          label rule New";
        }

        var root = WorkspaceSyntaxIndex.Create(workspace).Entries.Single(entry => entry.Parent is null);
        var replacement = SyntaxJson.Deserialize(SyntaxJson.Serialize(new ScreenplayCompiler().Parse(source).Value!));
        var result = workspace.ProposeAuthoring(Request(workspace) with { Operations = [new ReplaceWorkspaceNode(root.Handle, root.Node, replacement)], SemanticRenames = renames });
        Assert.True(result.Accepted, string.Join("; ", result.Conflicts.Select(conflict => conflict.Message)));
        WorkspaceNamedRuleIntentInventory.Create(result.Workspace!).Entries.Single(entry => entry.State == "pending").Hints.SequenceEqual(["Keep", "Everything"]).ShouldBeTrue();
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void should_require_original_removal_provenance_for_ambiguous_decoded_duplicates(bool pendingSurvivor, bool editHints)
    {
        var workspace = Workspace(Prefix + "            implementation\n              hint \"Keep\"\n          label rule Check" + (pendingSurvivor ? "\n            implementation\n              hint \"Keep\"" : ""));
        var index = WorkspaceSyntaxIndex.Create(workspace);
        var root = index.Entries.Single(entry => entry.Parent is null);
        var first = index.Entries.First(entry => entry.Node is ValidationRuleSyntax);
        var source = Prefix + (pendingSurvivor ? $"            implementation\n              hint \"{(editHints ? "Edited" : "Keep")}\"" : "");
        var replacement = SyntaxJson.Deserialize(SyntaxJson.Serialize(new ScreenplayCompiler().Parse(source).Value!));
        Propose(workspace, WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments, new ReplaceWorkspaceNode(root.Handle, root.Node, replacement)).Accepted.ShouldBeFalse();
        workspace.ProposeAuthoring(Request(workspace) with { Operations = [new RemoveWorkspaceNode(first.Handle, first.Node), new ReplaceWorkspaceNode(root.Handle, root.Node, replacement)] }).Accepted.ShouldBeTrue();
    }

    [Fact]
    public void should_not_borrow_a_matching_rule_from_another_owner()
    {
        var workspace = Workspace(Prefix + "            implementation\n              hint \"Keep\"\n      command Other\n        label String\n        validate\n          label rule Check");
        var root = WorkspaceSyntaxIndex.Create(workspace).Entries.Single(entry => entry.Parent is null);
        var replacement = SyntaxJson.Deserialize(SyntaxJson.Serialize(new ScreenplayCompiler().Parse(Prefix).Value!));
        Propose(workspace, WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments, new ReplaceWorkspaceNode(root.Handle, root.Node, replacement)).Accepted.ShouldBeFalse();
    }

    [Fact]
    public void should_not_choose_one_survivor_when_the_same_original_child_is_reused_twice()
    {
        var workspace = Workspace(Prefix + "            implementation\n              hint \"Keep\"");
        var index = WorkspaceSyntaxIndex.Create(workspace);
        var block = index.Entries.Single(entry => entry.Node is DeclarativeValidateSyntax);
        var rule = ((DeclarativeValidateSyntax)block.Node).Rules.Single();
        var replacement = (DeclarativeValidateSyntax)block.Node with { Rules = [rule with { Implementation = null }, rule] };
        Propose(workspace, WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments, new ReplaceWorkspaceNode(block.Handle, block.Node, replacement)).Accepted.ShouldBeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_accept_decoded_final_attachment_and_unwrap_with_a_new_sibling(bool document)
    {
        var workspace = Workspace(Prefix + "            implementation\n              hint \"Keep\"");
        var root = WorkspaceSyntaxIndex.Create(workspace).Entries.Single(entry => entry.Parent is null);
        var replacement = (ApplicationSyntax)SyntaxJson.Deserialize(SyntaxJson.Serialize(new ScreenplayCompiler().Parse(Prefix + "            file A.cs\n          label not empty").Value!));
        workspace.ProposeAuthoring(Request(workspace) with
        {
            Operations = document ? [] : [new ReplaceWorkspaceNode(root.Handle, root.Node, replacement)],
            Documents = document ? [new ReplaceWorkspaceSyntaxDocument(root.Handle.Document, replacement)] : []
        }).Accepted.ShouldBeTrue();
    }

    [Theory]
    [InlineData("copied", false, false)]
    [InlineData("copied", true, false)]
    [InlineData("copied", false, true)]
    [InlineData("copied", true, true)]
    [InlineData("copied-member", false, false)]
    [InlineData("copied-member", true, false)]
    [InlineData("copied-member", false, true)]
    [InlineData("copied-member", true, true)]
    [InlineData("duplicates", false, false)]
    [InlineData("duplicates", true, false)]
    [InlineData("duplicates", false, true)]
    [InlineData("duplicates", true, true)]
    [InlineData("reordered", false, false)]
    [InlineData("reordered", true, false)]
    [InlineData("reordered", false, true)]
    [InlineData("reordered", true, true)]
    public void should_conserve_pending_obligations_despite_copied_or_edited_guidance(string change, bool document, bool samePath)
    {
        var workspace = Workspace(Prefix + "            implementation\n              hint \"Keep\"" + (change.StartsWith("copied", StringComparison.Ordinal) ? "" : "\n          label rule Check\n            implementation\n              hint \"Keep\""));
        var root = WorkspaceSyntaxIndex.Create(workspace).Entries.Single(entry => entry.Parent is null);
        var source = change.StartsWith("copied", StringComparison.Ordinal) ? Prefix + $"          label rule {(change == "copied" ? "Other" : "Check")}\n            implementation\n              hint \"Keep\"" :
            Prefix.Replace("rule Check", "rule Renamed", StringComparison.Ordinal) + "            implementation\n              hint \"Edited\"";
        if (change == "reordered") source += "\n          label not empty";
        var parsed = new ScreenplayCompiler().Parse(source, samePath ? "model.play" : null).Value!;
        var replacement = samePath ? parsed : (ApplicationSyntax)SyntaxJson.Deserialize(SyntaxJson.Serialize(parsed));
        var result = workspace.ProposeAuthoring(Request(workspace) with
        {
            Operations = document ? [] : [new ReplaceWorkspaceNode(root.Handle, root.Node, replacement)],
            Documents = document ? [new ReplaceWorkspaceSyntaxDocument(root.Handle.Document, replacement)] : []
        });
        Assert.False(result.Accepted, $"{change}: {string.Join("; ", result.Conflicts.Select(conflict => conflict.Message))}");
        result.Conflicts.Single().Message.Contains("pending", StringComparison.OrdinalIgnoreCase).ShouldBeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_conserve_equal_pending_occurrences_without_allocating_ids_when_both_are_edited(bool document)
    {
        var workspace = Workspace(Prefix + "            implementation\n              hint \"Keep\"\n          label rule Check\n            implementation\n              hint \"Keep\"\n        validate\n          label rule Attached\n            file A.cs");
        var root = WorkspaceSyntaxIndex.Create(workspace).Entries.Single(entry => entry.Parent is null);
        var source = Prefix.Replace("rule Check", "rule Second", StringComparison.Ordinal) + "            implementation\n              hint \"Second edited\"\n          label rule First\n            implementation\n              hint \"First edited\"\n        validate\n          label rule Attached\n            file A.cs";
        var replacement = (ApplicationSyntax)SyntaxJson.Deserialize(SyntaxJson.Serialize(new ScreenplayCompiler().Parse(source).Value!));
        var before = workspace.Compilation.ImplementationRequirements.Single();
        var result = workspace.ProposeAuthoring(Request(workspace) with
        {
            Operations = document ? [] : [new ReplaceWorkspaceNode(root.Handle, root.Node, replacement)],
            Documents = document ? [new ReplaceWorkspaceSyntaxDocument(root.Handle.Document, replacement)] : []
        });
        Assert.True(result.Accepted, string.Join("; ", result.Conflicts.Select(conflict => conflict.Message)));
        WorkspaceNamedRuleIntentInventory.Create(result.Workspace!).Entries.Where(entry => entry.State == "pending").All(entry => entry.RequirementId is null).ShouldBeTrue();
        var after = result.Workspace!.Compilation.ImplementationRequirements.Single();
        after.RequirementId.ShouldEqual(before.RequirementId);
        after.ContentHash.ShouldEqual(before.ContentHash);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_require_the_correct_original_removal_when_equal_pending_rules_are_renamed_and_hint_edited(bool document)
    {
        var workspace = Workspace(Prefix + "            implementation\n              hint \"Keep\"\n          label rule Check\n            implementation\n              hint \"Keep\"\n      command Other\n        label String\n        validate\n          label rule Check\n            implementation\n              hint \"Keep\"");
        var index = WorkspaceSyntaxIndex.Create(workspace);
        var root = index.Entries.Single(entry => entry.Parent is null);
        var rules = index.Entries.Where(entry => entry.Node is ValidationRuleSyntax).ToArray();
        var source = Prefix.Replace("rule Check", "rule Renamed", StringComparison.Ordinal) + "            implementation\n              hint \"Edited\"\n      command Other\n        label String\n        validate\n          label rule Check\n            implementation\n              hint \"Keep\"";
        var replacement = (ApplicationSyntax)SyntaxJson.Deserialize(SyntaxJson.Serialize(new ScreenplayCompiler().Parse(source).Value!));
        WorkspaceAuthoringResult Remove(WorkspaceNodeHandle handle, SyntaxNode expected) => workspace.ProposeAuthoring(Request(workspace) with
        {
            Operations = document ? [new RemoveWorkspaceNode(handle, expected)] : [new RemoveWorkspaceNode(handle, expected), new ReplaceWorkspaceNode(root.Handle, root.Node, replacement)],
            Documents = document ? [new ReplaceWorkspaceSyntaxDocument(root.Handle.Document, replacement)] : []
        });
        Remove(rules[0].Handle, rules[0].Node).Accepted.ShouldBeTrue();
        Remove(rules[2].Handle, rules[2].Node).Accepted.ShouldBeFalse();
        Remove(rules[0].Handle with { Revision = WorkspaceRevision.Parse("wsrev1:" + new string('0', 64)) }, rules[0].Node).Accepted.ShouldBeFalse();
        Remove(rules[0].Handle, ((ValidationRuleSyntax)rules[0].Node) with { Property = "forged" }).Accepted.ShouldBeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_prove_full_pending_deletion_from_structural_surviving_members(bool document)
    {
        var workspace = Workspace(Prefix + "            implementation\n              hint \"Keep\"\n          label rule Check\n            implementation\n              hint \"Keep\"\n          label not empty");
        var root = WorkspaceSyntaxIndex.Create(workspace).Entries.Single(entry => entry.Parent is null);
        var source = Prefix.Replace("label rule Check", "label not empty", StringComparison.Ordinal);
        var replacement = (ApplicationSyntax)SyntaxJson.Deserialize(SyntaxJson.Serialize(new ScreenplayCompiler().Parse(source, "model.play").Value!));
        workspace.ProposeAuthoring(Request(workspace) with
        {
            Operations = document ? [] : [new ReplaceWorkspaceNode(root.Handle, root.Node, replacement)],
            Documents = document ? [new ReplaceWorkspaceSyntaxDocument(root.Handle.Document, replacement)] : []
        }).Accepted.ShouldBeTrue();
    }

    [Theory]
    [InlineData("command")]
    [InlineData("root")]
    [InlineData("document")]
    public void should_refuse_split_block_guidance_copies_across_the_whole_owner(string scope)
    {
        foreach (var (reverse, duplicate, metadata) in
            from reverse in new[] { false, true }
            from duplicate in new[] { false, true }
            from metadata in new[] { "decoded", "same-path", "changed-owner" }
            select (reverse, duplicate, metadata))
        {
            var workspace = Workspace(Prefix + "            implementation\n              hint \"Keep\"");
            var index = WorkspaceSyntaxIndex.Create(workspace);
            var pending = index.Entries.Single(entry => entry.Node is ValidationRuleSyntax);
            var target = index.Entries.Single(entry => scope == "command" ? entry.Node is CommandSyntax : entry.Parent is null);
            const string bare = "        validate\n          label rule Check\n";
            const string copy = bare + "            implementation\n              hint \"Keep\"\n";
            var source = Prefix[..Prefix.IndexOf("        validate", StringComparison.Ordinal)] +
                (reverse ? copy + bare : bare + copy) + (duplicate ? copy : "");
            if (metadata == "changed-owner") source = source.Replace("command C", "command Renamed", StringComparison.Ordinal);
            var parsed = new ScreenplayCompiler().Parse(source, metadata == "same-path" ? "model.play" : "different.play").Value!;
            SyntaxNode replacement = scope == "command" ? parsed.Modules.Single().Features.Single().Slices.Single().Commands.Single() : parsed;
            if (metadata == "decoded") replacement = SyntaxJson.Deserialize(SyntaxJson.Serialize(replacement));
            var request = Request(workspace) with
            {
                Operations = scope == "document" ? [] : [new ReplaceWorkspaceNode(target.Handle, target.Node, replacement)],
                Documents = scope == "document" ? [new ReplaceWorkspaceSyntaxDocument(target.Handle.Document, (ApplicationSyntax)replacement)] : []
            };
            var refused = workspace.ProposeAuthoring(request);
            Assert.False(refused.Accepted, $"{scope}/{reverse}/{duplicate}/{metadata}");
            if (metadata != "changed-owner")
            {
                refused.Conflicts.Single().Message.Contains("pending", StringComparison.OrdinalIgnoreCase).ShouldBeTrue();
                var removed = workspace.ProposeAuthoring(request with
                {
                    Operations = scope == "document" ? [new RemoveWorkspaceNode(pending.Handle, pending.Node)] :
                        [new RemoveWorkspaceNode(pending.Handle, pending.Node), new ReplaceWorkspaceNode(target.Handle, target.Node, replacement)]
                });
                Assert.True(removed.Accepted, string.Join("; ", removed.Conflicts.Select(conflict => conflict.Message)));
            }
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_distinguish_validated_ancestor_lineage_from_document_split_block_inference(bool document)
    {
        var workspace = Workspace(Prefix + "            implementation\n              hint \"Keep\"");
        var index = WorkspaceSyntaxIndex.Create(workspace);
        var root = index.Entries.Single(entry => entry.Parent is null);
        var application = (ApplicationSyntax)root.Node;
        var module = application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        var command = slice.Commands.Single();
        var block = (DeclarativeValidateSyntax)command.Validations.Single();
        var original = block.Rules.Single();
        var decoded = (DeclarativeValidateSyntax)SyntaxJson.Deserialize(SyntaxJson.Serialize(block));
        foreach (var keepOriginal in new[] { false, true })
        {
            // A with-copy retains the actual value child; a separately decoded copy does not.
            var bare = original with { Implementation = null };
            DeclarativeValidateSyntax[] validations = keepOriginal ? [block, decoded with { Rules = [decoded.Rules.Single() with { Implementation = null, Location = original.Location }] }] :
                [block with { Rules = [bare] }, decoded];
            var replacement = application with { Modules = [module with { Features = [feature with { Slices = [slice with { Commands = [command with { Validations = validations }] }] }] }] };
            var result = workspace.ProposeAuthoring(Request(workspace) with
            {
                Operations = document ? [] : [new ReplaceWorkspaceNode(root.Handle, root.Node, replacement)],
                Documents = document ? [new ReplaceWorkspaceSyntaxDocument(root.Handle.Document, replacement)] : []
            });

            // Only the ancestor operation validates the caller's original subtree expectation.
            // A document replacement supplies no expectation: matching metadata is not lineage.
            var accepted = keepOriginal && !document;
            Assert.True(result.Accepted == accepted, string.Join("; ", result.Conflicts.Select(conflict => conflict.Message)));
            if (accepted) WorkspaceNamedRuleIntentInventory.Create(result.Workspace!).Entries.Single(entry => entry.State == "pending").RequirementId.ShouldBeNull();
        }
    }

    [Fact]
    public void should_include_new_sibling_blocks_outside_the_replaced_validation_region()
    {
        var workspace = Workspace(Prefix + "            implementation\n              hint \"Keep\"");
        var index = WorkspaceSyntaxIndex.Create(workspace);
        var command = index.Entries.Single(entry => entry.Node is CommandSyntax);
        var block = index.Entries.Single(entry => entry.Node is DeclarativeValidateSyntax);
        var rule = index.Entries.Single(entry => entry.Node is ValidationRuleSyntax);
        var decoded = (DeclarativeValidateSyntax)SyntaxJson.Deserialize(SyntaxJson.Serialize(block.Node));
        var bare = decoded with { Rules = [decoded.Rules.Single() with { Implementation = null }] };
        foreach (var bareInside in new[] { false, true })
        {
            var replacement = new ReplaceWorkspaceNode(block.Handle, block.Node, bareInside ? bare : decoded with { Rules = [] });
            var added = new AddWorkspaceNode(command.Handle, command.Node, "validations", bareInside ? decoded : decoded with { Rules = [bare.Rules.Single(), decoded.Rules.Single()] });
            workspace.ProposeAuthoring(Request(workspace) with { Operations = [replacement, added] }).Accepted.ShouldBeFalse();
            workspace.ProposeAuthoring(Request(workspace) with { Operations = [new RemoveWorkspaceNode(rule.Handle, rule.Node), replacement, added] }).Accepted.ShouldBeTrue();
        }

        var emptied = new ReplaceWorkspaceNode(block.Handle, block.Node, decoded with { Rules = [] });
        workspace.ProposeAuthoring(Request(workspace) with { Operations = [emptied] }).Accepted.ShouldBeTrue();
        workspace.ProposeAuthoring(Request(workspace) with { Operations = [emptied, new AddWorkspaceNode(command.Handle, command.Node, "validations", decoded)] }).Accepted.ShouldBeTrue();
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void should_conserve_pending_occurrences_across_transaction_regions(bool reverse, bool samePath)
    {
        var workspace = Workspace(Prefix + "            implementation\n              hint \"Keep\"\n        validate\n          label rule Check\n            implementation\n              hint \"Keep\"");
        var index = WorkspaceSyntaxIndex.Create(workspace);
        var blocks = index.Entries.Where(entry => entry.Node is DeclarativeValidateSyntax).ToArray();
        var rules = index.Entries.Where(entry => entry.Node is ValidationRuleSyntax).ToArray();
        var parsed = new ScreenplayCompiler().Parse(Prefix.Replace("rule Check", "rule Renamed", StringComparison.Ordinal) + "            implementation\n              hint \"Edited\"", samePath ? "model.play" : null).Value!;
        var block = (DeclarativeValidateSyntax)parsed.Modules.Single().Features.Single().Slices.Single().Commands.Single().Validations.Single();
        if (!samePath) block = (DeclarativeValidateSyntax)SyntaxJson.Deserialize(SyntaxJson.Serialize(block));
        var empty = new ReplaceWorkspaceNode(blocks[0].Handle, blocks[0].Node, block with { Rules = [] });
        var renamed = new ReplaceWorkspaceNode(blocks[1].Handle, blocks[1].Node, block);
        WorkspaceAstOperation[] operations = reverse ? [renamed, empty] : [empty, renamed];
        var refused = workspace.ProposeAuthoring(Request(workspace) with { Operations = [.. operations] });
        Assert.False(refused.Accepted, string.Join("; ", refused.Conflicts.Select(conflict => conflict.Message)));
        refused.Conflicts.Single().Message.Contains("pending", StringComparison.OrdinalIgnoreCase).ShouldBeTrue();

        // A validated original removal discharges exactly that occurrence, not the sibling obligation.
        foreach (var rule in rules)
        {
            var allowed = workspace.ProposeAuthoring(Request(workspace) with { Operations = [new RemoveWorkspaceNode(rule.Handle, rule.Node), .. operations] });
            Assert.True(allowed.Accepted, string.Join("; ", allowed.Conflicts.Select(conflict => conflict.Message)));
            var intent = WorkspaceNamedRuleIntentInventory.Create(allowed.Workspace!).Entries.Single();
            intent.RequirementId.ShouldBeNull();
            intent.Hints.SequenceEqual(["Edited"]).ShouldBeTrue();
        }

        workspace.ProposeAuthoring(Request(workspace) with { Operations = [new RemoveWorkspaceNode(rules[0].Handle, rules[0].Node), new RemoveWorkspaceNode(rules[0].Handle, rules[0].Node), .. operations] }).Accepted.ShouldBeFalse();
        workspace.ProposeAuthoring(Request(workspace) with { Operations = [.. operations, new RemoveWorkspaceNode(rules[0].Handle, rules[0].Node)] }).Accepted.ShouldBeFalse();
        workspace.ProposeAuthoring(Request(workspace) with { Operations = [empty, new ReplaceWorkspaceNode(rules[0].Handle, rules[0].Node, rules[0].Node)] }).Accepted.ShouldBeFalse();
        workspace.ProposeAuthoring(Request(workspace) with { Operations = [empty, new ReplaceWorkspaceNode(blocks[1].Handle, blocks[1].Node, block with { Rules = [] })] }).Accepted.ShouldBeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_union_transaction_regions_with_document_replacement_by_exact_owner(bool reverse)
    {
        var workspace = Workspace(Prefix + "            implementation\n              hint \"Keep\"\n        validate\n          label rule Check\n            implementation\n              hint \"Keep\"\n      command Other\n        label String\n        validate\n          label rule Check\n            implementation\n              hint \"Keep\"");
        var index = WorkspaceSyntaxIndex.Create(workspace);
        var root = index.Entries.Single(entry => entry.Parent is null);
        var blocks = index.Entries.Where(entry => entry.Node is DeclarativeValidateSyntax).ToArray();
        var rules = index.Entries.Where(entry => entry.Node is ValidationRuleSyntax).ToArray();
        var parsed = new ScreenplayCompiler().Parse(Prefix.Replace("rule Check", "rule Renamed", StringComparison.Ordinal) + "            implementation\n              hint \"Edited\"\n      command Other\n        label String\n        validate\n          label rule Check\n            implementation\n              hint \"Keep\"").Value!;
        var replacement = (ApplicationSyntax)SyntaxJson.Deserialize(SyntaxJson.Serialize(parsed));
        var block = replacement.Modules.Single().Features.Single().Slices.Single().Commands.First().Validations.Single();
        WorkspaceAstOperation[] operations = [new ReplaceWorkspaceNode(blocks[0].Handle, blocks[0].Node, ((DeclarativeValidateSyntax)block) with { Rules = [] }), new ReplaceWorkspaceNode(blocks[1].Handle, blocks[1].Node, block)];
        if (reverse) Array.Reverse(operations);
        workspace.ProposeAuthoring(Request(workspace) with { Operations = [.. operations] }).Accepted.ShouldBeFalse();
        workspace.ProposeAuthoring(Request(workspace) with { Operations = [new RemoveWorkspaceNode(rules[2].Handle, rules[2].Node), .. operations] }).Accepted.ShouldBeFalse();

        // Document replacement and original removals overlap only under the existing validated exception.
        // The document's rule union must still retain the two independent C obligations.
        workspace.ProposeAuthoring(Request(workspace) with { Operations = [new RemoveWorkspaceNode(rules[2].Handle, rules[2].Node)], Documents = [new ReplaceWorkspaceSyntaxDocument(root.Handle.Document, replacement)] }).Accepted.ShouldBeFalse();
        workspace.ProposeAuthoring(Request(workspace) with { Operations = [new RemoveWorkspaceNode(rules[0].Handle, rules[0].Node)], Documents = [new ReplaceWorkspaceSyntaxDocument(root.Handle.Document, replacement)] }).Accepted.ShouldBeTrue();
        workspace.ProposeAuthoring(Request(workspace) with { Operations = [.. operations], Documents = [new ReplaceWorkspaceSyntaxDocument(root.Handle.Document, replacement)] }).Accepted.ShouldBeFalse();
    }

    [Fact]
    public void should_claim_structural_survivors_once_across_identical_transaction_regions()
    {
        var workspace = Workspace(Prefix + "            implementation\n              hint \"Keep\"\n        validate\n          label rule Check\n            implementation\n              hint \"Keep\"\n        validate\n          label rule Check\n            implementation\n              hint \"Keep\"");
        var index = WorkspaceSyntaxIndex.Create(workspace);
        var blocks = index.Entries.Where(entry => entry.Node is DeclarativeValidateSyntax).ToArray();
        var rule = index.Entries.First(entry => entry.Node is ValidationRuleSyntax);
        var decoded = (DeclarativeValidateSyntax)SyntaxJson.Deserialize(SyntaxJson.Serialize(blocks[0].Node));
        WorkspaceAstOperation[] operations = [new ReplaceWorkspaceNode(blocks[0].Handle, blocks[0].Node, decoded with { Rules = [] }), new ReplaceWorkspaceNode(blocks[1].Handle, blocks[1].Node, decoded with { Rules = [] }), new ReplaceWorkspaceNode(blocks[2].Handle, blocks[2].Node, decoded)];
        workspace.ProposeAuthoring(Request(workspace) with { Operations = [.. operations] }).Accepted.ShouldBeFalse();
        workspace.ProposeAuthoring(Request(workspace) with { Operations = [new RemoveWorkspaceNode(rule.Handle, rule.Node), .. operations] }).Accepted.ShouldBeFalse();
    }

    [Fact]
    public void should_claim_validated_reused_images_once_across_transaction_regions()
    {
        var workspace = Workspace(Prefix + "            implementation\n              hint \"Keep\"\n        validate\n          label rule Check\n            implementation\n              hint \"Keep\"\n        validate\n          label rule Check\n            implementation\n              hint \"Keep\"");
        var blocks = WorkspaceSyntaxIndex.Create(workspace).Entries.Where(entry => entry.Node is DeclarativeValidateSyntax).ToArray();
        var original = (DeclarativeValidateSyntax)blocks[0].Node;
        var decoded = (DeclarativeValidateSyntax)SyntaxJson.Deserialize(SyntaxJson.Serialize(original));
        var renamed = decoded.Rules.Single() with { Value = new PathExpressionSyntax("Renamed", decoded.Location), Implementation = decoded.Rules.Single().Implementation! with { Hints = [new("Edited", decoded.Location)] } };
        var refused = workspace.ProposeAuthoring(Request(workspace) with
        {
            Operations = [new ReplaceWorkspaceNode(blocks[0].Handle, original, original), new ReplaceWorkspaceNode(blocks[1].Handle, blocks[1].Node, decoded with { Rules = [] }), new ReplaceWorkspaceNode(blocks[2].Handle, blocks[2].Node, decoded with { Rules = [renamed] })]
        });
        refused.Accepted.ShouldBeFalse();
        refused.Conflicts.Single().Message.Contains("pending", StringComparison.OrdinalIgnoreCase).ShouldBeTrue();
    }

    [Fact]
    public void should_conserve_each_owner_across_node_and_document_transaction_regions()
    {
        const string source = Prefix + "            implementation\n              hint \"Keep\"\n        validate\n          label rule Check\n            implementation\n              hint \"Keep\"";
        var workspace = ScreenplayWorkspace.Create("A", [Document("model.play", source), Document("other.play", source.Replace("slice StateChange S", "slice StateChange Other", StringComparison.Ordinal).Replace("command C", "command Other", StringComparison.Ordinal))], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("A")));
        var document = workspace.Documents.Single(entry => entry.Path.Value == "model.play");
        var other = workspace.Documents.Single(entry => entry.Path.Value == "other.play");
        var index = WorkspaceSyntaxIndex.Create(workspace);
        var blocks = index.Entries.Where(entry => entry.Handle.Document == document.Id && entry.Node is DeclarativeValidateSyntax).ToArray();
        var first = index.Entries.First(entry => entry.Handle.Document == document.Id && entry.Node is ValidationRuleSyntax);
        var second = index.Entries.First(entry => entry.Handle.Document == other.Id && entry.Node is ValidationRuleSyntax);
        var parsed = new ScreenplayCompiler().Parse(Prefix.Replace("rule Check", "rule Renamed", StringComparison.Ordinal) + "            implementation\n              hint \"Edited\"").Value!;
        var block = (DeclarativeValidateSyntax)SyntaxJson.Deserialize(SyntaxJson.Serialize(parsed.Modules.Single().Features.Single().Slices.Single().Commands.Single().Validations.Single()));
        var replacement = (ApplicationSyntax)SyntaxJson.Deserialize(SyntaxJson.Serialize(new ScreenplayCompiler().Parse(Prefix.Replace("slice StateChange S", "slice StateChange Other", StringComparison.Ordinal).Replace("command C", "command Other", StringComparison.Ordinal).Replace("rule Check", "rule Renamed", StringComparison.Ordinal) + "            implementation\n              hint \"Edited\"").Value!));
        WorkspaceAstOperation[] operations = [new ReplaceWorkspaceNode(blocks[0].Handle, blocks[0].Node, block with { Rules = [] }), new ReplaceWorkspaceNode(blocks[1].Handle, blocks[1].Node, block)];
        var request = Request(workspace) with { Operations = [.. operations], Documents = [new ReplaceWorkspaceSyntaxDocument(other.Id, replacement)] };
        workspace.ProposeAuthoring(request).Accepted.ShouldBeFalse();
        workspace.ProposeAuthoring(request with { Operations = [new RemoveWorkspaceNode(first.Handle, first.Node), .. operations] }).Accepted.ShouldBeFalse();
        var accepted = workspace.ProposeAuthoring(request with { Operations = [new RemoveWorkspaceNode(first.Handle, first.Node), new RemoveWorkspaceNode(second.Handle, second.Node), .. operations] });
        Assert.True(accepted.Accepted, string.Join("; ", accepted.Conflicts.Select(conflict => conflict.Message)));
        WorkspaceNamedRuleIntentInventory.Create(accepted.Workspace!).Entries.All(entry => entry.RequirementId is null).ShouldBeTrue();
    }

    [Fact]
    public void should_preserve_attached_roles_and_canonical_bytes_after_transaction_region_attachment_and_unwrap()
    {
        const string attached = "\n        validate\n          label rule Attached\n            implementation // attached wrapper\n              hint \"Keep\" // attached guidance\n              file B.cs // attached source";
        var workspace = Workspace(Prefix + "            implementation\n              hint \"Keep\"\n        validate\n          label rule Other\n            implementation\n              hint \"Keep\"" + attached + "\nconcept Label : String\n  validate\n    not empty");
        var blocks = WorkspaceSyntaxIndex.Create(workspace).Entries.Where(entry => entry.Node is DeclarativeValidateSyntax && entry.Handle.Path.StartsWith("/modules/", StringComparison.Ordinal)).ToArray();
        var source = Prefix + "            file A.cs\n        validate\n          label rule Other\n            ```csharp\n            return true;\n            ```" + attached + "\nconcept Label : String\n  validate\n    not empty";
        var direct = Workspace(source);
        var parsed = new ScreenplayCompiler().Parse(source, "model.play").Value!;
        var replacements = parsed.Modules.Single().Features.Single().Slices.Single().Commands.Single().Validations.ToArray();
        var result = workspace.ProposeAuthoring(Request(workspace) with
        {
            Operations = [new ReplaceWorkspaceNode(blocks[0].Handle, blocks[0].Node, SyntaxJson.Deserialize(SyntaxJson.Serialize(replacements[0]))), new ReplaceWorkspaceNode(blocks[1].Handle, blocks[1].Node, SyntaxJson.Deserialize(SyntaxJson.Serialize(replacements[1])))]
        });
        Assert.True(result.Accepted, string.Join("; ", result.Conflicts.Select(conflict => conflict.Message)));
        result.Workspace!.Compilation.Success.ShouldBeTrue();
        SemanticModelSerializer.Serialize(result.Workspace.Compilation.Value!.Model).ShouldEqual(SemanticModelSerializer.Serialize(direct.Compilation.Value!.Model));
        SemanticTypedContextSerializer.Serialize(result.Workspace.Compilation.TypedContextDescriptors).ShouldEqual(SemanticTypedContextSerializer.Serialize(direct.Compilation.TypedContextDescriptors));
        result.Workspace.Compilation.ImplementationRequirements.Select(requirement => (requirement.RequirementId, requirement.ContentHash, requirement.Role)).ShouldEqual(direct.Compilation.ImplementationRequirements.Select(requirement => (requirement.RequirementId, requirement.ContentHash, requirement.Role)));
        foreach (var comment in new[] { "// attached wrapper", "// attached guidance", "// attached source" }) result.Workspace.Documents.Single().Text.Split(comment, StringSplitOptions.None).Length.ShouldEqual(2);
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

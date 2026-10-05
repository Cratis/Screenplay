// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring;

public class when_renaming_owners_with_pending_and_bare_rules : Specification
{
    ScreenplayWorkspace _workspace;
    WorkspaceRenameRequest _request;
    WorkspaceAuthoringResult _canonical;
    WorkspaceAuthoringResult _trivia;

    void Establish()
    {
        _workspace = Workspace("inline", true);
        var target = WorkspaceSyntaxIndex.Create(_workspace).Entries.Single(entry => entry.Node is CommandSyntax);
        _request = new()
        {
            ExpectedRevision = _workspace.Revision,
            ExpectedCatalogRevision = _workspace.IdentityCatalog.Revision,
            Target = target.Handle,
            ExpectedName = "C",
            NewName = "Renamed",
            Validation = WorkspaceAuthoringValidation.Authoring
        };
    }

    void Because()
    {
        _canonical = _workspace.ProposeRename(_request with { Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments });
        _trivia = _workspace.ProposeRename(_request with { Formatting = WorkspaceAuthoringFormatting.PreserveTrivia });
    }

    [Fact] void should_accept_the_generated_command_rename_in_canonical_mode() => _canonical.Accepted.ShouldBeTrue();
    [Fact] void should_accept_the_generated_command_rename_in_trivia_mode() => _trivia.Accepted.ShouldBeTrue();

    const string Slice = "slice StateChange S // slice\n  command C // command\n    label String\n    validate\n      label rule Check\n        implementation // pending wrapper\n          hint \"Keep\" // pending guidance\n";
    const string Bare = "    validate\n      label rule Check // bare\n";
    const string Attached = "    validate\n      label rule Attached\n        implementation // attached wrapper\n          hint \"Attached\" // attached guidance\n          file A.cs // attached source\n";

    public static TheoryData<string, string, WorkspaceAuthoringFormatting, bool> Cases
    {
        get
        {
            var cases = new TheoryData<string, string, WorkspaceAuthoringFormatting, bool>();
            foreach (var layout in new[] { "inline", "scoped", "native" })
            {
                foreach (var name in new[] { "C", "S", "F", "M" })
                {
                    foreach (var formatting in new[] { WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments, WorkspaceAuthoringFormatting.PreserveTrivia })
                    {
                        foreach (var duplicate in new[] { false, true })
                        {
                            cases.Add(layout, name, formatting, duplicate);
                        }
                    }
                }
            }

            return cases;
        }
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void should_preserve_verified_generated_rule_correspondence_and_assigned_owner_identity(string layout, string name, WorkspaceAuthoringFormatting formatting, bool duplicate)
    {
        var workspace = Workspace(layout, duplicate);
        var index = WorkspaceSyntaxIndex.Create(workspace);
        index.Diagnostics.ShouldBeEmpty();
        var target = index.Entries.First(entry => entry.Address?.Name == name && entry.Node is CommandSyntax or SliceSyntax or FeatureSyntax or ModuleSyntax);
        var before = WorkspaceNamedRuleIntentInventory.Create(workspace).Entries;
        var requirement = workspace.Compilation.ImplementationRequirements.Single();
        var result = workspace.ProposeRename(new()
        {
            ExpectedRevision = workspace.Revision,
            ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
            Target = target.Handle,
            ExpectedName = name,
            NewName = "Renamed",
            Validation = WorkspaceAuthoringValidation.Authoring,
            Formatting = formatting
        });
        Assert.True(result.Accepted, $"{layout}/{name}/{formatting}/{duplicate}: {string.Join("; ", result.Conflicts.Select(conflict => conflict.Message))}");
        var candidate = result.Workspace!;
        var after = WorkspaceNamedRuleIntentInventory.Create(candidate).Entries;
        after.Length.ShouldEqual(before.Length);
        var pending = after.Single(entry => entry.State == "pending");
        pending.Hints.SequenceEqual(["Keep"]).ShouldBeTrue();
        pending.RequirementId.ShouldBeNull();
        pending.OwnerId.ShouldEqual(before.Single(entry => entry.State == "pending").OwnerId);
        pending.IsProvisional.ShouldBeFalse();
        pending.Owner.Parts.Any(part => part.Key == "Renamed").ShouldBeTrue();
        var attached = candidate.Compilation.ImplementationRequirements.Single();
        attached.RequirementId.ShouldEqual(requirement.RequirementId);
        attached.ContentHash.ShouldEqual(requirement.ContentHash);
        attached.Role.ShouldEqual(requirement.Role);
        after.Single(entry => entry.State == "file").Hints.SequenceEqual(["Attached"]).ShouldBeTrue();
        candidate.IdentityCatalog.Semantics.Select(assignment => assignment.Id).OrderBy(id => id.ToString()).ShouldEqual(workspace.IdentityCatalog.Semantics.Select(assignment => assignment.Id).OrderBy(id => id.ToString()));
        if (formatting == WorkspaceAuthoringFormatting.PreserveTrivia)
        {
            foreach (var comment in new[] { "// pending wrapper", "// pending guidance", "// attached wrapper", "// attached guidance", "// attached source" })
            {
                string.Join('\n', candidate.Documents.Select(document => document.Text)).Split(comment, StringSplitOptions.None).Length.ShouldEqual(2);
            }
            foreach (var document in workspace.Documents)
            {
                var expected = document.Text.Replace($"module {name}", "module Renamed", StringComparison.Ordinal)
                    .Replace($"feature {name}", "feature Renamed", StringComparison.Ordinal)
                    .Replace($"slice StateChange {name}", "slice StateChange Renamed", StringComparison.Ordinal)
                    .Replace($"command {name}", "command Renamed", StringComparison.Ordinal);
                candidate.Documents.Single(current => current.Id == document.Id).Bytes.ToArray().ShouldEqual(Encoding.UTF8.GetBytes(expected));
            }
        }
        var restarted = ScreenplayWorkspaceSerializer.Deserialize(ScreenplayWorkspaceSerializer.Serialize(candidate));
        WorkspaceNamedRuleIntentInventory.Create(restarted).Entries.Single(entry => entry.State == "pending").OwnerId.ShouldEqual(pending.OwnerId);
    }

    [Fact]
    public void should_refuse_a_programmatic_generated_rewrite_with_the_wrong_shape()
    {
        var workspace = Workspace("inline", true);
        var document = workspace.Documents.Single();
        var syntax = new ScreenplayCompiler().Parse(document.Text + "concept Extra : String").Value!;
        var result = new WorkspaceAuthoringTransaction(workspace, shapePreservingReplacements: new HashSet<DocumentId> { document.Id }).Propose(new()
        {
            ExpectedRevision = workspace.Revision,
            ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
            Validation = WorkspaceAuthoringValidation.Authoring,
            Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments,
            Documents = [new ReplaceWorkspaceSyntaxDocument(document.Id, syntax)]
        });
        result.Accepted.ShouldBeFalse();
        result.Conflicts.Single().Message.ShouldContain("changed the syntax shape of a document");
    }

    [Fact]
    public void should_not_treat_an_arbitrary_shape_preserving_document_replacement_as_a_generated_rename()
    {
        var workspace = Workspace("inline", true);
        var document = workspace.Documents.Single();
        var syntax = (ApplicationSyntax)SyntaxJson.Deserialize(SyntaxJson.Serialize(new ScreenplayCompiler().Parse(document.Text.Replace("command C", "command Renamed", StringComparison.Ordinal), document.Path.Value).Value!));
        var owner = WorkspaceNamedRuleIntentInventory.Create(workspace).Entries[0].Owner;
        var renamed = SemanticAddress.ForCommand(SemanticAddress.ForSlice(workspace.IdentityCatalog.Application, "M", ["F"], "S"), "Renamed");
        var result = workspace.ProposeAuthoring(new()
        {
            ExpectedRevision = workspace.Revision,
            ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
            Validation = WorkspaceAuthoringValidation.Authoring,
            Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments,
            Documents = [new ReplaceWorkspaceSyntaxDocument(document.Id, syntax)],
            SemanticRenames = [new(owner, renamed), new(SemanticAddress.ForProperty(owner, "label"), SemanticAddress.ForProperty(renamed, "label"))]
        });
        result.Accepted.ShouldBeFalse();
        result.Conflicts.Single().Message.Contains("pending", StringComparison.OrdinalIgnoreCase).ShouldBeTrue();
    }

    static ScreenplayWorkspace Workspace(string layout, bool duplicate)
    {
        var slice = Slice + (duplicate ? Bare : "") + Attached;
        ImmutableArray<WorkspaceDocument> documents = layout switch
        {
            "inline" => [Document("model.play", "module M // module\n  feature F // feature\n" + string.Join('\n', slice.Split('\n').Select(line => line.Length > 0 ? "    " + line : line)))],
            "scoped" => [Document("root.play", "module M // module\n  feature F // feature\n    import \"leaf.play\"\n"), Document("leaf.play", slice)],
            _ => [Document("root.play", "module M // module\n  import \"native.play\"\n"), Document("native.play", "module M // restated\n  feature F // feature\n    import \"leaf.play\"\n"), Document("leaf.play", slice)]
        };
        var application = ApplicationIdentity.Create("A");
        var provisional = ScreenplayWorkspace.Create("A", documents, SemanticIdentityCatalog.Empty(application));
        var assignments = WorkspaceSyntaxIndex.Create(provisional).Entries.Where(entry => entry.Address is not null).Select(entry => entry.Address!).Distinct()
            .Select(address => new SemanticIdentityAssignment(address, SemanticId.Create(address), SemanticIdentityOrigin.Persisted));

        return ScreenplayWorkspace.Create("A", documents, SemanticIdentityCatalog.Create(application, [], [.. assignments], []));
    }

    static WorkspaceDocument Document(string path, string source) => WorkspaceDocument.Create(path, PortablePlayPath.Parse(path), Encoding.UTF8.GetBytes(source));
}

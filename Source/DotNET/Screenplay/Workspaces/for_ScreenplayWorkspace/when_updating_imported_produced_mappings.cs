// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Workspaces.for_ScreenplayWorkspace;

public class when_updating_imported_produced_mappings
{
    const string Slice = "// café: oldName is not a patch span\nslice StateChange S\n  command C\n    key String identifier\n    oldName String\n    newName String\n    produces E\n      for key\n      key = key\n      value = oldName  // keep oldName café\n  event E\n    key String\n    value String\n";

    [Theory]
    [InlineData("scoped")]
    [InlineData("native")]
    [InlineData("transitive")]
    public void should_patch_the_original_resolved_source_and_verify_placement_aware_canonical_equivalence(string layout)
    {
        var workspace = Workspace(layout);
        workspace.Compilation.Success.ShouldBeTrue();
        var document = workspace.Documents.Single(value => value.Path.Value == "imported.play");
        var operation = Operation(workspace);
        var before = WorkspaceSyntaxIndex.Create(workspace);
        var mapping = Mapping(before);
        var result = workspace.Propose(new()
        {
            ExpectedRevision = workspace.Revision,
            ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
            Operations = [operation]
        });
        result.Success.ShouldBeTrue();
        var candidate = result.Workspace!;
        var expected = document.Text.Replace("value = oldName  //", "value = newName  //", StringComparison.Ordinal);
        candidate.Documents.Single(value => value.Id == document.Id).Bytes.ToArray().ShouldEqual([0xef, 0xbb, 0xbf, .. Encoding.UTF8.GetBytes(expected)]);
        result.WritePlan!.Entries.Single().Document.ShouldEqual(document.Id);
        RequireContinuity(workspace, candidate, document.Id, mapping);
        before.Find(mapping.Handle)!.Node.ShouldEqual(mapping.Node);
        ((PathExpressionSyntax)((PropertyMappingSyntax)mapping.Node).Source).Path.ShouldEqual("oldName");
    }

    [Theory]
    [InlineData("scoped", WorkspaceAuthoringFormatting.PreserveTrivia)]
    [InlineData("native", WorkspaceAuthoringFormatting.PreserveTrivia)]
    [InlineData("transitive", WorkspaceAuthoringFormatting.PreserveTrivia)]
    [InlineData("scoped", WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments)]
    [InlineData("native", WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments)]
    [InlineData("transitive", WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments)]
    public void should_support_both_typed_mapping_formatting_policies_without_changing_other_source_or_identities(string layout, WorkspaceAuthoringFormatting formatting)
    {
        var workspace = Workspace(layout);
        var index = WorkspaceSyntaxIndex.Create(workspace);
        var mapping = Mapping(index);
        var original = (PropertyMappingSyntax)mapping.Node;
        var intended = original with { Source = ((PathExpressionSyntax)original.Source) with { Path = "newName" } };
        var result = workspace.ProposeAuthoring(new()
        {
            ExpectedRevision = workspace.Revision,
            ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
            Validation = WorkspaceAuthoringValidation.Executable,
            Formatting = formatting,
            Operations = [new ReplaceWorkspaceNode(mapping.Handle, original, intended)]
        });
        result.Accepted.ShouldBeTrue();
        result.ExecutableReady.ShouldBeTrue();
        var document = workspace.Documents.Single(value => value.Id == mapping.Handle.Document);
        var candidate = result.Workspace!;
        var printed = candidate.Documents.Single(value => value.Id == document.Id);
        RequireContinuity(workspace, candidate, document.Id, mapping);
        result.WritePlan!.Entries.Single().Document.ShouldEqual(document.Id);
        if (formatting == WorkspaceAuthoringFormatting.PreserveTrivia)
        {
            printed.Bytes.ToArray().ShouldEqual([0xef, 0xbb, 0xbf, .. Encoding.UTF8.GetBytes(document.Text.Replace("value = oldName  //", "value = newName  //", StringComparison.Ordinal))]);
        }
        else
        {
            printed.Text.ShouldContain("value = newName");
            printed.Text.ShouldContain("keep oldName café");
            printed.Text.Contains("module M", StringComparison.Ordinal).ShouldBeFalse();
            printed.Text.Contains("feature F", StringComparison.Ordinal).ShouldEqual(layout == "native");
            result.AuthoringDiagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.AuthoringSourceNormalization).ShouldBeTrue();
        }
    }

    [Theory]
    [InlineData("scoped")]
    [InlineData("native")]
    public void should_refuse_multiple_placements_without_any_wrong_or_partial_edits(string layout)
    {
        var workspace = Workspace(layout);
        var documents = workspace.Documents.Select(document => document.Path.Value == "root.play"
            ? WorkspaceDocument.Create(document.Id, document.StableKey, document.Path, Encoding.UTF8.GetBytes("module M\n  feature F\n    import \"imported.play\"\n  feature G\n    import \"imported.play\"\n"))
            : document);
        var unresolved = ScreenplayWorkspace.Create("A", [.. documents], workspace.IdentityCatalog);
        unresolved.Compilation.Success.ShouldBeFalse();
        var result = unresolved.Propose(new()
        {
            ExpectedRevision = unresolved.Revision,
            ExpectedCatalogRevision = unresolved.IdentityCatalog.Revision,
            Operations = [Operation(workspace)]
        });
        result.Success.ShouldBeFalse();
        result.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.CompilationFailed);
        result.Workspace.ShouldBeNull();
        result.WritePlan.ShouldBeNull();
        var canonical = ProducedEventMappingSourcePatch.VerifyCanonical(unresolved);
        canonical!.Kind.ShouldEqual(WorkspaceConflictKind.UnsupportedSemanticField);
        canonical.Message.ShouldContain("UnresolvedPlacement:");
    }

    [Theory]
    [InlineData("scoped")]
    [InlineData("native")]
    public void should_refuse_an_imported_mapping_that_is_not_a_direct_command_property(string layout)
    {
        var workspace = Workspace(layout);
        var documents = workspace.Documents.Select(document => document.Path.Value == "imported.play"
            ? WorkspaceDocument.Create(document.Id, document.StableKey, document.Path, Encoding.UTF8.GetBytes(document.Text.Replace("value = oldName  //", "value = \"literal\"  //", StringComparison.Ordinal)))
            : document);
        var unsupported = ScreenplayWorkspace.Create("A", [.. documents], workspace.IdentityCatalog);
        unsupported.Compilation.Success.ShouldBeTrue();
        var result = unsupported.Propose(new()
        {
            ExpectedRevision = unsupported.Revision,
            ExpectedCatalogRevision = unsupported.IdentityCatalog.Revision,
            Operations = [Operation(workspace)]
        });
        result.Success.ShouldBeFalse();
        result.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.UnsupportedSemanticField);
        result.Workspace.ShouldBeNull();
        result.WritePlan.ShouldBeNull();
    }

    [Fact]
    public void should_refuse_a_drifted_source_before_using_original_mapping_ranges()
    {
        var workspace = Workspace("scoped");
        var document = workspace.Documents.Single(value => value.Path.Value == "imported.play");
        var drifted = WorkspaceDocument.Create(document.Id, document.StableKey, document.Path, Encoding.UTF8.GetBytes("// unrelated drift\n" + document.Text));
        var candidates = workspace.Documents.ToDictionary(value => value.Id);
        candidates[document.Id] = drifted;
        var conflict = ProducedEventMappingPatch.Apply(Operation(workspace), workspace, candidates);
        conflict!.Kind.ShouldEqual(WorkspaceConflictKind.UnsupportedSemanticField);
        conflict.Message.ShouldContain("original source snapshot");
        ReferenceEquals(candidates[document.Id], drifted).ShouldBeTrue();
    }

    static void RequireContinuity(ScreenplayWorkspace workspace, ScreenplayWorkspace candidate, DocumentId edited, WorkspaceSyntaxEntry mapping)
    {
        SemanticIdentityCatalogSerializer.Serialize(candidate.IdentityCatalog).ShouldEqual(SemanticIdentityCatalogSerializer.Serialize(workspace.IdentityCatalog));
        foreach (var document in workspace.Documents.Where(value => value.Id != edited))
        {
            ReferenceEquals(candidate.Documents.Single(value => value.Id == document.Id), document).ShouldBeTrue();
        }

        var index = WorkspaceSyntaxIndex.Create(candidate);
        index.Diagnostics.ShouldBeEmpty();
        var after = index.Find(mapping.Handle with { Revision = candidate.Revision })!;
        ((PathExpressionSyntax)((PropertyMappingSyntax)after.Node).Source).Path.ShouldEqual("newName");
        var intended = WorkspaceSyntaxMutation.Json(WorkspaceSyntaxIndex.Create(workspace).Find(mapping.Handle with { Path = string.Empty })!.Node);
        WorkspaceSyntaxMutation.Set(intended, $"{mapping.Handle.Path}/source/path", "newName");
        SyntaxJson.StructurallyEqual(WorkspaceSyntaxMutation.Syntax(intended), index.Find(after.Handle with { Path = string.Empty })!.Node).ShouldBeTrue();
        var operation = Operation(workspace);
        var command = candidate.Compilation.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single().Commands.Single();
        ((SemanticResolvedExpression)command.Produces.Single().Mappings.Single(value => value.TargetProperty == operation.TargetProperty).Source).Target.ShouldEqual(operation.NewSourceCommandProperty);
        command.Id.ShouldEqual(operation.Command);
    }

    static WorkspaceSyntaxEntry Mapping(WorkspaceSyntaxIndex index) => index.Entries.Single(entry => entry.Node is PropertyMappingSyntax mapping && mapping.Property == "value");

    static UpdateProducedEventMappingSource Operation(ScreenplayWorkspace workspace)
    {
        var slice = workspace.Compilation.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single();
        var command = slice.Commands.Single();
        var @event = slice.Events.Single();
        return new()
        {
            Command = command.Id,
            ProducedEvent = @event.Id,
            TargetProperty = @event.Properties.Single(value => value.Name == "value").Id,
            ExpectedSourceCommandProperty = command.Properties.Single(value => value.Name == "oldName").Id,
            NewSourceCommandProperty = command.Properties.Single(value => value.Name == "newName").Id
        };
    }

    static ScreenplayWorkspace Workspace(string layout)
    {
        var imported = layout == "native" ? "module M\n  feature F\n" + string.Join('\n', Slice.Split('\n').Select(line => "    " + line)) : Slice;
        var root = layout switch
        {
            "native" => "module M\n  import \"imported.play\"\n",
            "transitive" => "module M\n  feature F\n    import \"barrel.play\"\n",
            _ => "module M\n  feature F\n    import \"imported.play\"\n"
        };
        var documents = new List<WorkspaceDocument>
        {
            Document("root.play", root, false),
            Document("imported.play", imported, true),
            Document("unrelated.play", "// untouched oldName\nconcept Label : String\n", false)
        };
        if (layout == "transitive") documents.Add(Document("barrel.play", "// transitive\nimport \"imported.play\"\n", false));
        var application = ApplicationIdentity.Create("A");
        var provisional = ScreenplayWorkspace.Create("A", [.. documents], SemanticIdentityCatalog.Empty(application));
        var catalog = SemanticIdentityCatalog.Create(
            application,
            [.. provisional.IdentityCatalog.Documents.Select(assignment => assignment with { Origin = SemanticIdentityOrigin.Persisted })],
            [.. provisional.IdentityCatalog.Semantics.Select(assignment => assignment with { Origin = SemanticIdentityOrigin.Persisted })],
            [.. provisional.IdentityCatalog.EventContracts.Select(assignment => assignment with { Origin = SemanticIdentityOrigin.Persisted })]);
        return ScreenplayWorkspace.Create("A", provisional.Documents, catalog);
    }

    static WorkspaceDocument Document(string path, string source, bool bom)
    {
        var bytes = Encoding.UTF8.GetBytes(source.Replace("\n", "\r\n", StringComparison.Ordinal));
        return WorkspaceDocument.Create(path, PortablePlayPath.Parse(path), bom ? [0xef, 0xbb, 0xbf, .. bytes] : bytes);
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring;

public class when_migrating_placed_optionality
{
    [Theory]
    [InlineData(false, WorkspaceAuthoringFormatting.PreserveTrivia)]
    [InlineData(true, WorkspaceAuthoringFormatting.PreserveTrivia)]
    [InlineData(false, WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments)]
    [InlineData(true, WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments)]
    public void should_discover_and_propose_the_same_repair_without_changing_placement(bool native, WorkspaceAuthoringFormatting formatting)
    {
        const string body = "// café?\r\nslice StateChange S\r\n  command C\r\n    value  String? // keep?\r\n    handler\r\n      implementation";
        var imported = native ? "// native\r\nmodule M\r\n  feature F\r\n" + string.Join("\r\n", body.Split("\r\n").Select(line => "    " + line)) : body;
        var document = WorkspaceDocument.Create("imported", PortablePlayPath.Parse("imported.play"), [0xef, 0xbb, 0xbf, .. Encoding.UTF8.GetBytes(imported)]);
        var root = Document("root.play", native ? "module M\n  import \"imported.play\"" : "module M\n  feature F\n    import \"imported.play\"");
        var independent = Document("independent.play", "// untouched\r\ntype Details\r\n  note String?\r\n");
        var workspace = ScreenplayWorkspace.Create("A", [root, document, independent], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("A")));
        var index = WorkspaceSyntaxIndex.Create(workspace);
        var before = (ApplicationSyntax)index.Entries.Single(entry => entry.Handle.Document == document.Id && entry.Parent is null).Node;
        var handle = new WorkspaceNodeHandle(workspace.Revision, document.Id, string.Empty);
        var recipe = WorkspaceDiagnosticRepairs.FindDocumentOptionality(index, handle).Single();
        var diagnostic = index.Diagnostics.Single(value => value.Code == DiagnosticCodes.LegacyOptionalSuffix && value.Location.Path == document.Path.Value);
        WorkspaceDiagnosticRepairs.Find(index, workspace.Revision, diagnostic).Length.ShouldEqual(1);
        var result = WorkspaceDiagnosticRepairs.ProposeRepair(workspace, recipe, new()
        {
            ExpectedRevision = workspace.Revision,
            ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
            Validation = WorkspaceAuthoringValidation.Authoring,
            Formatting = formatting
        });
        result.Accepted.ShouldBeTrue();
        var candidate = result.Workspace!;
        var printed = candidate.Documents.Single(value => value.Id == document.Id);
        var after = WorkspaceSyntaxIndex.Create(candidate);
        SyntaxJson.StructurallyEqual(before, after.Entries.Single(entry => entry.Handle.Document == document.Id && entry.Parent is null).Node).ShouldBeTrue();
        after.Diagnostics.Any(value => value.Code == DiagnosticCodes.LegacyOptionalSuffix && value.Location.Path == document.Path.Value).ShouldBeFalse();
        candidate.Documents.Single(value => value.Id == root.Id).Bytes.ShouldEqual(root.Bytes);
        candidate.Documents.Single(value => value.Id == independent.Id).Bytes.ShouldEqual(independent.Bytes);
        result.WritePlan!.Entries.Length.ShouldEqual(1);
        if (formatting == WorkspaceAuthoringFormatting.PreserveTrivia)
        {
            printed.Bytes.ToArray().ShouldEqual([0xef, 0xbb, 0xbf, .. Encoding.UTF8.GetBytes(imported.Replace("String?", "String optional", StringComparison.Ordinal))]);
        }
        else
        {
            printed.Text.ShouldContain("slice StateChange S");
            printed.Text.Contains("feature F", StringComparison.Ordinal).ShouldEqual(native);
            printed.Text.Contains("module M", StringComparison.Ordinal).ShouldBeFalse();
        }
    }

    static WorkspaceDocument Document(string path, string source) => WorkspaceDocument.Create(path, PortablePlayPath.Parse(path), Encoding.UTF8.GetBytes(source));
}

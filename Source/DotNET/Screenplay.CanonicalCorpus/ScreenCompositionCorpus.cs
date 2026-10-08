// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.CanonicalCorpus;

/// <summary>
/// Provides the canonical screen composition conformance corpus.
/// </summary>
public static class ScreenCompositionCorpus
{
    const string Prefix = "Cratis.Screenplay.CanonicalCorpus.Corpus.ScreenComposition.v1";

    /// <summary>
    /// Gets the canonical screen composition corpus.
    /// </summary>
    public static CanonicalScreenCorpusVector V1 { get; } = LoadV1();

    static CanonicalScreenCorpusVector LoadV1()
    {
        var folder = FolderForm();
        return new CanonicalScreenCorpusVector
        {
            Name = "screen-composition/v1",
            ApplicationName = "Workspaces",
            ApplicationIdentity = ApplicationIdentity.Parse("app1:20ccb167f2400bc55fae1597b1a0f4d19b40841f513bd013a7fa815e9e7f2994"),
            SourceForms =
            [
                new CanonicalCorpusSourceForm
                {
                    Name = "single",
                    Documents = [Document("screen-workspaces-vector", "ScreenWorkspaces.play", $"{Prefix}.source.ScreenWorkspaces.play")],
                    IdentityCatalogBytes = Resource($"{Prefix}.identity.single-catalog-v1.json")
                },
                folder,
                new CanonicalCorpusSourceForm
                {
                    Name = "reordered",
                    Documents = [.. folder.Documents.Reverse()],
                    IdentityCatalogBytes = folder.IdentityCatalogBytes
                },
                new CanonicalCorpusSourceForm
                {
                    Name = "relocated",
                    Documents = [.. folder.Documents.Reverse().Select(document => new CanonicalCorpusDocument
                    {
                        StableKey = document.StableKey,
                        DisplayPath = $"Archive/{document.DisplayPath}",
                        Bytes = document.Bytes
                    })],
                    IdentityCatalogBytes = folder.IdentityCatalogBytes
                }
            ],
            EsmDiagnostics =
            [
                Diagnostic("PLAY0269", "UI profile 'Desktop' is explicitly deferred from the backend ESM v1 profile."),
                Diagnostic("PLAY0269", "Layout 'AppShell' is explicitly deferred from the backend ESM v1 profile."),
                Diagnostic("PLAY0269", "Behavior 'ConfirmClose' is explicitly deferred from the backend ESM v1 profile."),
                Diagnostic("PLAY0268", "Query 'AllWorkItems' uses delivery, filtering, scope, or implementation behavior outside the first ESM v1 vertical."),
                Diagnostic("PLAY0268", "Query 'AllWorkItems' must declare one caller-supplied 'by' argument in the first ESM v1 vertical."),
                Diagnostic("PLAY0268", "Query 'CommentsForWorkItem' uses delivery, filtering, scope, or implementation behavior outside the first ESM v1 vertical."),
                Diagnostic("PLAY0268", "Query 'CommentsForWorkItem' must return one optional read model in the first ESM v1 vertical."),
                Diagnostic("PLAY0269", "Screen template 'MasterDetail' is explicitly deferred from the backend ESM v1 profile."),
                Diagnostic("PLAY0269", "Dialog template 'EditDialog' is explicitly deferred from the backend ESM v1 profile."),
                Diagnostic("PLAY0269", "Form 'CreateWorkItemForm' is explicitly deferred from the backend ESM v1 profile."),
                Diagnostic("PLAY0269", "Form 'RenameWorkItemForm' is explicitly deferred from the backend ESM v1 profile."),
                Diagnostic("PLAY0269", "Contribution to 'Navigation' is explicitly deferred from the backend ESM v1 profile."),
                Diagnostic("PLAY0269", "Screen 'WorkItemList' is explicitly deferred from the backend ESM v1 profile."),
                Diagnostic("PLAY0269", "Screen 'WorkItemDetails' is explicitly deferred from the backend ESM v1 profile."),
                Diagnostic("PLAY0269", "Screen 'CommentThread' is explicitly deferred from the backend ESM v1 profile.")
            ],
            BehaviorProbes =
            [
                Probe("master-detail-selection", "Selecting a row navigates from WorkItemList to WorkItemDetails by workItemId.", "single", "screen WorkItemList", ["ScreenTemplateReferenceSyntax", "ScreenTableSyntax", "ScreenNavigateSyntax"]),
                Probe("query-rebind", "WorkItemDetails rebinds its details query and comments query from the routed workItemId.", "single", "screen WorkItemDetails", ["ScreenDataSyntax", "ScreenSectionSyntax"]),
                Probe("auto-manual-command-forms", "Create and rename command forms stay discoverable for screen actions.", "single", "form CreateWorkItemForm; form RenameWorkItemForm", ["FormSyntax", "FormFieldSyntax"]),
                Probe("recursive-nested-hierarchy", "Comments contain a nested hierarchy section with a table and composer action.", "single", "section comments", ["ScreenSectionSyntax", "ScreenTableSyntax", "ScreenActionSyntax"]),
                Probe("toolbar-dialog-url-surfaces", "Navigation contribution, dialog template and dialog-opening interaction stay authored in the corpus.", "single", "contribute to Navigation; dialog template EditDialog", ["ContributionSyntax", "DialogTemplateSyntax", "OpenDialogActionSyntax"]),
                Probe("package-icon-style-contract", "The desktop UI profile pins package resolution inputs for render hosts.", "single", "ui profile Desktop", ["UiProfileSyntax", "LayoutSyntax"])
            ],
            StagePlans = []
        };
    }

    static CanonicalCorpusSourceForm FolderForm() => new()
    {
        Name = "folder",
        Documents =
        [
            Document("application", "application.play", $"{Prefix}.source.folder.application.play"),
            Document("workspaces-module", "Workspaces/Workspaces.play", $"{Prefix}.source.folder.Workspaces.Workspaces.play"),
            Document("workspaces-tracking-feature", "Workspaces/Tracking/Tracking.play", $"{Prefix}.source.folder.Workspaces.Tracking.Tracking.play"),
            Document("create-work-item-slice", "Workspaces/Tracking/CreateWorkItem/CreateWorkItem.play", $"{Prefix}.source.folder.Workspaces.Tracking.CreateWorkItem.CreateWorkItem.play"),
            Document("rename-work-item-slice", "Workspaces/Tracking/RenameWorkItem/RenameWorkItem.play", $"{Prefix}.source.folder.Workspaces.Tracking.RenameWorkItem.RenameWorkItem.play"),
            Document("close-work-item-slice", "Workspaces/Tracking/CloseWorkItem/CloseWorkItem.play", $"{Prefix}.source.folder.Workspaces.Tracking.CloseWorkItem.CloseWorkItem.play"),
            Document("add-comment-slice", "Workspaces/Tracking/AddComment/AddComment.play", $"{Prefix}.source.folder.Workspaces.Tracking.AddComment.AddComment.play"),
            Document("work-item-list-slice", "Workspaces/Tracking/WorkItemList/WorkItemList.play", $"{Prefix}.source.folder.Workspaces.Tracking.WorkItemList.WorkItemList.play"),
            Document("work-item-details-slice", "Workspaces/Tracking/WorkItemDetails/WorkItemDetails.play", $"{Prefix}.source.folder.Workspaces.Tracking.WorkItemDetails.WorkItemDetails.play"),
            Document("work-item-comments-slice", "Workspaces/Tracking/WorkItemComments/WorkItemComments.play", $"{Prefix}.source.folder.Workspaces.Tracking.WorkItemComments.WorkItemComments.play")
        ],
        IdentityCatalogBytes = Resource($"{Prefix}.identity.folder-catalog-v1.json")
    };

    static CanonicalCorpusDiagnosticExpectation Diagnostic(string code, string message) => new()
    {
        Code = code,
        Message = message
    };

    static CanonicalScreenBehaviorProbe Probe(string name, string behavior, string sourceForm, string declaration, ImmutableArray<string> syntaxKinds) => new()
    {
        Name = name,
        Behavior = behavior,
        SourceForm = sourceForm,
        Declaration = declaration,
        SyntaxKinds = syntaxKinds
    };

    static CanonicalCorpusDocument Document(string stableKey, string path, string resource) => new()
    {
        StableKey = stableKey,
        DisplayPath = path,
        Bytes = Resource(resource)
    };

    static ImmutableArray<byte> Resource(string name)
    {
        using var stream = typeof(ScreenCompositionCorpus).Assembly.GetManifestResourceStream(name) ??
                           throw new InvalidOperationException($"Canonical corpus resource '{name}' is missing");
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return [.. memory.ToArray()];
    }
}

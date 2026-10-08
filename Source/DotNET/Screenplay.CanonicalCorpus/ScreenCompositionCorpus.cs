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
            TypedSourceCases =
            [
                new CanonicalTypedScreenSourceCase
                {
                    Name = "screen-release-ui-positive",
                    Document = Document("screen-release-ui-positive", "positive/ScreenReleaseUi.play", $"{Prefix}.positive.ScreenReleaseUi.play.txt"),
                    Requires = "Screenplay PR #553 typed screen authoring syntax",
                    PendingReason = "Current origin/main has not merged the typed screen binding/component/toolbar/icon authoring parser yet.",
                    ExpectedSyntaxKinds =
                    [
                        "TemplateAssignmentSyntax",
                        "ScreenComponentSyntax",
                        "ScreenToolbarSyntax",
                        "ScreenComponentBindingSyntax",
                        "FormColumnsSyntax",
                        "UiProfileSyntax"
                    ]
                }
            ],
            BehaviorExpectations =
            [
                Behavior(
                    "selection-details",
                    "browser",
                    "A work item list is rendered with two rows and no selected item.",
                    "Select the row with workItemId 3fa85f64-5717-4562-b3fc-2c963f66afa6.",
                    "The details outlet renders WorkItemDetails and routes the selected workItemId into GetWorkItem.",
                    [
                        Assertion("selection.workItemId", "equals", "3fa85f64-5717-4562-b3fc-2c963f66afa6"),
                        Assertion("queries.GetWorkItem.arguments.workItemId", "equals", "3fa85f64-5717-4562-b3fc-2c963f66afa6"),
                        Assertion("outlets.detail.screen", "equals", "WorkItemDetails")
                    ]),
                Behavior(
                    "query-rebind-clear",
                    "browser",
                    "WorkItemDetails is showing an existing selection.",
                    "Clear the selection through the toolbar navigation item.",
                    "The details query is not invoked with a stale workItemId and the empty master/detail outlet is shown.",
                    [
                        Assertion("selection.workItemId", "isNull", string.Empty),
                        Assertion("queries.GetWorkItem.calls", "equals", "0"),
                        Assertion("outlets.detail.screen", "equals", "WorkItemList.emptyState")
                    ]),
                Behavior(
                    "native-form-validation-submit",
                    "browser",
                    "CreateWorkItemForm is opened from the create toolbar item.",
                    "Submit once with an empty title, then with a valid title and priority.",
                    "Native command-form validation blocks the invalid submit and the valid submit executes CreateWorkItem once before navigating to WorkItemDetails.",
                    [
                        Assertion("forms.CreateWorkItem.invalidSubmits", "equals", "1"),
                        Assertion("commands.CreateWorkItem.executions", "equals", "1"),
                        Assertion("navigation.currentScreen", "equals", "WorkItemDetails")
                    ]),
                Behavior(
                    "dialog-outlet-deep-link",
                    "browser",
                    "WorkItemDetails is opened from a deep link route.",
                    "Open the rename action, save the dialog, and follow the details deep link.",
                    "The dialog uses EditWorkItemDialog, writes through the detail outlet and preserves the authored route parameter.",
                    [
                        Assertion("dialogs.current.template", "equals", "EditWorkItemDialog"),
                        Assertion("outlets.actions.component", "equals", "scene.web.CommandForm"),
                        Assertion("routes.current", "equals", "work-items/{workItemId}")
                    ]),
                Behavior(
                    "package-rendering",
                    "stage",
                    "The Web profile resolves scene.web and workspace.icons.",
                    "Render the package-backed DataGrid and DetailsPanel components.",
                    "The host resolves package components, icons and presentation hints without falling back to core placeholders.",
                    [
                        Assertion("packages.components.scene.web.DataGrid", "exists", "true"),
                        Assertion("icons.workspace.icons.save", "exists", "true"),
                        Assertion("fallbacks.placeholderComponents", "equals", "0")
                    ]),
                Behavior(
                    "protected-business-semantics",
                    "compiler",
                    "The UI corpus is compiled for backend ESM admission.",
                    "Compile the current source forms.",
                    "The compiler fails closed with pinned UI/query diagnostics and produces no artifact paths.",
                    [
                        Assertion("diagnostics.codes", "contains", "PLAY0268"),
                        Assertion("diagnostics.codes", "contains", "PLAY0269"),
                        Assertion("artifactPaths.count", "equals", "0")
                    ])
            ],
            McpEditExpectations =
            [
                Mcp(
                    "mcp-edit-component-binding",
                    "replace property selectedItem from component workItems.selectedItem null clear",
                    "component scene.web.DataGrid workItems",
                    [
                        Assertion("proposal.validation", "equals", "Authoring"),
                        Assertion("binding.invalidRawTextPreserved", "equals", "true"),
                        Assertion("identities.changed", "equals", "0")
                    ]),
                Mcp(
                    "mcp-edit-dialog-action",
                    "add dialog toolbar submit action",
                    "screen RenameWorkItemDialog",
                    [
                        Assertion("proposal.droppedComments", "equals", "0"),
                        Assertion("target.template", "equals", "EditWorkItemDialog"),
                        Assertion("source.roundTrips", "equals", "true")
                    ])
            ],
            Harnesses =
            [
                Harness(
                    "browser-runtime",
                    "Scene browser",
                    "screenplay-conformance browser --vector screen-composition/v1 --profile Web",
                    "Scene >=4.10, Stage >=4.40, Screenplay >= PR553 release",
                    "Final released package vector not pinned yet.",
                    [
                        Assertion("behaviors.selection-details", "passes", "true"),
                        Assertion("behaviors.native-form-validation-submit", "passes", "true"),
                        Assertion("behaviors.dialog-outlet-deep-link", "passes", "true")
                    ]),
                Harness(
                    "mcp-authoring",
                    "Screenplay MCP",
                    "screenplay-conformance mcp --vector screen-composition/v1 --source-case screen-release-ui-positive",
                    "Screenplay >= PR553 release",
                    "Typed syntax branch is still open.",
                    [
                        Assertion("mcp.mcp-edit-component-binding", "passes", "true"),
                        Assertion("mcp.mcp-edit-dialog-action", "passes", "true")
                    ]),
                Harness(
                    "cli-stage-parity",
                    "Cratis CLI and Stage",
                    "cratis stage render --profile Web --screenplay screen-composition/v1",
                    "Stage/CLI final screens release vector",
                    "Exact CLI profile flags and Stage plan schema are pending from stage-cli.",
                    [
                        Assertion("sceneJson.matchesLiveStage", "equals", "true"),
                        Assertion("routes.commands.CreateWorkItem", "exists", "true"),
                        Assertion("bindings.generated", "contains", "workItemId")
                    ]),
                Harness(
                    "studio-roundtrip",
                    "Studio",
                    "studio-conformance import-edit-export --vector screen-composition/v1",
                    "Studio final screens release",
                    "Studio production authoring entry point is still being wired.",
                    [
                        Assertion("export.bytesRoundTrip", "equals", "true"),
                        Assertion("designer.componentBindingPreserved", "equals", "true"),
                        Assertion("play.deepLinkPreserved", "equals", "true")
                    ])
            ],
            StagePlans =
            [
                new CanonicalStagePlanExpectation
                {
                    Target = "scene-web",
                    Profile = "Web",
                    PlanDigest = "pending:stage-scene-web-plan-v1",
                    RequiredVersionVector = "Screenplay PR #553 release + Stage final screens release + Scene final screens release + CLI final screens release",
                    PendingReason = "The exact released Stage plan JSON schema, artifact paths and hashes are not published yet.",
                    Artifacts = [],
                    Assertions =
                    [
                        Assertion("plan.profile", "equals", "Web"),
                        Assertion("plan.scene.documents", "contains", "scene.json"),
                        Assertion("plan.routes.commands", "contains", "CreateWorkItem"),
                        Assertion("plan.routes.queries", "contains", "GetWorkItem"),
                        Assertion("plan.nativeForms", "contains", "CreateWorkItemForm"),
                        Assertion("plan.packageFallbacks", "equals", "0")
                    ]
                }
            ]
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

    static CanonicalScreenBehaviorExpectation Behavior(string name, string category, string given, string when, string then, ImmutableArray<CanonicalScreenAssertion> assertions) => new()
    {
        Name = name,
        Category = category,
        SourceCase = "screen-release-ui-positive",
        Given = given,
        When = when,
        Then = then,
        Assertions = assertions
    };

    static CanonicalMcpEditExpectation Mcp(string name, string operation, string targetDeclaration, ImmutableArray<CanonicalScreenAssertion> assertions) => new()
    {
        Name = name,
        SourceCase = "screen-release-ui-positive",
        Operation = operation,
        TargetDeclaration = targetDeclaration,
        Assertions = assertions
    };

    static CanonicalScreenHarnessExpectation Harness(string name, string host, string entryPoint, string requiredVersionVector, string pendingReason, ImmutableArray<CanonicalScreenAssertion> assertions) => new()
    {
        Name = name,
        Host = host,
        EntryPoint = entryPoint,
        RequiredVersionVector = requiredVersionVector,
        PendingReason = pendingReason,
        Assertions = assertions
    };

    static CanonicalScreenAssertion Assertion(string path, string operation, string value) => new()
    {
        Path = path,
        Operation = operation,
        Value = value
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

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
    const string FolderSourceCase = "screen-composition/v1/source/folder";
    const string TypedScreenSourceCase = "screen-release-ui-positive";

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
                    Requires = "Screenplay v4.96.0 typed screen authoring syntax",
                    PendingReason = null,
                    ExpectedSyntaxKinds =
                    [
                        "ScreenComponentSyntax",
                        "ScreenToolbarSyntax",
                        "UiBindingSyntax",
                        "UiProfileSyntax"
                    ]
                }
            ],
            BehaviorExpectations =
            [
                Behavior(
                    "selection-details",
                    "browser",
                    "The folder source renders WorkItemList through the MasterDetail template with two rows and no selected item.",
                    "Select the row with workItemId 3fa85f64-5717-4562-b3fc-2c963f66afa6 by pointer or keyboard activation.",
                    "The selected identity becomes the route parameter, GetWorkItem is invoked with that identity and the details slot renders WorkItemDetails.",
                    [
                        Assertion("source.form", "equals", FolderSourceCase),
                        Assertion("screens.WorkItemList.template", "equals", "MasterDetail"),
                        Assertion("screens.WorkItemList.list.query", "equals", "AllWorkItems"),
                        Assertion("screens.WorkItemList.list.table.columns", "equals", "title,status"),
                        Assertion("screens.WorkItemList.list.rowClick", "equals", "navigate WorkItemDetails by workItemId"),
                        Assertion("selection.workItemId", "equals", "3fa85f64-5717-4562-b3fc-2c963f66afa6"),
                        Assertion("routes.parameters.workItemId", "equals", "3fa85f64-5717-4562-b3fc-2c963f66afa6"),
                        Assertion("queries.GetWorkItem.arguments.workItemId", "equals", "3fa85f64-5717-4562-b3fc-2c963f66afa6"),
                        Assertion("outlets.details.screen", "equals", "WorkItemDetails")
                    ]),
                Behavior(
                    "query-rebind-clear",
                    "browser",
                    "WorkItemDetails is showing an existing selection from the folder source.",
                    "Navigate back to WorkItemList without a selected workItemId.",
                    "The details query is not invoked with a stale workItemId, the comments query is cleared, and the empty master/detail outlet is shown.",
                    [
                        Assertion("source.form", "equals", FolderSourceCase),
                        Assertion("selection.workItemId", "isNull", string.Empty),
                        Assertion("queries.GetWorkItem.calls", "equals", "0"),
                        Assertion("queries.GetWorkItem.staleArguments", "equals", "0"),
                        Assertion("queries.CommentsForWorkItem.calls", "equals", "0"),
                        Assertion("outlets.details.screen", "equals", "WorkItemList.emptyState"),
                        Assertion("sections.emptyState.title", "equals", "Select a work item")
                    ]),
                Behavior(
                    "native-form-validation-submit",
                    "browser",
                    "CreateWorkItemForm from the folder source is opened from the WorkItemList empty-state action.",
                    "Submit once with an empty title, then with title 'Design master detail' and a generated workItemId.",
                    "Native command-form validation blocks the invalid submit and the valid submit executes CreateWorkItem once before following the authored navigation.",
                    [
                        Assertion("source.form", "equals", FolderSourceCase),
                        Assertion("forms.CreateWorkItemForm.command", "equals", "CreateWorkItem"),
                        Assertion("forms.CreateWorkItemForm.fields", "equals", "workItemId,title"),
                        Assertion("forms.CreateWorkItemForm.submitNavigation", "equals", "WorkItemList by workItemId"),
                        Assertion("forms.CreateWorkItem.invalidSubmits", "equals", "1"),
                        Assertion("commands.CreateWorkItem.executions", "equals", "1"),
                        Assertion("commands.CreateWorkItem.arguments.title", "equals", "Design master detail"),
                        Assertion("navigation.currentScreen", "equals", "WorkItemList")
                    ]),
                Behavior(
                    "dialog-outlet-deep-link",
                    "browser",
                    "WorkItemDetails and CommentThread are opened from a deep link route with workItemId set.",
                    "Open the rename action, open the comments dialog, save, and follow the details deep link.",
                    "Dialog and outlet routing use the authored templates, preserve the route parameter and refresh CommentsForWorkItem after the dialog result.",
                    [
                        Assertion("source.form", "equals", FolderSourceCase),
                        Assertion("dialogs.EditDialog.slots", "equals", "body,actions"),
                        Assertion("forms.RenameWorkItemForm.populateFrom", "equals", "item"),
                        Assertion("forms.RenameWorkItemForm.submitNavigation", "equals", "WorkItemDetails by workItemId"),
                        Assertion("screens.CommentThread.dialog", "equals", "EditDialog"),
                        Assertion("dialogs.EditDialog.with.workItemId", "equals", "item.workItemId"),
                        Assertion("dialogs.EditDialog.onResult", "equals", "refresh CommentsForWorkItem"),
                        Assertion("routes.current", "equals", "work-items/{workItemId}"),
                        Assertion("routes.parameters.workItemId", "equals", "3fa85f64-5717-4562-b3fc-2c963f66afa6")
                    ]),
                Behavior(
                    "package-rendering",
                    "stage",
                    "The folder source's Desktop ui profile targets web with AppShell and core plus Cratis.Components packages.",
                    "Render the MasterDetail screen, tables, forms, topbar, navigation and content slots.",
                    "The host resolves the package-backed controls from the authored profile and produces no placeholder fallbacks or missing-icon fallbacks.",
                    [
                        Assertion("source.form", "equals", FolderSourceCase),
                        Assertion("uiProfiles.Desktop.platform", "equals", "web"),
                        Assertion("uiProfiles.Desktop.size", "equals", "expanded"),
                        Assertion("uiProfiles.Desktop.layout", "equals", "AppShell"),
                        Assertion("uiProfiles.Desktop.packages", "equals", "core,Cratis.Components"),
                        Assertion("layouts.AppShell.slots", "equals", "topbar,navigation,content"),
                        Assertion("layouts.MasterDetail.slots", "equals", "list,details"),
                        Assertion("icons.authored.count", "equals", "0"),
                        Assertion("fallbacks.placeholderComponents", "equals", "0"),
                        Assertion("fallbacks.missingIcons", "equals", "0")
                    ]),
                Behavior(
                    "browser-control-behavior",
                    "browser",
                    "The folder source is rendered in an expanded web profile with navigation, list and details controls visible.",
                    "Use keyboard focus to activate the Navigation contribution, select a table row, open and close the EditDialog, then use browser back.",
                    "Control state follows authored navigation and focus returns to the details summary without losing the selected workItemId.",
                    [
                        Assertion("source.form", "equals", FolderSourceCase),
                        Assertion("navigation.Navigation.items", "contains", "Work items"),
                        Assertion("navigation.Navigation.order.WorkItemList", "equals", "10"),
                        Assertion("controls.table.rowActivation.keyboard", "equals", "navigate WorkItemDetails by workItemId"),
                        Assertion("controls.dialog.escapeCloses", "equals", "true"),
                        Assertion("controls.focus.afterDialogClose", "equals", "WorkItemDetails.summary"),
                        Assertion("browser.history.back", "equals", "WorkItemList"),
                        Assertion("browser.history.forward", "equals", "WorkItemDetails")
                    ]),
                Behavior(
                    "protected-business-semantics",
                    "compiler",
                    "The UI corpus is compiled for backend ESM admission.",
                    "Compile the current source forms.",
                    "The compiler fails closed with pinned UI/query diagnostics and produces no artifact paths.",
                    [
                        Assertion("source.form", "equals", FolderSourceCase),
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
                        Assertion("source.case", "equals", TypedScreenSourceCase),
                        Assertion("proposal.validation", "equals", "Authoring"),
                        Assertion("binding.invalidRawTextPreserved", "equals", "true"),
                        Assertion("identities.changed", "equals", "0")
                    ]),
                Mcp(
                    "mcp-edit-dialog-action",
                    "add dialog toolbar submit action",
                    "screen RenameWorkItemDialog",
                    [
                        Assertion("source.case", "equals", TypedScreenSourceCase),
                        Assertion("proposal.droppedComments", "equals", "0"),
                        Assertion("target.template", "equals", "EditWorkItemDialog"),
                        Assertion("source.roundTrips", "equals", "true")
                    ]),
                Mcp(
                    "mcp-folder-master-detail-edit",
                    "open folder source form, add a harmless details-section action and round-trip the import graph",
                    "screen WorkItemDetails",
                    [
                        Assertion("source.form", "equals", FolderSourceCase),
                        Assertion("documents.stableKeys.count", "equals", "10"),
                        Assertion("imports.reachableFromRoot", "equals", "true"),
                        Assertion("proposal.validation", "equals", "Authoring"),
                        Assertion("screens.WorkItemDetails.template", "equals", "MasterDetail"),
                        Assertion("queries.GetWorkItem.binding", "equals", "workItemId"),
                        Assertion("identities.changed", "equals", "0")
                    ],
                    FolderSourceCase),
                Mcp(
                    "mcp-folder-dialog-routing-edit",
                    "open folder source form, edit CommentThread dialog routing and verify route/result bindings survive",
                    "screen CommentThread",
                    [
                        Assertion("source.form", "equals", FolderSourceCase),
                        Assertion("target.dialog", "equals", "EditDialog"),
                        Assertion("target.with.workItemId", "equals", "item.workItemId"),
                        Assertion("target.onResult", "equals", "refresh CommentsForWorkItem"),
                        Assertion("proposal.droppedComments", "equals", "0"),
                        Assertion("source.roundTrips", "equals", "true")
                    ],
                    FolderSourceCase)
            ],
            Harnesses =
            [
                Harness(
                    "browser-runtime",
                    "Scene browser",
                    "screenplay-conformance browser --vector screen-composition/v1 --source-form folder --profile Desktop",
                    "Scene browser harness for Stage/Scene native screens runtime",
                    "Docker server is available locally, but no released browser conformance runner/native screens runtime endpoint currently executes the folder corpus end to end.",
                    [
                        Assertion("source.form", "equals", FolderSourceCase),
                        Assertion("behaviors.selection-details", "passes", "true"),
                        Assertion("behaviors.query-rebind-clear", "passes", "true"),
                        Assertion("behaviors.native-form-validation-submit", "passes", "true"),
                        Assertion("behaviors.dialog-outlet-deep-link", "passes", "true"),
                        Assertion("behaviors.package-rendering", "passes", "true"),
                        Assertion("behaviors.browser-control-behavior", "passes", "true")
                    ]),
                Harness(
                    "mcp-authoring",
                    "Screenplay MCP",
                    "screenplay-conformance mcp --vector screen-composition/v1 --source-case screen-release-ui-positive",
                    "Screenplay >=4.96.0",
                    null,
                    [
                        Assertion("source.case", "equals", TypedScreenSourceCase),
                        Assertion("mcp.mcp-edit-component-binding", "passes", "true"),
                        Assertion("mcp.mcp-edit-dialog-action", "passes", "true")
                    ]),
                Harness(
                    "mcp-folder-authoring",
                    "Screenplay MCP",
                    "screenplay-conformance mcp --vector screen-composition/v1 --source-form folder",
                    "Screenplay MCP multi-document authoring harness for the folder corpus",
                    "The current repository executes the typed source MCP harness; the real folder import/edit/export MCP harness is authored here but not wired as a live spec yet.",
                    [
                        Assertion("source.form", "equals", FolderSourceCase),
                        Assertion("mcp.mcp-folder-master-detail-edit", "passes", "true"),
                        Assertion("mcp.mcp-folder-dialog-routing-edit", "passes", "true")
                    ]),
                Harness(
                    "cli-stage-parity",
                    "Cratis CLI and Stage",
                    "cratis render <folder-fixture> --target cratis --destination <out> --name Workspaces -y",
                    "Screenplay CLI with bundled compiler >= 4.94.0 + Stage screens ESM vertical",
                    "Executed on CLI 3.39.0 + Stage 4.43.0: planning this corpus publishes zero artifacts because the bundled compiler 4.93.0 cannot parse the screens authoring syntax and the renderer's ESM vertical refuses observable list queries (PLAY0268). Executed parity evidence exists for the legacy RegisterProject vector only; it is not a substitute for screens acceptance.",
                    [
                        Assertion("source.form", "equals", FolderSourceCase),
                        Assertion("stage.refusalCodes", "equals", "PLAY0025,PLAY0103,PLAY0207,PLAY0210,PLAY0029,PLAY0268"),
                        Assertion("stage.artifactPaths.count", "equals", "0"),
                        Assertion("sceneJson.matchesLiveStage", "equals", "true"),
                        Assertion("routes.commands.CreateWorkItem", "exists", "true"),
                        Assertion("routes.queries.GetWorkItem", "exists", "true"),
                        Assertion("routes.queries.CommentsForWorkItem", "exists", "true"),
                        Assertion("bindings.generated", "contains", "workItemId")
                    ]),
                Harness(
                    "studio-roundtrip",
                    "Studio",
                    "studio-conformance import-edit-export --vector screen-composition/v1 --source-form folder",
                    "Studio production authoring/import-edit-export automation entry point + workspace export format contract",
                    "Studio production authoring/import-edit-export automation entry point and workspace export format contract are not published yet.",
                    [
                        Assertion("source.form", "equals", FolderSourceCase),
                        Assertion("export.bytesRoundTrip", "equals", "true"),
                        Assertion("designer.masterDetailPreserved", "equals", "true"),
                        Assertion("designer.queryBindingsPreserved", "equals", "true"),
                        Assertion("designer.dialogRoutingPreserved", "equals", "true"),
                        Assertion("designer.packageProfilePreserved", "equals", "true"),
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
                    RequiredVersionVector = "Screenplay CLI whose bundled compiler is >= the screens authoring syntax release (4.94.0), plus Stage final screens release",
                    PendingReason = "Executed on CLI 3.39.0 + cratis/stage:4.43.0: the bundled compiler 4.93.0 predates the screens authoring syntax and rejects template category/type/exposes/outlet slots (PLAY0025), screen toolbar and component directives (PLAY0103), ui profile icons (PLAY0207), form columns (PLAY0210) and slice templates (PLAY0029); the pre-4.94 corpus form parses but planning refuses its observable list queries (PLAY0268) and publishes zero artifacts. Red vectors preserved; no expectation was regenerated to force green.",
                    Artifacts = [],
                    ObservedRefusalCodes = ["PLAY0025", "PLAY0103", "PLAY0207", "PLAY0210", "PLAY0029", "PLAY0268"],
                    Assertions =
                    [
                        Assertion("source.form", "equals", FolderSourceCase),
                        Assertion("plan.profile", "equals", "Desktop"),
                        Assertion("plan.scene.documents", "contains", "scene.json"),
                        Assertion("plan.layout", "contains", "AppShell"),
                        Assertion("plan.templates", "contains", "MasterDetail"),
                        Assertion("plan.routes.commands", "contains", "CreateWorkItem"),
                        Assertion("plan.routes.commands", "contains", "RenameWorkItem"),
                        Assertion("plan.routes.commands", "contains", "CloseWorkItem"),
                        Assertion("plan.routes.commands", "contains", "AddComment"),
                        Assertion("plan.routes.queries", "contains", "AllWorkItems"),
                        Assertion("plan.routes.queries", "contains", "GetWorkItem"),
                        Assertion("plan.routes.queries", "contains", "CommentsForWorkItem"),
                        Assertion("plan.nativeForms", "contains", "CreateWorkItemForm"),
                        Assertion("plan.nativeForms", "contains", "RenameWorkItemForm"),
                        Assertion("plan.navigation", "contains", "Work items"),
                        Assertion("plan.dialogs", "contains", "EditDialog"),
                        Assertion("plan.packages", "equals", "core,Cratis.Components"),
                        Assertion("plan.packageFallbacks", "equals", "0"),
                        Assertion("plan.missingIconFallbacks", "equals", "0")
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
        SourceCase = FolderSourceCase,
        Given = given,
        When = when,
        Then = then,
        Assertions = assertions
    };

    static CanonicalMcpEditExpectation Mcp(string name, string operation, string targetDeclaration, ImmutableArray<CanonicalScreenAssertion> assertions, string sourceCase = TypedScreenSourceCase) => new()
    {
        Name = name,
        SourceCase = sourceCase,
        Operation = operation,
        TargetDeclaration = targetDeclaration,
        Assertions = assertions
    };

    static CanonicalScreenHarnessExpectation Harness(string name, string host, string entryPoint, string requiredVersionVector, string? pendingReason, ImmutableArray<CanonicalScreenAssertion> assertions) => new()
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

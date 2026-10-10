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
                Probe("guarded-gesture", "Double click chooses one action list from rendered row status without fallback on denial or an absent subject.", "single", "screen WorkItemList", ["InteractionAlternativeSyntax", "InteractionOtherwiseSyntax"]),
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
                        "CommandFormLayoutSyntax",
                        "FormFieldPlacementSyntax",
                        "FormLayoutColumnSyntax",
                        "FormWidthSyntax",
                        "LiteralExpressionSyntax",
                        "ListExpressionSyntax",
                        "ObjectExpressionSyntax",
                        "ScreenToolbarSyntax",
                        "UiBindingSyntax",
                        "UiProfileSyntax",
                        "ExposureSyntax",
                        "ExposedPropertySyntax",
                        "InstanceContributionsSyntax",
                        "InstanceContributionSyntax",
                        "ContributedItemSyntax",
                        "ContributedItemValueSyntax",
                        "ScreenContributionSyntax",
                        "TemplateSlotContentSyntax",
                        "ArrangementContainerSyntax"
                    ]
                }
            ],
            RejectionVectors =
            [
                Rejection(
                    "incompatible-template-scope",
                    "IncompatibleTemplateScope",
                    Diagnostic("PLAY0631", "The screen template 'FeatureShell' cannot be used at slice scope - it allows feature")),
                Rejection(
                    "incompatible-component-package",
                    "IncompatibleComponentPackage",
                    Diagnostic("PLAY0632", "The component 'vendor.charts.Gauge' comes from package 'vendor.charts', which no ui profile declares - add it to a profile's 'packages'")),
                Rejection(
                    "incompatible-instance-value",
                    "IncompatibleInstanceValue",
                    Diagnostic("PLAY0628", "'shell:header.actions' is exposed as a collection - contribute 'items', not 'set'")),
                Rejection(
                    "missing-outlet",
                    "MissingOutlet",
                    Diagnostic("PLAY0630", "Unknown outlet 'details' - no layout, template or component declares 'outlet details'")),
                Rejection(
                    "missing-references",
                    "MissingReferences",
                    Diagnostic("PLAY0622", "Unknown exposure owner 'NoSuchTemplate' - an exposure belongs to a layout, screen template or dialog template"),
                    Diagnostic("PLAY0626", "Unknown instance 'NoSuchScreen' - instance values belong to a screen, screen template or dialog template"),
                    Diagnostic("PLAY0627", "'WorkItemList' stores a value for 'shell:header.subtitle', which no exposure exposes"),
                    Diagnostic("PLAY0629", "The screen template 'FeatureShell' provides content for slot 'footer', which it does not declare")),
                Rejection(
                    "re-exposure-cycle",
                    "ReExposureCycle",
                    Diagnostic("PLAY0623", "Re-exposing 'shell:header.title' from 'Outer' forms a cycle - a re-exposure must lead back to the owner that exposes it first"),
                    Diagnostic("PLAY0623", "Re-exposing 'shell:header.title' from 'Inner' forms a cycle - a re-exposure must lead back to the owner that exposes it first"))
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
            WorkingBranchHarnesses =
            [
                WorkingBranch(
                    "stage-pr260-screen-plan",
                    "Stage",
                    "Cratis/Stage",
                    "https://github.com/Cratis/Stage/pull/260",
                    "release/screens-stage-screenplay-4.105-20261009",
                    "cc73c85740c025951004711483ecd6fd2b1e0723",
                    "dotnet test Source/Rendering.Cratis/Rendering.Cratis.csproj --filter FullyQualifiedName~when_planning_the_screen_composition_corpus",
                    "Replace the branch checkout with the released Stage package/CLI once PR #260 publishes; keep these assertions and clear working-branch status only after the released render produces equivalent outcomes.",
                    "passed",
                    null,
                    [
                        Assertion("working.stage.specs.passed", "equals", "19"),
                        Assertion("backend.plan.diagnostics", "equals", "0"),
                        Assertion("backend.queries", "contains", "AllWorkItems"),
                        Assertion("backend.queries", "contains", "CommentsForWorkItem"),
                        Assertion("backend.queryArguments", "contains", "workItemId"),
                        Assertion("scene.folder.findings", "equals", "0"),
                        Assertion("scene.folder.navigation", "contains", "Navigation"),
                        Assertion("scene.typed.grid", "equals", "scene.web.DataGrid"),
                        Assertion("scene.typed.binding.selectedItem.kind", "equals", "ComponentProperty"),
                        Assertion("scene.typed.binding.selectedItem.nullBehavior", "equals", "Clear"),
                        Assertion("scene.typed.exposes", "contains", "selectedWorkItem"),
                        Assertion("scene.typed.toolbar.icons", "contains", "add")
                    ]),
                WorkingBranch(
                    "studio-pr1615-transport",
                    "Studio",
                    "Cratis/Studio",
                    "https://github.com/Cratis/Studio/pull/1615",
                    "release/screens-studio-transport-20261009",
                    "307243caf43571101ef06d0064c38ee82fb4ab92",
                    "dotnet test Source/Mcp/Mcp.csproj --filter FullyQualifiedName~Screenplay",
                    "Use the released Studio build that contains PR #1615's typed transport contract, then run the same MCP transport transcript against release artifacts.",
                    "passed",
                    null,
                    [
                        Assertion("working.studio.transport.specs.passed", "equals", "8"),
                        Assertion("transport.sceneDocument", "preserves", "typed metadata"),
                        Assertion("transport.fallbackOpaqueReferences", "refuses", "Play/static generation"),
                        Assertion("mcp.session.boundary", "equals", "one event model per session")
                    ]),
                WorkingBranch(
                    "studio-pr1617-native-play",
                    "Studio",
                    "Cratis/Studio",
                    "https://github.com/Cratis/Studio/pull/1617",
                    "release/screens-studio-native-nav-play-20261009",
                    "3600c8b432ec4a16bd58aafcbc323899e12d7275",
                    "dotnet test Source/Mcp/Mcp.csproj --filter FullyQualifiedName~Screenplay",
                    "Use the released Studio build that includes PR #1617, then run production save/export/import/Play over the folder source and require byte-preserving metadata round-trip.",
                    "passed-partial",
                    "The MCP session specs pass, but no production save/export/import/Play browser automation endpoint exists in this repository checkout for the full folder app.",
                    [
                        Assertion("working.studio.native.specs.passed", "equals", "8"),
                        Assertion("studio.production.save", "pending", "true"),
                        Assertion("studio.exportImport.roundTrip", "pending", "true"),
                        Assertion("studio.play.deepLinks", "pending", "true"),
                        Assertion("studio.nativeMetadata", "mustPreserve", "navigation,toolbar,dialog,outlets,bindings")
                    ]),
                WorkingBranch(
                    "scene-form-geometry",
                    "Scene",
                    "Cratis/Scene",
                    "local branch release/screens-scene-form-geometry-20261009 (no PR found)",
                    "release/screens-scene-form-geometry-20261009",
                    "5a612dc64b9254ee57779c3b8b7c98380307f9e5",
                    "dotnet test Source/DotNET/Model.Specs/Model.Specs.csproj --filter FullyQualifiedName~Form",
                    "Replace the local branch with the published Scene package that carries form geometry, then run the browser native-control harness against that package.",
                    "passed-partial",
                    "Model form-geometry specs pass, but native browser control execution is still blocked by the missing released Scene form-geometry package and browser harness endpoint.",
                    [
                        Assertion("working.scene.form.specs.passed", "equals", "9"),
                        Assertion("scene.form.geometry", "preserves", "columns,widths,field order"),
                        Assertion("browser.native.controls", "pending", "true"),
                        Assertion("browser.focus.lifecycle", "pending", "true")
                    ]),
                WorkingBranch(
                    "browser-native-controls",
                    "Browser",
                    "Cratis/Stage+Cratis/Scene",
                    "Stage PR #260 + Scene form-geometry branch",
                    "release/screens-stage-screenplay-4.105-20261009 + release/screens-scene-form-geometry-20261009",
                    "cc73c85740c025951004711483ecd6fd2b1e0723 + 5a612dc64b9254ee57779c3b8b7c98380307f9e5",
                    "screenplay-conformance browser --vector screen-composition/v1 --source-form folder --profile Desktop",
                    "Switch to released Stage, Scene and CLI packages, then remove the working-branch composite and run the same browser harness against package versions.",
                    "pending",
                    "Docker is available locally, but no released or working-branch browser conformance runner/native screens runtime endpoint is available to execute the folder corpus end to end.",
                    [
                        Assertion("browser.masterDetail.selection", "mustPass", "true"),
                        Assertion("browser.queryRebind.staleArguments", "equals", "0"),
                        Assertion("browser.nativeValidation.invalidSubmitBlocked", "equals", "1"),
                        Assertion("browser.dialog.escapeCloses", "equals", "true"),
                        Assertion("browser.deepLinks.workItemId", "preserves", "true"),
                        Assertion("browser.packageFallbacks", "equals", "0"),
                        Assertion("browser.missingIconFallbacks", "equals", "0")
                    ]),
                WorkingBranch(
                    "mcp-revision-edit-transcript",
                    "Screenplay MCP",
                    "Cratis/Screenplay",
                    "https://github.com/Cratis/Screenplay/pull/578",
                    "release/screens-esm-query-shapes-20261009",
                    "4b6c59bcb76cb4503771d4179a259bed017f0730",
                    "dotnet test Source/DotNET/Screenplay.Mcp/Screenplay.Mcp.csproj --filter FullyQualifiedName~when_authoring_screen_release_ui_from_an_empty_folder",
                    "Use the released Screenplay MCP package after the current branch publishes, then run the same transcript through Studio's production MCP bridge.",
                    "passed-typed-source-only",
                    "The typed-source MCP transcript passes in Screenplay; the multi-document folder revision edit transcript through Studio remains pending.",
                    [
                        Assertion("mcp.typedTranscript.specs.passed", "equals", "6"),
                        Assertion("mcp.revision.expectedRevision", "mustMatch", "true"),
                        Assertion("mcp.proposal.droppedComments", "equals", "0"),
                        Assertion("mcp.identityContinuity.changed", "equals", "0"),
                        Assertion("mcp.folderTranscript", "pending", "true")
                    ])
            ],
            ReleasedVectorResults =
            [
                ReleasedVector(
                    "cli-stage-render-3.40.2-4.49.2",
                    "Cratis CLI and Stage",
                    "CLI 3.40.2 (70cd950b2b465b06cdb85d9d0045b7150018cac9) + Stage 4.49.2 (0e1ac3eceff0c8d3b0c4193f7042c64830cdbf41, cratis/stage:4.49.2) + bundled Screenplay 4.105.0 (9f3738f6cea34fc594eed705d7bb694404f0cbbc) + Scene 4.12.0 (921720db4a9a6b20c7f45ad8820ea2858c398992) + corpus release Screenplay 4.111.0 (5bedf429b052a743df08b752dfe2bf8de5e07966)",
                    "cratis render Source/DotNET/Screenplay.CanonicalCorpus/Corpus/ScreenComposition/v1/source/folder --target cratis --destination .ai-work/stage-render-screen-r10/out --name Workspaces -y -o plain",
                    "passed-authored-scene-with-guarded-warnings",
                    "Render succeeds and emits the authored Scene composition surface. Two guarded-action/interaction warnings remain because pinned Scene packages cannot safely execute the guarded Close action and double-click interaction yet; browser-native behavior remains pending.",
                    [
                        Assertion("released.cli.version", "equals", "3.40.2"),
                        Assertion("released.stage.version", "equals", "4.49.2"),
                        Assertion("released.screenplay.bundle", "equals", "4.105.0"),
                        Assertion("released.screenplay.corpus", "equals", "4.111.0"),
                        Assertion("released.scene.version", "equals", "4.12.0"),
                        Assertion("render.exitCode", "equals", "0"),
                        Assertion("render.documents", "equals", "10"),
                        Assertion("render.artifacts", "equals", "37"),
                        Assertion("render.semanticRevision", "equals", "rev1:ba6161900f9462226e66e5f7276b8c4c02dee59d59d37d3685f93694ddabbf6b"),
                        Assertion("render.manifest.sha256", "equals", "affab2d33fe332f0bf02770f25d4851503be089df0606a496f5bb62cc65cac4b"),
                        Assertion("render.scene.sha256", "equals", "9faaceecf9d496d2561875b80733700593d159bdb162526a08b24d6e6cf63e82"),
                        Assertion("render.bindings.sha256", "equals", "b76082ae4439f1a177a6577a6d7fcf36db84d54d5c7839bc4b6409af4c07a302"),
                        Assertion("render.log.sha256", "equals", "1aa5d3dc837e162bba18b5ee6af5183a7eac78d7f19009570370b20049731fe8"),
                        Assertion("render.shaMismatches", "equals", "0"),
                        Assertion("render.warningCodes", "equals", "STAGE-SCENE-ACTION-001,STAGE-SCENE-INTERACTION-001"),
                        Assertion("render.warningCount", "equals", "2"),
                        Assertion("render.infoCodes", "equals", "PLAY0269"),
                        Assertion("render.infoCount", "equals", "11"),
                        Assertion("scene.screens", "equals", "CommentThread,WorkItemDetails,WorkItemList"),
                        Assertion("scene.layouts", "equals", "AppShell:topbar,navigation,content"),
                        Assertion("scene.screenTemplates", "equals", "MasterDetail:list,details"),
                        Assertion("scene.dialogTemplates", "equals", "EditDialog:body,actions"),
                        Assertion("scene.uiProfiles", "equals", "Desktop:web:expanded:AppShell:core,Cratis.Components"),
                        Assertion("scene.themes.count", "equals", "0"),
                        Assertion("scene.navigation.Navigation", "contains", "Work items"),
                        Assertion("scene.navigation.WorkItemList.order", "equals", "10"),
                        Assertion("scene.WorkItemList.data", "equals", "AllWorkItems"),
                        Assertion("scene.WorkItemList.table.navigation", "equals", "WorkItemDetails by workItemId"),
                        Assertion("scene.WorkItemDetails.data", "equals", "GetWorkItem by workItemId"),
                        Assertion("scene.WorkItemDetails.list.data", "equals", "AllWorkItems"),
                        Assertion("scene.CommentThread.data", "equals", "CommentsForWorkItem by workItemId"),
                        Assertion("bindings.commands", "equals", "AddComment,CloseWorkItem,CreateWorkItem,RenameWorkItem"),
                        Assertion("bindings.queries", "equals", "AllWorkItems,CommentsForWorkItem,GetWorkItem,WorkItemById"),
                        Assertion("guarded.Close", "warns", "STAGE-SCENE-ACTION-001"),
                        Assertion("guarded.WorkItemList.doubleClick", "warns", "STAGE-SCENE-INTERACTION-001")
                    ]),
                ReleasedVector(
                    "cratis-run-stage-4.49.1",
                    "Cratis CLI and Stage sandbox",
                    "CLI 3.40.1 + cratis/stage:4.49.1 + bundled Screenplay 4.105.0",
                    "cratis run Source/DotNET/Screenplay.CanonicalCorpus/Corpus/ScreenComposition/v1/source/folder --tag 4.49.1 --port 19092 --workbench-port 35092 --yes --verbose",
                    "passed-sandbox-started",
                    "The Stage sandbox starts and serves /index.html with HTTP 200, but no browser automation endpoint executes the authored folder-source full-app behavior. Browser pass remains pending.",
                    [
                        Assertion("run.stage.image", "equals", "cratis/stage:4.49.1"),
                        Assertion("run.ready", "equals", "true"),
                        Assertion("run.index.status", "equals", "200"),
                        Assertion("run.log.sha256", "equals", "8050916bdba879c18722b153e1f90a83b2bfa5d36b5b0b4f830c12c061f7d9ff"),
                        Assertion("browser.masterDetail.selection", "pending", "true"),
                        Assertion("browser.nativeValidation.submit", "pending", "true"),
                        Assertion("browser.dialog.deepLink", "pending", "true")
                    ]),
                ReleasedVector(
                    "browser-native-controls-runtime",
                    "Browser native controls",
                    "CLI 3.40.2 + cratis/stage:4.49.2 + bundled Screenplay 4.105.0 + Playwright Chromium 156.0.8078.4",
                    "node Source/DotNET/Screenplay.CanonicalVectors.Specs/BrowserHarness/screen-composition-browser-native-controls.cjs",
                    "partial-acceptance-blocked",
                    "The committed browser harness now seeds two distinct work items and comments, selects row B, verifies the Stage endpoints and list/detail identity that is currently visible, and records blockers for missing scoped comment query arguments, missing selected-id deep links, unobserved stale-response cancellation, disabled native command controls and non-applied dark theme styling. It also prepares invalid/valid native command assertions so a Stage 4.49.4+ rerun can prove command payloads, projection updates and reopen persistence instead of only checking enabled buttons.",
                    [
                        Assertion("browser.harness.implemented", "equals", "true"),
                        Assertion("browser.result.sha256", "equals", "f4c81e385d23408e10e2b0f031bb9a99f86d52002c0e327e1941ff8e4a40f828"),
                        Assertion("browser.runtime.shell", "loads", "true"),
                        Assertion("browser.endpoint./stage/routes", "equals", "200"),
                        Assertion("browser.endpoint./stage/scene", "equals", "200"),
                        Assertion("browser.endpoint./stage/locales", "equals", "200"),
                        Assertion("browser.seed.CreateWorkItem.A", "passes", "Design master detail"),
                        Assertion("browser.seed.CreateWorkItem.B", "passes", "Implement native browser forms"),
                        Assertion("browser.seed.AddComment.A", "passes", "Needs compact layout"),
                        Assertion("browser.seed.AddComment.B", "passes", "Validate selected work item only"),
                        Assertion("browser.navigation.WorkItemList", "passes", "true"),
                        Assertion("browser.query.AllWorkItems.titleA", "equals", "Design master detail"),
                        Assertion("browser.query.AllWorkItems.titleB", "equals", "Implement native browser forms"),
                        Assertion("browser.masterDetail.selection", "equals", "Clear selection"),
                        Assertion("browser.query.WorkItemDetails.titleB", "equals", "Implement native browser forms"),
                        Assertion("browser.query.CommentsForWorkItem.commentB", "blocked", "missing text: Validate selected work item only"),
                        Assertion("browser.query.CommentsForWorkItem.commentAAbsent", "passes", "true"),
                        Assertion("browser.queryArgs.selectedB", "blocked", "no API request carried selected row B identity"),
                        Assertion("browser.deepLink.selectedB", "blocked", "selected URL does not carry B identity"),
                        Assertion("browser.queryRebind.staleDelayedA.request", "blocked", "no delayed A query was observed"),
                        Assertion("browser.queryRebind.staleDelayedA.discard", "blocked", "delayed A response was not proven discarded"),
                        Assertion("browser.queryRebind.clearSelection.button", "passes", "true"),
                        Assertion("browser.queryRebind.clearSelection.route", "passes", "true"),
                        Assertion("browser.native.CreateWorkItem.control", "blocked-disabled", "This command is not exposed as an API yet"),
                        Assertion("browser.native.CreateWorkItem.invalidSubmit.zeroRequests", "pending", "button disabled"),
                        Assertion("browser.native.CreateWorkItem.validSubmit.payload", "pending", "button disabled"),
                        Assertion("browser.native.CreateWorkItem.validSubmit.projectionReopen", "pending", "button disabled"),
                        Assertion("browser.native.RenameWorkItem.control", "blocked-disabled", "This command is not exposed as an API yet"),
                        Assertion("browser.native.RenameWorkItem.validSubmit.payload", "pending", "button disabled"),
                        Assertion("browser.dialog.deepLink.lifecycle", "pending", "native command actions disabled"),
                        Assertion("browser.native.AddComment.control", "blocked-disabled", "This command is not exposed as an API yet"),
                        Assertion("browser.native.AddComment.validSubmit.payload", "pending", "button disabled"),
                        Assertion("browser.package.renderHost", "passes", "styleSheetCount=16;buttonCount=7"),
                        Assertion("browser.theme.dark.appliedStyle", "blocked", "body background did not change"),
                        Assertion("browser.nextStageTag", "prepared", "4.49.4"),
                        Assertion("browser.guarded.Close", "pending", "STAGE-SCENE-ACTION-001"),
                        Assertion("browser.guarded.doubleClick", "pending", "STAGE-SCENE-INTERACTION-001")
                    ]),
                ReleasedVector(
                    "browser-native-controls-runtime-3.40.3-4.49.5",
                    "Browser native controls",
                    "CLI 3.40.3 (b538d1bd7873b69a481f5bd4c66740b08445f126) + cratis/stage:4.49.5 + bundled Screenplay 4.105.0 + Playwright Chromium 156.0.8078.4",
                    "NODE_PATH=/Users/einari/.npm/_npx/e41f203b7505f1fb/node_modules SCREENPLAY_STAGE_TAG=4.49.5 SCREENPLAY_NEXT_STAGE_TAG=4.49.5 SCREENPLAY_BROWSER_PORT=19158 SCREENPLAY_BROWSER_WORKBENCH_PORT=35158 SCREENPLAY_BROWSER_RESULT=.ai-work/browser-native-controls-r14d-stage4495.json node Source/DotNET/Screenplay.CanonicalVectors.Specs/BrowserHarness/screen-composition-browser-native-controls.cjs",
                    "partial-acceptance-blocked",
                    "The exact public CLI 3.40.3 and Stage 4.49.5 rerun enables native command controls, but still fails acceptance: selecting row B does not produce scoped comment query data, selected query requests do not carry the B identity, the selected URL does not carry the B identity, stale A response discard is not observed, invalid CreateWorkItem submit sends one empty command request instead of zero, native form fields are missing, and dark theme style application is not visible.",
                    [
                        Assertion("released.cli.version", "equals", "3.40.3"),
                        Assertion("released.cli.commit", "equals", "b538d1bd7873b69a481f5bd4c66740b08445f126"),
                        Assertion("released.stage.version", "equals", "4.49.5"),
                        Assertion("browser.result.sha256", "equals", "cd5f972382557e3b5cac8f96d545e4bc1ffb42821c6d5357dfca7c75fef90240"),
                        Assertion("browser.endpoint./stage/routes", "equals", "200"),
                        Assertion("browser.endpoint./stage/scene", "equals", "200"),
                        Assertion("browser.endpoint./stage/locales", "equals", "200"),
                        Assertion("browser.seed.CreateWorkItem.A", "passes", "Design master detail"),
                        Assertion("browser.seed.CreateWorkItem.B", "passes", "Implement native browser forms"),
                        Assertion("browser.seed.AddComment.A", "passes", "Needs compact layout"),
                        Assertion("browser.seed.AddComment.B", "passes", "Validate selected work item only"),
                        Assertion("browser.query.AllWorkItems.titleA", "equals", "Design master detail"),
                        Assertion("browser.query.AllWorkItems.titleB", "equals", "Implement native browser forms"),
                        Assertion("browser.query.WorkItemDetails.titleB", "equals", "Implement native browser forms"),
                        Assertion("browser.query.CommentsForWorkItem.commentB", "blocked", "missing text: Validate selected work item only"),
                        Assertion("browser.query.CommentsForWorkItem.commentAAbsent", "passes", "true"),
                        Assertion("browser.queryArgs.selectedB", "blocked", "no API request carried selected row B identity"),
                        Assertion("browser.deepLink.selectedB", "blocked", "selected URL does not carry B identity"),
                        Assertion("browser.queryRebind.staleDelayedA.request", "blocked", "no delayed A query was observed"),
                        Assertion("browser.queryRebind.staleDelayedA.discard", "blocked", "delayed A response was not proven discarded"),
                        Assertion("browser.queryRebind.clearSelection.route", "passes", "true"),
                        Assertion("browser.native.CreateWorkItem.control", "passes", "enabled"),
                        Assertion("browser.native.CreateWorkItem.invalidSubmit.zeroRequests", "blocked", "expected zero command requests, saw 1"),
                        Assertion("browser.native.CreateWorkItem.invalidSubmit.payload", "equals", "POST /api/workspaces/tracking/create-work-item {}"),
                        Assertion("browser.native.CreateWorkItem.invalidSubmit.response", "equals", "500"),
                        Assertion("browser.native.CreateWorkItem.invalidSubmit.validation", "blocked", "missing text: required"),
                        Assertion("browser.native.CreateWorkItem.fields", "blocked", "workItemId,title missing"),
                        Assertion("browser.native.RenameWorkItem.control", "passes", "enabled"),
                        Assertion("browser.native.RenameWorkItem.fields", "blocked", "title missing"),
                        Assertion("browser.native.AddComment.control", "passes", "enabled"),
                        Assertion("browser.native.AddComment.fields", "blocked", "commentId,text missing"),
                        Assertion("browser.package.renderHost", "passes", "styleSheetCount=18;buttonCount=7"),
                        Assertion("browser.theme.dark.appliedStyle", "blocked", "body background did not change"),
                        Assertion("browser.guarded.Close", "pending", "STAGE-SCENE-ACTION-001"),
                        Assertion("browser.guarded.doubleClick", "pending", "STAGE-SCENE-INTERACTION-001")
                    ]),
                ReleasedVector(
                    "browser-native-controls-runtime-3.40.4-4.49.7",
                    "Browser native controls",
                    "CLI 3.40.4 (89e98f0bc43adbbcde255d3fda36571d24b859d1) + cratis/stage:4.49.7 + bundled Screenplay 4.105.0 + Playwright Chromium 156.0.8078.4",
                    "NODE_PATH=/Users/einari/.npm/_npx/e41f203b7505f1fb/node_modules SCREENPLAY_STAGE_TAG=4.49.7 SCREENPLAY_NEXT_STAGE_TAG=4.49.7 SCREENPLAY_BROWSER_PORT=19168 SCREENPLAY_BROWSER_WORKBENCH_PORT=35168 SCREENPLAY_BROWSER_RESULT=.ai-work/browser-native-controls-r16b-stage4497.json node Source/DotNET/Screenplay.CanonicalVectors.Specs/BrowserHarness/screen-composition-browser-native-controls.cjs",
                    "partial-acceptance-blocked",
                    "The exact public CLI 3.40.4 and Stage 4.49.7 rerun preserves the remaining scoped selection/theme blockers and improves native form behavior only partially: native controls are enabled and invalid CreateWorkItem is client-blocked with zero command requests, but required validation text is absent, expected form fields are still missing, valid native submit/persistence cannot run, B comments are missing, selected query requests and deep links do not carry the B identity, stale A response discard is not observed, and dark theme style application is not visible. /stage/scene now preserves punctuation and dotted stable ids in the emitted Scene payload.",
                    [
                        Assertion("released.cli.version", "equals", "3.40.4"),
                        Assertion("released.cli.commit", "equals", "89e98f0bc43adbbcde255d3fda36571d24b859d1"),
                        Assertion("released.stage.version", "equals", "4.49.7"),
                        Assertion("browser.result.sha256", "equals", "bd486f1ff007e73319a930d33a8cc5f07dce66b195b844dbaf225f77d4e39857"),
                        Assertion("browser.endpoint./stage/routes", "equals", "200"),
                        Assertion("browser.endpoint./stage/scene", "equals", "200"),
                        Assertion("browser.endpoint./stage/locales", "equals", "200"),
                        Assertion("browser.scene.identifierFidelity.punctuation", "passes", "true"),
                        Assertion("browser.scene.identifierFidelity.dotted", "passes", "true"),
                        Assertion("browser.seed.CreateWorkItem.A", "passes", "Design master detail"),
                        Assertion("browser.seed.CreateWorkItem.B", "passes", "Implement native browser forms"),
                        Assertion("browser.seed.AddComment.A", "passes", "Needs compact layout"),
                        Assertion("browser.seed.AddComment.B", "passes", "Validate selected work item only"),
                        Assertion("browser.query.AllWorkItems.titleA", "equals", "Design master detail"),
                        Assertion("browser.query.AllWorkItems.titleB", "equals", "Implement native browser forms"),
                        Assertion("browser.query.WorkItemDetails.titleB", "equals", "Implement native browser forms"),
                        Assertion("browser.query.CommentsForWorkItem.commentB", "blocked", "missing text: Validate selected work item only"),
                        Assertion("browser.query.CommentsForWorkItem.commentAAbsent", "passes", "true"),
                        Assertion("browser.queryArgs.selectedB", "blocked", "no API request carried selected row B identity"),
                        Assertion("browser.deepLink.selectedB", "blocked", "selected URL does not carry B identity"),
                        Assertion("browser.queryRebind.staleDelayedA.request", "blocked", "no delayed A query was observed"),
                        Assertion("browser.queryRebind.staleDelayedA.discard", "blocked", "delayed A response was not proven discarded"),
                        Assertion("browser.queryRebind.clearSelection.route", "passes", "true"),
                        Assertion("browser.native.CreateWorkItem.control", "passes", "enabled"),
                        Assertion("browser.native.CreateWorkItem.invalidSubmit.zeroRequests", "passes", "true"),
                        Assertion("browser.native.CreateWorkItem.invalidSubmit.validation", "blocked", "missing text: required"),
                        Assertion("browser.native.CreateWorkItem.fields", "blocked", "workItemId,title missing"),
                        Assertion("browser.native.RenameWorkItem.control", "passes", "enabled"),
                        Assertion("browser.native.RenameWorkItem.fields", "blocked", "title missing"),
                        Assertion("browser.native.AddComment.control", "passes", "enabled"),
                        Assertion("browser.native.AddComment.fields", "blocked", "commentId,text missing"),
                        Assertion("browser.native.commandRequests", "equals", "0"),
                        Assertion("browser.package.renderHost", "passes", "styleSheetCount=18;buttonCount=7"),
                        Assertion("browser.theme.dark.appliedStyle", "blocked", "body background did not change"),
                        Assertion("browser.guarded.Close", "pending", "STAGE-SCENE-ACTION-001"),
                        Assertion("browser.guarded.doubleClick", "pending", "STAGE-SCENE-INTERACTION-001")
                    ]),
                ReleasedVector(
                    "browser-native-controls-runtime-3.40.7-4.51.1",
                    "Browser native controls",
                    "CLI 3.40.7 (af9f18e2e5632e9153af1c6a5208bba7a5398951, public Homebrew formula) + cratis/stage:4.51.1 (CLI default) + bundled Screenplay 4.114.0",
                    "SCREENPLAY_EXPECTED_CLI_VERSION=3.40.7 SCREENPLAY_EXPECTED_STAGE_TAG=4.51.1 Source/DotNET/Screenplay.CanonicalVectors.Specs/BrowserHarness/run-public-acceptance.sh",
                    "passed",
                    "The first exact public vector on which every browser assertion passes: 51 passed, none blocked or pending. The work item list shows both seeded items, selecting row B narrows the comment query to B's identity and shows only B's comment, the URL deep-links B, a delayed response for row A is observed and discarded, and clearing the selection clears the comments and the route identity. Create, Rename and AddComment each refuse an invalid submit with zero requests and required-field validation, then send one canonical payload, update the projection and persist after reload. The dark theme changes the page background. Guarded Close and the double-click interaction remain fail-closed, which this harness does not exercise.",
                    [
                        Assertion("released.cli.version", "equals", "3.40.7"),
                        Assertion("released.cli.commit", "equals", "af9f18e2e5632e9153af1c6a5208bba7a5398951"),
                        Assertion("released.stage.version", "equals", "4.51.1"),
                        Assertion("browser.result.sha256", "equals", "8ecb9aecdb890e9b895ad7259efbd28ff71c4711f6a5babac96ec249d1a66d98"),
                        Assertion("browser.assertions.passed", "equals", "51"),
                        Assertion("browser.assertions.blocked", "equals", "0"),
                        Assertion("browser.scene.identifierFidelity.dotted", "passes", "true"),
                        Assertion("browser.query.AllWorkItems.titleA", "equals", "Design master detail"),
                        Assertion("browser.query.AllWorkItems.titleB", "equals", "Implement native browser forms"),
                        Assertion("browser.query.CommentsForWorkItem.commentB", "equals", "Validate selected work item only"),
                        Assertion("browser.query.CommentsForWorkItem.commentAAbsent", "passes", "true"),
                        Assertion("browser.queryArgs.selectedB", "equals", "comments-for-work-item?workItemId=22222222-2222-2222-2222-222222222222"),
                        Assertion("browser.deepLink.selectedB", "equals", "#/WorkItemDetails?workItemId=22222222-2222-2222-2222-222222222222"),
                        Assertion("browser.queryRebind.staleDelayedA.request", "equals", "1"),
                        Assertion("browser.queryRebind.staleDelayedA.discard", "passes", "true"),
                        Assertion("browser.queryRebind.clearSelection.commentBAbsent", "passes", "true"),
                        Assertion("browser.queryRebind.clearSelection.route", "equals", "#/WorkItemDetails"),
                        Assertion("browser.navigation.ui.WorkItemList", "equals", "navigation link"),
                        Assertion("browser.native.CreateWorkItem.invalidSubmit.zeroRequests", "passes", "true"),
                        Assertion("browser.native.CreateWorkItem.validSubmit.payload", "equals", "{\"workItemId\":\"33333333-3333-3333-3333-333333333333\",\"title\":\"Browser-created work item\"}"),
                        Assertion("browser.native.CreateWorkItem.validSubmit.projectionReopen", "passes", "true"),
                        Assertion("browser.native.RenameWorkItem.validSubmit.payload", "equals", "{\"workItemId\":\"22222222-2222-2222-2222-222222222222\",\"title\":\"Renamed selected B in browser\"}"),
                        Assertion("browser.native.RenameWorkItem.validSubmit.projection", "passes", "true"),
                        Assertion("browser.native.AddComment.invalidSubmit.zeroRequests", "passes", "true"),
                        Assertion("browser.native.AddComment.validSubmit.payload", "equals", "{\"commentId\":\"44444444-4444-4444-4444-444444444444\",\"workItemId\":\"22222222-2222-2222-2222-222222222222\",\"text\":\"Added through native browser form\"}"),
                        Assertion("browser.native.AddComment.validSubmit.reopen", "passes", "true"),
                        Assertion("browser.theme.dark.appliedStyle", "equals", "rgb(244, 245, 247) -> rgb(15, 17, 23)"),
                        Assertion("browser.guarded.Close", "pending", "STAGE-SCENE-ACTION-001"),
                        Assertion("browser.guarded.doubleClick", "pending", "STAGE-SCENE-INTERACTION-001")
                    ]),
                ReleasedVector(
                    "mcp-released-typed-transcript",
                    "Screenplay MCP",
                    "AI #563 merged a real stdio JSON-RPC transcript harness; conformance reran it with packaged public CLI 3.40.7 against the canonical screen corpus.",
                    "SCREENPLAY_AI_VERIFICATION=<Cratis/AI>/Source/Verification Source/DotNET/Screenplay.CanonicalVectors.Specs/BrowserHarness/run-public-acceptance.sh",
                    "passed-stdio-transcript-public-cli-3.40.7",
                    "The real AI-owned stdio MCP transcript initializes the packaged CLI MCP server, lists tools, opens a scratch corpus, proposes a source edit, proves stale apply fails closed with StaleRevision, applies with the correct revision, reopens, observes revision change and preserves a source comment. The public CLI 3.40.7 run returned 9 requests/responses, 37 tools and executableReady true.",
                    [
                        Assertion("mcp.typedTranscript.specs.passed", "equals", "6"),
                        Assertion("mcp.ai563.issue", "equals", "Cratis/AI#563"),
                        Assertion("mcp.stdioTranscript.implemented", "equals", "Source/Verification/screenplay-mcp-transcript.ts"),
                        Assertion("mcp.stdioTranscript.cli.version", "equals", "3.40.7"),
                        Assertion("mcp.stdioTranscript.requests", "equals", "9"),
                        Assertion("mcp.stdioTranscript.responses", "equals", "9"),
                        Assertion("mcp.stdioTranscript.tools", "equals", "37"),
                        Assertion("mcp.stdioTranscript.openWorkspace", "equals", "sourceSuccess=true;semanticSuccess=true;executableReady=true"),
                        Assertion("mcp.stdioTranscript.diagnostics", "equals", "11"),
                        Assertion("mcp.stdioTranscript.staleApply", "equals", "StaleRevision"),
                        Assertion("mcp.stdioTranscript.apply", "passes", "true"),
                        Assertion("mcp.stdioTranscript.revisionChanged", "passes", "true"),
                        Assertion("mcp.stdioTranscript.commentPreserved", "passes", "true")
                    ]),
                ReleasedVector(
                    "studio-0.136.6-production-play",
                    "Studio",
                    "Studio 0.136.6 includes Studio PR #1628 production Play automation selectors and contract; final conformance execution remains pending on production deploy/Core health.",
                    "NODE_PATH=/Users/einari/.npm/_npx/e41f203b7505f1fb/node_modules SCREENPLAY_STUDIO_RESULT=.ai-work/studio-production-play-r15.json node Source/DotNET/Screenplay.CanonicalVectors.Specs/BrowserHarness/screen-composition-studio-production-play.cjs",
                    "blocked-production-core-health",
                    "Studio PR #1628 merged and release v0.136.6 is public. The automation contract covers save/reopen, JSON export/import, Play start/stop/restart, Frontend/API/Chronicle view switching and failed-start diagnostics. Final conformance execution remains blocked until production Studio deploy/Core health is available; the harness must not report pass from local or Storybook-only routes.",
                    [
                        Assertion("studio.release.version", "equals", "0.136.6"),
                        Assertion("studio.productionAutomation.pr", "equals", "Cratis/Studio#1628"),
                        Assertion("studio.productionAutomation.commit", "equals", "7938d8446d6e2384ce505ab9ced858d6b93846c1"),
                        Assertion("studio.harness.implemented", "equals", "true"),
                        Assertion("studio.harness.operations", "equals", "save,reopen,export,import,play,start,stop,restart,views,failed-start"),
                        Assertion("studio.productionDeploy", "blocked", "Core health / deploy availability"),
                        Assertion("studio.saveExportImport", "pending", "SCREENPLAY_STUDIO_URL or Studio production target variables"),
                        Assertion("studio.play.deepLinks", "pending", "Studio production Play route")
                    ])
            ],
            StagePlans =
            [
                new CanonicalStagePlanExpectation
                {
                    Target = "scene-web",
                    Profile = "Web",
                    PlanDigest = "released:scene-sha256:9faaceecf9d496d2561875b80733700593d159bdb162526a08b24d6e6cf63e82",
                    RequiredVersionVector = "CLI 3.40.2 + Stage 4.49.2 + bundled Screenplay 4.105.0 + Scene 4.12.0",
                    PendingReason = "Executed on the exact released vector: CLI 3.40.2 + Stage 4.49.2 + bundled Screenplay 4.105.0 + Scene 4.12.0. Rendering succeeds and emits the authored Scene composition. Browser execution proves routes, two-item list rendering, selecting row B shows B details and clearing removes the selected route, but scoped comment query arguments, selected-id deep links, stale-response discard, native command submit, dialog/deep-link command flows, dark theme style application and guarded Close/double-click behavior remain pending or blocked by STAGE-SCENE-ACTION-001 and STAGE-SCENE-INTERACTION-001. The harness is prepared for a Stage 4.49.4+ rerun once stage-cli ships the command API exposure follow-up.",
                    Artifacts = [],
                    ObservedRefusalCodes = ["PLAY0269", "STAGE-SCENE-ACTION-001", "STAGE-SCENE-INTERACTION-001"],
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

    static CanonicalCorpusRejectionVector Rejection(string name, string file, params CanonicalCorpusDiagnosticExpectation[] diagnostics) => new()
    {
        Name = $"screen-composition/v1/negative/{name}",
        ApplicationName = "Workspaces",
        ApplicationIdentity = ApplicationIdentity.Parse("app1:20ccb167f2400bc55fae1597b1a0f4d19b40841f513bd013a7fa815e9e7f2994"),
        SourceForm = new CanonicalCorpusSourceForm
        {
            Name = "single",
            Documents = [Document(name, $"negative/{file}.play", $"{Prefix}.negative.{file}.play.txt")]
        },
        Diagnostics = [.. diagnostics],
        ArtifactPaths = []
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

    static CanonicalWorkingBranchHarnessExpectation WorkingBranch(string name, string host, string repository, string pullRequest, string branch, string commit, string entryPoint, string releaseSwitch, string status, string? pendingReason, ImmutableArray<CanonicalScreenAssertion> assertions) => new()
    {
        Name = name,
        Host = host,
        Repository = repository,
        PullRequest = pullRequest,
        Branch = branch,
        Commit = commit,
        EntryPoint = entryPoint,
        ReleaseSwitch = releaseSwitch,
        Status = status,
        PendingReason = pendingReason,
        Assertions = assertions
    };

    static CanonicalReleasedVectorHarnessResult ReleasedVector(string name, string host, string versionVector, string entryPoint, string status, string? pendingReason, ImmutableArray<CanonicalScreenAssertion> assertions) => new()
    {
        Name = name,
        Host = host,
        VersionVector = versionVector,
        EntryPoint = entryPoint,
        Status = status,
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

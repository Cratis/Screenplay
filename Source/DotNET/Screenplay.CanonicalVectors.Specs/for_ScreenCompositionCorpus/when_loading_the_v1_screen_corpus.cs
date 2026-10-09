// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_ScreenCompositionCorpus;

public class when_loading_the_v1_screen_corpus : Specification
{
    CanonicalScreenCorpusVector _corpus = null!;
    Diagnostic[] _diagnostics = [];

    void Because()
    {
        _corpus = ScreenCompositionCorpus.V1;
        _diagnostics = [.. CompileForExecutableModel(_corpus.SourceForms.Single(form => form.Name == "single"))];
    }

    [Fact] void should_publish_the_expected_source_forms() => _corpus.SourceForms.Select(form => form.Name).ShouldEqual("single", "folder", "reordered", "relocated");
    [Fact] void should_keep_the_corpus_identity() => _corpus.Name.ShouldEqual("screen-composition/v1");
    [Fact] void should_keep_the_application_identity() => _corpus.ApplicationIdentity.ToString().ShouldEqual("app1:20ccb167f2400bc55fae1597b1a0f4d19b40841f513bd013a7fa815e9e7f2994");
    [Fact] void should_keep_the_single_document_key() => _corpus.SourceForms[0].Documents.Single().StableKey.ShouldEqual("screen-workspaces-vector");
    [Fact] void should_keep_the_folder_document_keys() => _corpus.SourceForms[1].Documents.Select(document => document.StableKey).ShouldEqual("application", "workspaces-module", "workspaces-tracking-feature", "create-work-item-slice", "rename-work-item-slice", "close-work-item-slice", "add-comment-slice", "work-item-list-slice", "work-item-details-slice", "work-item-comments-slice");
    [Fact] void should_reverse_document_order_in_the_reordered_form() => _corpus.SourceForms[2].Documents.Select(document => document.StableKey).ShouldEqual(_corpus.SourceForms[1].Documents.Reverse().Select(document => document.StableKey));
    [Fact] void should_relocate_every_document_in_the_relocated_form() => _corpus.SourceForms[3].Documents.All(document => document.DisplayPath.StartsWith("Archive/", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_parse_every_source_document_without_authoring_diagnostics() => _corpus.SourceForms.SelectMany(form => form.Documents).All(document => Parse(document.Text).Success).ShouldBeTrue();
    [Fact] void should_pin_the_screen_behavior_probes() => _corpus.BehaviorProbes.Select(probe => probe.Name).ShouldEqual("master-detail-selection", "guarded-gesture", "query-rebind", "auto-manual-command-forms", "recursive-nested-hierarchy", "toolbar-dialog-url-surfaces", "package-icon-style-contract");
    [Fact] void should_publish_the_positive_typed_source_case() => _corpus.TypedSourceCases.Select(sourceCase => sourceCase.Name).ShouldEqual("screen-release-ui-positive");
    [Fact] void should_publish_real_behavior_expectations() => _corpus.BehaviorExpectations.Select(expectation => expectation.Name).ShouldEqual("selection-details", "query-rebind-clear", "native-form-validation-submit", "dialog-outlet-deep-link", "package-rendering", "browser-control-behavior", "protected-business-semantics");
    [Fact] void should_publish_mcp_edit_invariants() => _corpus.McpEditExpectations.Select(expectation => expectation.Name).ShouldEqual("mcp-edit-component-binding", "mcp-edit-dialog-action", "mcp-folder-master-detail-edit", "mcp-folder-dialog-routing-edit");
    [Fact] void should_publish_harness_entry_points() => _corpus.Harnesses.Select(harness => harness.Name).ShouldEqual("browser-runtime", "mcp-authoring", "mcp-folder-authoring", "cli-stage-parity", "studio-roundtrip");
    [Fact] void should_publish_working_branch_harness_entry_points() => _corpus.WorkingBranchHarnesses.Select(harness => harness.Name).ShouldEqual("stage-pr260-screen-plan", "studio-pr1615-transport", "studio-pr1617-native-play", "scene-form-geometry", "browser-native-controls", "mcp-revision-edit-transcript");
    [Fact] void should_publish_released_vector_results() => _corpus.ReleasedVectorResults.Select(result => result.Name).ShouldEqual("cli-stage-render-3.40.1-4.49.1", "cratis-run-stage-4.49.1", "browser-native-controls-runtime", "mcp-released-typed-transcript", "studio-0.136.3-production-play");
    [Fact] void should_publish_pending_stage_plan_assertions_without_claiming_artifact_bytes() => _corpus.StagePlans.Single().PendingReason.ShouldNotBeNull();

    [Fact]
    void should_fail_backend_esm_admission_with_the_expected_diagnostics()
    {
        ((string[])[.. _diagnostics.Select(diagnostic => diagnostic.Code)]).ShouldEqual([.. _corpus.EsmDiagnostics.Select(diagnostic => diagnostic.Code)]);
        ((string[])[.. _diagnostics.Select(diagnostic => diagnostic.Message)]).ShouldEqual([.. _corpus.EsmDiagnostics.Select(diagnostic => diagnostic.Message)]);
    }

    [Fact]
    void should_keep_every_source_form_at_the_same_backend_esm_boundary()
    {
        foreach (var form in _corpus.SourceForms)
        {
            var diagnostics = CompileForExecutableModel(form).ToArray();
            ((string[])[.. diagnostics.Select(diagnostic => diagnostic.Code).Order()]).ShouldEqual([.. _corpus.EsmDiagnostics.Select(diagnostic => diagnostic.Code).Order()]);
            ((string[])[.. diagnostics.Select(diagnostic => diagnostic.Message).Order()]).ShouldEqual([.. _corpus.EsmDiagnostics.Select(diagnostic => diagnostic.Message).Order()]);
        }
    }

    [Fact]
    void should_keep_every_probe_backed_by_authored_syntax()
    {
        var syntaxKinds = SyntaxKinds(_corpus.SourceForms.Single(form => form.Name == "single").Documents.Single().Text);
        foreach (var kind in _corpus.BehaviorProbes.SelectMany(probe => probe.SyntaxKinds).Distinct())
        {
            syntaxKinds.ShouldContain(kind);
        }
    }

    [Fact]
    void should_keep_the_positive_source_grounded_in_the_typed_screen_contract()
    {
        var source = _corpus.TypedSourceCases.Single();
        source.PendingReason.ShouldBeNull();
        source.Requires.ShouldEqual("Screenplay v4.96.0 typed screen authoring syntax");
        source.Document.Text.Contains("component scene.web.DataGrid workItems", StringComparison.Ordinal).ShouldBeTrue();
        source.Document.Text.Contains("property selectedItem from component workItems.selectedItem null clear", StringComparison.Ordinal).ShouldBeTrue();
        source.Document.Text.Contains("columns manual", StringComparison.Ordinal).ShouldBeTrue();
        source.Document.Text.Contains("icons", StringComparison.Ordinal).ShouldBeTrue();
        source.ExpectedSyntaxKinds.ShouldContain("ScreenComponentSyntax");
    }

    [Fact]
    void should_give_every_behavior_expectation_machine_checkable_assertions()
    {
        foreach (var expectation in _corpus.BehaviorExpectations)
        {
            expectation.SourceCase.ShouldEqual("screen-composition/v1/source/folder");
            expectation.Assertions.ShouldNotBeEmpty();
        }
    }

    [Fact]
    void should_give_every_mcp_edit_invariant_machine_checkable_assertions()
    {
        foreach (var expectation in _corpus.McpEditExpectations)
        {
            ((string[])["screen-release-ui-positive", "screen-composition/v1/source/folder"]).ShouldContain(expectation.SourceCase);
            expectation.Assertions.ShouldNotBeEmpty();
        }
    }

    [Fact]
    void should_give_every_harness_a_real_entry_point_and_assertions()
    {
        foreach (var harness in _corpus.Harnesses)
        {
            harness.EntryPoint.ShouldNotBeEmpty();
            harness.RequiredVersionVector.ShouldNotBeEmpty();
            harness.Assertions.ShouldNotBeEmpty();
        }
    }

    [Fact]
    void should_pin_folder_master_detail_query_and_native_form_assertions()
    {
        ShouldHaveBehaviorAssertions("selection-details", "source.form", "screens.WorkItemList.template", "screens.WorkItemList.list.query", "screens.WorkItemList.list.rowClick", "queries.GetWorkItem.arguments.workItemId", "outlets.details.screen");
        ShouldHaveBehaviorAssertions("query-rebind-clear", "source.form", "queries.GetWorkItem.staleArguments", "queries.CommentsForWorkItem.calls", "sections.emptyState.title");
        ShouldHaveBehaviorAssertions("native-form-validation-submit", "source.form", "forms.CreateWorkItemForm.command", "forms.CreateWorkItemForm.fields", "forms.CreateWorkItemForm.submitNavigation", "commands.CreateWorkItem.arguments.title");
    }

    [Fact]
    void should_pin_folder_navigation_dialog_package_and_browser_control_assertions()
    {
        ShouldHaveBehaviorAssertions("dialog-outlet-deep-link", "source.form", "dialogs.EditDialog.slots", "forms.RenameWorkItemForm.populateFrom", "screens.CommentThread.dialog", "dialogs.EditDialog.onResult", "routes.parameters.workItemId");
        ShouldHaveBehaviorAssertions("package-rendering", "source.form", "uiProfiles.Desktop.platform", "uiProfiles.Desktop.layout", "uiProfiles.Desktop.packages", "layouts.AppShell.slots", "layouts.MasterDetail.slots", "icons.authored.count", "fallbacks.placeholderComponents");
        ShouldHaveBehaviorAssertions("browser-control-behavior", "source.form", "navigation.Navigation.items", "controls.table.rowActivation.keyboard", "controls.dialog.escapeCloses", "controls.focus.afterDialogClose", "browser.history.back", "browser.history.forward");
    }

    [Fact]
    void should_pin_folder_mcp_and_harness_assertions()
    {
        ShouldHaveMcpAssertions("mcp-folder-master-detail-edit", "source.form", "documents.stableKeys.count", "imports.reachableFromRoot", "screens.WorkItemDetails.template", "queries.GetWorkItem.binding");
        ShouldHaveMcpAssertions("mcp-folder-dialog-routing-edit", "source.form", "target.dialog", "target.with.workItemId", "target.onResult", "source.roundTrips");
        ShouldHaveHarnessAssertions("browser-runtime", "source.form", "behaviors.selection-details", "behaviors.browser-control-behavior");
        ShouldHaveHarnessAssertions("mcp-folder-authoring", "source.form", "mcp.mcp-folder-master-detail-edit", "mcp.mcp-folder-dialog-routing-edit");
        ShouldHaveHarnessAssertions("studio-roundtrip", "source.form", "designer.masterDetailPreserved", "designer.dialogRoutingPreserved", "designer.packageProfilePreserved");
    }

    [Fact]
    void should_mark_only_browser_cli_and_studio_harnesses_pending_on_external_releases()
    {
        _corpus.Harnesses.Single(harness => harness.Name == "mcp-authoring").PendingReason.ShouldBeNull();
        _corpus.Harnesses.Where(harness => harness.Name != "mcp-authoring").All(harness => harness.PendingReason is not null).ShouldBeTrue();
    }

    [Fact]
    void should_pin_exact_working_branch_heads()
    {
        ShouldHaveWorkingBranch("stage-pr260-screen-plan", "https://github.com/Cratis/Stage/pull/260", "release/screens-stage-screenplay-4.105-20261009", "cc73c85740c025951004711483ecd6fd2b1e0723", "passed");
        ShouldHaveWorkingBranch("studio-pr1615-transport", "https://github.com/Cratis/Studio/pull/1615", "release/screens-studio-transport-20261009", "307243caf43571101ef06d0064c38ee82fb4ab92", "passed");
        ShouldHaveWorkingBranch("studio-pr1617-native-play", "https://github.com/Cratis/Studio/pull/1617", "release/screens-studio-native-nav-play-20261009", "3600c8b432ec4a16bd58aafcbc323899e12d7275", "passed-partial");
        ShouldHaveWorkingBranch("scene-form-geometry", "local branch release/screens-scene-form-geometry-20261009 (no PR found)", "release/screens-scene-form-geometry-20261009", "5a612dc64b9254ee57779c3b8b7c98380307f9e5", "passed-partial");
        ShouldHaveWorkingBranch("mcp-revision-edit-transcript", "https://github.com/Cratis/Screenplay/pull/578", "release/screens-esm-query-shapes-20261009", "4b6c59bcb76cb4503771d4179a259bed017f0730", "passed-typed-source-only");
    }

    [Fact]
    void should_pin_working_branch_full_app_assertions()
    {
        ShouldHaveWorkingBranchAssertions("stage-pr260-screen-plan", "backend.queries", "backend.queryArguments", "scene.folder.findings", "scene.folder.navigation", "scene.typed.binding.selectedItem.nullBehavior", "scene.typed.toolbar.icons");
        ShouldHaveWorkingBranchAssertions("studio-pr1617-native-play", "studio.production.save", "studio.exportImport.roundTrip", "studio.play.deepLinks", "studio.nativeMetadata");
        ShouldHaveWorkingBranchAssertions("browser-native-controls", "browser.masterDetail.selection", "browser.queryRebind.staleArguments", "browser.nativeValidation.invalidSubmitBlocked", "browser.dialog.escapeCloses", "browser.deepLinks.workItemId", "browser.packageFallbacks", "browser.missingIconFallbacks");
        ShouldHaveWorkingBranchAssertions("mcp-revision-edit-transcript", "mcp.revision.expectedRevision", "mcp.proposal.droppedComments", "mcp.identityContinuity.changed", "mcp.folderTranscript");
    }

    [Fact]
    void should_keep_stage_plan_pending_but_assertable()
    {
        var plan = _corpus.StagePlans.Single();
        plan.PlanDigest.StartsWith("released:scene-sha256:", StringComparison.Ordinal).ShouldBeTrue();
        plan.Artifacts.ShouldBeEmpty();
        plan.ObservedRefusalCodes.ShouldEqual("PLAY0269", "STAGE-SCENE-ACTION-001", "STAGE-SCENE-INTERACTION-001");
        plan.Assertions.Select(assertion => assertion.Path).ShouldContain("source.form");
        plan.Assertions.Select(assertion => assertion.Path).ShouldContain("plan.nativeForms");
        plan.Assertions.Select(assertion => assertion.Path).ShouldContain("plan.dialogs");
        plan.Assertions.Select(assertion => assertion.Path).ShouldContain("plan.packageFallbacks");
    }

    [Fact]
    void should_record_the_released_vector_stage_pending_warnings_in_the_pending_reason()
    {
        var plan = _corpus.StagePlans.Single();
        plan.PendingReason!.ShouldContain("CLI 3.40.1");
        plan.PendingReason.ShouldContain("Stage 4.49.1");
        plan.PendingReason.ShouldContain("authored Scene composition");
        plan.PendingReason.ShouldContain("STAGE-SCENE-ACTION-001");
    }

    [Fact]
    void should_pin_released_stage_render_results()
    {
        ShouldHaveReleasedVector("cli-stage-render-3.40.1-4.49.1", "passed-authored-scene-with-guarded-warnings", "released.cli.version", "released.stage.version", "released.screenplay.bundle", "released.screenplay.corpus", "render.artifacts", "render.semanticRevision", "render.shaMismatches", "render.warningCodes", "scene.screens", "scene.layouts", "scene.screenTemplates", "scene.dialogTemplates", "scene.uiProfiles", "scene.WorkItemList.data", "scene.WorkItemDetails.data", "scene.CommentThread.data", "bindings.queries", "guarded.Close", "guarded.WorkItemList.doubleClick");
    }

    [Fact]
    void should_pin_released_run_mcp_and_studio_results()
    {
        ShouldHaveReleasedVector("cratis-run-stage-4.49.1", "passed-sandbox-started", "run.stage.image", "run.ready", "run.index.status", "browser.masterDetail.selection", "browser.nativeValidation.submit", "browser.dialog.deepLink");
        ShouldHaveReleasedVector("browser-native-controls-runtime", "blocked-missing-stage-endpoints", "browser.harness.implemented", "browser.runtime.shell", "browser.endpoint./stage/routes", "browser.endpoint./stage/scene", "browser.endpoint./stage/locales", "browser.masterDetail.selection", "browser.queryLifecycle", "browser.nativeValidation.submit", "browser.dialog.deepLink", "browser.packageProfile.icons");
        ShouldHaveReleasedVector("mcp-released-typed-transcript", "passed-typed-source-only", "mcp.typedTranscript.specs.passed", "mcp.proposal.droppedComments", "mcp.identityContinuity.changed", "mcp.folderTranscript");
        ShouldHaveReleasedVector("studio-0.136.3-production-play", "blocked", "studio.release.version", "studio.release.commit", "studio.productionDeploy", "studio.saveExportImport", "studio.play.deepLinks");
    }

    [Fact]
    void should_parse_the_positive_typed_screen_source_without_authoring_diagnostics()
    {
        var sourceCase = _corpus.TypedSourceCases.Single();
        sourceCase.PendingReason.ShouldBeNull();
        var parsed = Parse(sourceCase.Document.Text);
        var syntaxKinds = SyntaxKinds(parsed.Value!);
        foreach (var kind in sourceCase.ExpectedSyntaxKinds)
        {
            syntaxKinds.ShouldContain(kind);
        }
    }

    [Fact]
    void should_admit_the_positive_typed_screen_source_shape()
    {
        var application = Parse(_corpus.TypedSourceCases.Single().Document.Text).Value!;
        var module = application.Modules.Single();
        var feature = module.Features.Single();
        var listSlice = feature.Slices.Single(slice => slice.Name == "WorkItemList");
        var detailSlice = feature.Slices.Single(slice => slice.Name == "WorkItemDetails");
        var dialogSlice = feature.Slices.Single(slice => slice.Name == "WorkItemDialog");
        application.Templates.Single().Name.ShouldEqual("ApplicationShell");
        module.Templates.Single().Name.ShouldEqual("WorkspaceFeatureShell");
        feature.Templates.Single().Name.ShouldEqual("WorkspaceFeatureShell");
        listSlice.Templates.Single().Name.ShouldEqual("WorkspaceFeatureShell");
        module.Forms.Single(form => form.Name == "RenameWorkItemForm").Columns.Single().Property.ShouldEqual("title");
        listSlice.Screens.Single().Directives.OfType<ScreenToolbarSyntax>().Single().Items.Select(item => item.Name).ShouldEqual("create", "refresh");
        listSlice.Screens.Single().Directives.OfType<ScreenComponentSyntax>().Single().Properties.Single(property => property.Property == "selectedItem").Binding!.NullBehavior.ShouldEqual(UiBindingNullBehavior.Clear);
        detailSlice.Screens.Single().Directives.OfType<ScreenComponentSyntax>().Single().Outlets.Single().Name.ShouldEqual("actions");
        dialogSlice.Screens.Single().Directives.OfType<ScreenTemplateReferenceSyntax>().Single().Name.ShouldEqual("EditWorkItemDialog");
    }

    void ShouldHaveBehaviorAssertions(string expectationName, params string[] paths)
    {
        var assertionPaths = _corpus.BehaviorExpectations.Single(expectation => expectation.Name == expectationName).Assertions.Select(assertion => assertion.Path);
        foreach (var path in paths)
        {
            assertionPaths.ShouldContain(path);
        }
    }

    void ShouldHaveMcpAssertions(string expectationName, params string[] paths)
    {
        var assertionPaths = _corpus.McpEditExpectations.Single(expectation => expectation.Name == expectationName).Assertions.Select(assertion => assertion.Path);
        foreach (var path in paths)
        {
            assertionPaths.ShouldContain(path);
        }
    }

    void ShouldHaveHarnessAssertions(string harnessName, params string[] paths)
    {
        var assertionPaths = _corpus.Harnesses.Single(harness => harness.Name == harnessName).Assertions.Select(assertion => assertion.Path);
        foreach (var path in paths)
        {
            assertionPaths.ShouldContain(path);
        }
    }

    void ShouldHaveWorkingBranch(string harnessName, string pullRequest, string branch, string commit, string status)
    {
        var harness = _corpus.WorkingBranchHarnesses.Single(harness => harness.Name == harnessName);
        harness.PullRequest.ShouldEqual(pullRequest);
        harness.Branch.ShouldEqual(branch);
        harness.Commit.ShouldEqual(commit);
        harness.Status.ShouldEqual(status);
        harness.ReleaseSwitch.ShouldNotBeEmpty();
    }

    void ShouldHaveWorkingBranchAssertions(string harnessName, params string[] paths)
    {
        var assertionPaths = _corpus.WorkingBranchHarnesses.Single(harness => harness.Name == harnessName).Assertions.Select(assertion => assertion.Path);
        foreach (var path in paths)
        {
            assertionPaths.ShouldContain(path);
        }
    }

    void ShouldHaveReleasedVector(string resultName, string status, params string[] paths)
    {
        var result = _corpus.ReleasedVectorResults.Single(result => result.Name == resultName);
        result.Status.ShouldEqual(status);
        result.EntryPoint.ShouldNotBeEmpty();
        result.VersionVector.ShouldNotBeEmpty();
        var assertionPaths = result.Assertions.Select(assertion => assertion.Path);
        foreach (var path in paths)
        {
            assertionPaths.ShouldContain(path);
        }
    }

    static IEnumerable<Diagnostic> CompileForExecutableModel(CanonicalCorpusSourceForm form)
    {
        var catalog = SemanticIdentityCatalogSerializer.Deserialize(form.IdentityCatalogBytes.AsSpan());
        var documents = form.Documents.Select(document => SemanticSourceDocument.Create(catalog.ResolveDocument(document.StableKey), document.StableKey, document.DisplayPath, document.Text));
        var result = new SemanticModelCompiler().Compile("Workspaces", SemanticDocumentSet.Create([.. documents], catalog));
        return result.Diagnostics;
    }

    static string[] SyntaxKinds(string source)
    {
        var parsed = Parse(source);
        return SyntaxKinds(parsed.Value!);
    }

    static string[] SyntaxKinds(ApplicationSyntax application)
    {
        var collector = new NodeKindCollector();
        collector.VisitApplication(application);
        return [.. collector.Kinds];
    }

    static CompilationResult<ApplicationSyntax> Parse(string source)
    {
        var parsed = new ScreenplayCompiler().Parse(source);
        if (!parsed.Success)
        {
            Assert.Fail(string.Join('\n', parsed.Diagnostics.Select(diagnostic => diagnostic.Message)));
        }
        return parsed;
    }

    sealed class NodeKindCollector : ScreenplaySyntaxWalker
    {
        public HashSet<string> Kinds { get; } = [];

        public override void VisitNode(SyntaxNode node) => Kinds.Add(node.GetType().Name);
    }
}

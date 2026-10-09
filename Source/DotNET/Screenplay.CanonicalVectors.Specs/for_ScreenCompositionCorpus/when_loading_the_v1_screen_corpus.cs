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
    void should_keep_stage_plan_pending_but_assertable()
    {
        var plan = _corpus.StagePlans.Single();
        plan.PlanDigest.StartsWith("pending:", StringComparison.Ordinal).ShouldBeTrue();
        plan.Artifacts.ShouldBeEmpty();
        plan.ObservedRefusalCodes.ShouldEqual("PLAY0025", "PLAY0103", "PLAY0207", "PLAY0210", "PLAY0029", "PLAY0268");
        plan.Assertions.Select(assertion => assertion.Path).ShouldContain("source.form");
        plan.Assertions.Select(assertion => assertion.Path).ShouldContain("plan.nativeForms");
        plan.Assertions.Select(assertion => assertion.Path).ShouldContain("plan.dialogs");
        plan.Assertions.Select(assertion => assertion.Path).ShouldContain("plan.packageFallbacks");
    }

    [Fact]
    void should_record_the_released_toolchain_refusals_in_the_pending_reason()
    {
        var plan = _corpus.StagePlans.Single();
        plan.PendingReason!.ShouldContain("CLI 3.39.0");
        plan.PendingReason.ShouldContain("4.43.0");
        plan.PendingReason.ShouldContain("zero artifacts");
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

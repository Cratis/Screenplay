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
    [Fact] void should_pin_the_screen_behavior_probes() => _corpus.BehaviorProbes.Select(probe => probe.Name).ShouldEqual("master-detail-selection", "query-rebind", "auto-manual-command-forms", "recursive-nested-hierarchy", "toolbar-dialog-url-surfaces", "package-icon-style-contract");
    [Fact] void should_leave_stage_artifact_bytes_unclaimed_until_stage_publishes_the_plan_schema() => _corpus.StagePlans.ShouldBeEmpty();

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

    static IEnumerable<Diagnostic> CompileForExecutableModel(CanonicalCorpusSourceForm form)
    {
        var catalog = SemanticIdentityCatalogSerializer.Deserialize(form.IdentityCatalogBytes.AsSpan());
        var documents = form.Documents.Select(document => SemanticSourceDocument.Create(catalog.ResolveDocument(document.StableKey), document.StableKey, document.DisplayPath, document.Text));
        var result = new SemanticModelCompiler().Compile("Workspaces", SemanticDocumentSet.Create([.. documents], catalog));
        result.Success.ShouldBeFalse();
        return result.Diagnostics;
    }

    static string[] SyntaxKinds(string source)
    {
        var parsed = Parse(source);
        var collector = new NodeKindCollector();
        collector.VisitApplication(parsed.Value!);
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

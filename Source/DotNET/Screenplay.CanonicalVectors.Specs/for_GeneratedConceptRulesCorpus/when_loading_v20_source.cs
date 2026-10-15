// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Generation;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_GeneratedConceptRulesCorpus;

// Decision 0084 preserves released source and ESM bytes; authoring and execution now refuse its lookahead.
public class when_loading_v20_source : Specification
{
    ExecutableSemanticModel _model;

    void Because() => _model = SemanticModelSerializer.Deserialize(GeneratedConceptRulesCorpus.V20.EsmBytes.AsSpan());

    [Fact] void should_preserve_the_released_revision() => _model.Revision.ShouldEqual(GeneratedConceptRulesCorpus.V20.SemanticRevision);
    [Fact] void should_preserve_the_released_esm_bytes() => SemanticModelSerializer.Serialize(_model).SequenceEqual(GeneratedConceptRulesCorpus.V20.EsmBytes).ShouldBeTrue();

    [Fact]
    void should_classify_legacy_generation_without_running_its_pattern()
    {
        var concept = _model.Application.Concepts.Single(concept => concept.Name == "InviteCode");
        RuleConsistency.Check(concept).Single().ShouldEqual(RuleConsistencyResult.Unproven("outside the portable match dialect: lookaround"));
    }

    [Fact]
    void should_refuse_execution_at_plan_time()
    {
        var compilation = SemanticExecutionPlan.Compile(_model);
        compilation.Plan.ShouldBeNull();
        compilation.Issues.Any(issue => issue.Kind == SemanticPlanIssueKind.UnsupportedValidation && issue.Details.Contains("lookaround", StringComparison.Ordinal)).ShouldBeTrue();
    }

    [Fact]
    void should_report_the_authoring_error_for_every_preserved_source_form()
    {
        foreach (var form in GeneratedConceptRulesCorpus.SourceForms)
        {
            var catalog = SemanticIdentityCatalogSerializer.Deserialize(form.IdentityCatalogBytes.AsSpan());
            var documents = form.Documents.Select(document => SemanticSourceDocument.Create(catalog.ResolveDocument(document.StableKey), document.StableKey, document.DisplayPath, document.Text));
            var result = new SemanticModelCompiler().Compile("GeneratedConceptRules", SemanticDocumentSet.Create([.. documents], catalog));
            result.Success.ShouldBeFalse();
            result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.PatternOutsidePortableMatchDialect && diagnostic.Message.Contains("lookaround", StringComparison.Ordinal)).ShouldBeTrue();
        }
    }
}

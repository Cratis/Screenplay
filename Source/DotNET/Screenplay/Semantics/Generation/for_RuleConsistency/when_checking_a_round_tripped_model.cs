// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Semantics.Generation.for_RuleConsistency;

public class when_checking_a_round_tripped_model : Specification
{
    CompilationResult<SemanticCompilation> _compilation;
    ExecutableSemanticModel _model;
    Dictionary<string, ImmutableArray<RuleConsistencyResult>> _results;

    void Establish()
    {
        const string Source = """
            concept InviteCode : String
              generate nanoid
              validate
                matches "^[A-Za-z0-9_-]{21}$"
            concept ReviewedCode : String
              generate nanoid
              validate
                not empty
                matches "^(?<code>.{21})$"
                length == 21
                rule ApprovedInvite
                  ```csharp
                  return true;
                  ```
            concept DatedCode : String
              generate pattern "{yyyy}-#{4}"
              validate
                length == 9
                min 9
            concept Id : Uuid
              validate
                rule ApprovedIdentity
                  ```csharp
                  return true;
                  ```
            module Shop
              feature Invitations
                slice StateChange Invite
                  command Invite
                    id Id generated identifier
                    code InviteCode generated
                    reviewed ReviewedCode generated
                    dated DatedCode generated
                    returns id
            """;
        var syntax = new ScreenplayCompiler().Compile(Source);
        syntax.Success.ShouldBeTrue();
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("RuleProofs"));
        const string DocumentKey = "application";
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument(DocumentKey), DocumentKey, "application.play", Source);
        var documents = SemanticDocumentSet.Create([document], catalog);
        _compilation = new SemanticModelBinder().Bind("RuleProofs", syntax.Value!, documents);
        _compilation.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        _model = SemanticModelSerializer.Deserialize(SemanticModelSerializer.Serialize(_compilation.Value!.Model));
    }

    void Because() => _results = _model.Application.Concepts.ToDictionary(concept => concept.Name, concept => RuleConsistency.Check(concept));

    [Fact] void should_check_all_four_concepts() => _results.Count.ShouldEqual(4);
    [Fact] void should_prove_the_nanoid_shape() => _results["InviteCode"].Single().Status.ShouldEqual(RuleConsistencyStatus.Proven);
    [Fact] void should_preserve_rule_order_and_opaque_results() => _results["ReviewedCode"].Select(result => result.Status).ShouldEqual([RuleConsistencyStatus.Proven, RuleConsistencyStatus.Unproven, RuleConsistencyStatus.Proven, RuleConsistencyStatus.Unproven]);
    [Fact] void should_prove_date_pattern_lengths() => _results["DatedCode"].Select(result => result.Status).ShouldEqual([RuleConsistencyStatus.Proven, RuleConsistencyStatus.Proven]);
    [Fact] void should_leave_the_opaque_implicit_uuid_rule_unproven() => _results["Id"].Single().Status.ShouldEqual(RuleConsistencyStatus.Unproven);
    [Fact] void should_keep_the_implicit_uuid_strategy_absent() => _model.Application.Concepts.Single(concept => concept.Name == "Id").Generation.ShouldBeNull();
    [Fact] void should_match_the_compiler_unproven_rule_count() => _compilation.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.UnprovenGeneratedConceptRule).ShouldEqual(_results.Values.SelectMany(results => results).Count(result => result.Status == RuleConsistencyStatus.Unproven));
    [Fact] void should_warn_for_both_unproven_nanoid_rules() => _compilation.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.UnprovenGeneratedConceptRule && diagnostic.Message.Contains("'ReviewedCode'", StringComparison.Ordinal)).ShouldEqual(2);
    [Fact] void should_warn_for_the_opaque_implicit_uuid_rule() => _compilation.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.UnprovenGeneratedConceptRule && diagnostic.Message.Contains("'Id'", StringComparison.Ordinal)).ShouldEqual(1);
    [Fact] void should_not_report_refuted_rules() => _compilation.Diagnostics.ShouldNotContain(diagnostic => diagnostic.Code == DiagnosticCodes.RefutedGeneratedConceptRule);
}

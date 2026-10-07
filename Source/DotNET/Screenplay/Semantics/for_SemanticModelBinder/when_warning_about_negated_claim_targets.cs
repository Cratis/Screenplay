// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_warning_about_negated_claim_targets : given.a_semantic_binder
{
    [Fact] void should_warn_about_an_optional_path() => Warns("not claim \"owner\" matches owner", "String optional");
    [Fact] void should_warn_about_a_grouped_optional_path() => Warns("not (authenticated and claim \"owner\" matches owner)", "String optional");
    [Fact] void should_warn_about_double_negation() => Warns("not not claim \"owner\" matches owner", "String optional");
    [Fact] void should_warn_about_a_numeric_path() => Warns("not claim \"owner\" matches owner", "Int");
    [Fact] void should_warn_about_a_boolean_path() => Warns("not claim \"owner\" matches owner", "Bool");
    [Fact] void should_warn_about_a_collection_path() => Warns("not claim \"owner\" matches owner", "String[]");
    [Fact] void should_warn_about_an_absent_subject() => Warns("not claim \"owner\" matches subject", "String");
    [Fact] void should_not_warn_about_a_required_string() => Quiet("not claim \"owner\" matches owner", "String");
    [Fact] void should_not_warn_about_a_literal() => Quiet("not claim \"owner\" matches \"person\"", "String optional");
    [Fact] void should_not_warn_about_a_positive_optional_target() => Quiet("claim \"owner\" matches owner", "String optional");
    [Fact] void should_not_warn_about_an_unnegated_claim_next_to_not() => Quiet("not role \"Service\" and claim \"owner\" matches owner", "String optional");
    [Fact] void should_not_warn_about_a_text_subject() => Quiet("not claim \"owner\" matches subject", "String identifier");
    [Fact] void should_not_warn_about_a_string_concept() => Quiet("not claim \"owner\" matches owner", "Owner", declarations: "concept Owner : String");
    [Fact] void should_warn_about_a_numeric_concept() => Warns("not claim \"owner\" matches owner", "Owner", declarations: "concept Owner : Int");
    [Fact] void should_warn_about_an_optional_parent() => Warns("not claim \"owner\" matches owner.name", "Owner optional", declarations: "type Owner\n  name String");
    [Fact] void should_warn_about_an_optional_member() => Warns("not claim \"owner\" matches owner.name", "Owner", declarations: "type Owner\n  name String optional");
    [Fact] void should_warn_about_a_numeric_member() => Warns("not claim \"owner\" matches owner.amount", "Owner", declarations: "type Owner\n  amount Int");
    [Fact] void should_not_warn_about_a_required_string_member() => Quiet("not claim \"owner\" matches owner.name", "Owner", declarations: "type Owner\n  name String");

    [Fact]
    void should_not_warn_about_a_keyed_text_query_subject()
    {
        var result = Bind("""
            policy Access
              require not claim "owner" matches subject
            module Portal
              feature Reports
                slice StateView Report
                  readmodel ReportView
                    owner String
                  query GetReport => ReportView optional
                    by owner String
                    authorize Access
            """);
        Assert.True(result.Success, string.Join('\n', result.Diagnostics.Select(diagnostic => diagnostic.Message)));
        result.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.IndeterminateNegatedClaimTarget).ShouldBeEmpty();
    }

    void Warns(string condition, string type, string identifier = "", string declarations = "")
    {
        var result = Binding(condition, type, identifier, declarations);
        var warning = result.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.IndeterminateNegatedClaimTarget);
        warning.Severity.ShouldEqual(DiagnosticSeverity.Warning);
        warning.Location.Line.ShouldEqual(declarations.Split('\n').Length + 9);
        warning.Message.ShouldContain("unknown");
    }

    void Quiet(string condition, string type, string identifier = "", string declarations = "") =>
        Binding(condition, type, identifier, declarations).Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.IndeterminateNegatedClaimTarget).ShouldBeEmpty();

    CompilationResult<SemanticCompilation> Binding(string condition, string type, string identifier, string declarations)
    {
        var result = Bind($"""
            {declarations}
            policy Access
              require {condition}
            module Portal
              feature Reports
                slice StateChange FileReport
                  command FileReport
                    {identifier}
                    owner {type}
                    authorize Access
            """);
        Assert.True(result.Success, string.Join('\n', result.Diagnostics.Select(diagnostic => diagnostic.Message)));

        return result;
    }
}

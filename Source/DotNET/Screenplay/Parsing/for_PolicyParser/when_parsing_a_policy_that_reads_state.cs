// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_PolicyParser;

public class when_parsing_a_policy_that_reads_state : Specification
{
    const string Source = "policy DataQualityEnabled\n  reads AssetPlan as plan by assetId\n  require authenticated and plan.dataQuality == true\n\npolicy IsAuthor\n  reads ArticleView as article by slug\n  require article.author == $identity.id\n";

    CompilationResult<ApplicationSyntax> _parsed;
    PolicySyntax _first;
    PolicySyntax _second;
    string _printed;
    CompilationResult<ApplicationSyntax> _reparsed;

    void Because()
    {
        var compiler = new ScreenplayCompiler();
        _parsed = compiler.Parse(Source);
        _first = _parsed.Value!.Policies.First();
        _second = _parsed.Value!.Policies.Last();
        _printed = new ScreenplayPrinter().Print(_parsed.Value!);
        _reparsed = compiler.Parse(_printed);
    }

    [Fact] void should_parse_without_diagnostics() => _parsed.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
    [Fact] void should_keep_the_read_with_its_alias_and_key() => _first.Reads!.Single().ShouldMatch(read => read.ReadModel == "AssetPlan" && read.Alias == "plan" && read.By == "assetId");
    [Fact] void should_parse_the_state_condition_beside_authenticated() => ((LogicalPolicyConditionSyntax)_first.Condition!).Right.ShouldBeOfExactType<StateConditionSyntax>();
    [Fact] void should_compare_with_the_caller_identity() => ((StateConditionSyntax)_second.Condition!).Comparison.Right.ShouldNotBeNull();
    [Fact] void should_print_the_reads_and_the_condition() => _printed.ShouldContain("reads AssetPlan as plan by assetId");
    [Fact] void should_print_the_state_condition() => _printed.ShouldContain("require authenticated and plan.dataQuality == true");
    [Fact] void should_reparse_the_printed_text_to_the_same_text() => new ScreenplayPrinter().Print(_reparsed.Value!).ShouldEqual(_printed);
}

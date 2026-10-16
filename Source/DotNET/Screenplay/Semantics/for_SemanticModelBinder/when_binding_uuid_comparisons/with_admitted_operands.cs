// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_uuid_comparisons;

public class with_admitted_operands : given.a_uuid_comparison
{
    readonly List<CompilationResult<SemanticCompilation>> _results = [];

    void Because()
    {
        foreach (var op in Operators)
        {
            foreach (var condition in new[]
            {
                $"requestedBy {op} approvedBy",
                $"raw {op} other",
                $"requestedBy {op} raw",
                $"raw {op} requestedBy",
                $"requestedBy {op} \"{Canonical}\"",
                $"raw {op} \"{Canonical}\"",
                $"View.memberId {op} requestedBy",
                $"requestedBy {op} View.memberId",
                $"View.memberId {op} raw",
                $"raw {op} View.memberId",
                $"View.memberId {op} \"{Canonical}\""
            })
            {
                _results.Add(Bind(Model(condition)));
            }

            foreach (var condition in new[] { $"requestedBy {op} approvedBy", $"raw {op} other", $"requestedBy {op} raw", $"requestedBy {op} \"{Canonical}\"" })
                _results.Add(Bind(Model("note != \"\"", condition)));
        }
    }

    [Fact] void should_cover_every_operand_form_and_both_positions() => _results.Count.ShouldEqual(30);
    [Fact] void should_bind_every_comparison() => Assert.True(_results.TrueForAll(result => result.Success), string.Join('\n', _results.SelectMany(result => result.Diagnostics).Select(diagnostic => diagnostic.Message)));
    [Fact] void should_select_the_uuid_comparison_version() => _results.Where(result => result.Success).All(result => result.Value!.Model.SemanticVersion == SemanticVersion.V27 && result.Value.Model.LanguageVersion == LanguageVersion.V27).ShouldBeTrue();
    [Fact] void should_keep_the_literal_as_text() => ((SemanticComparison)Condition(_results[4])).Right.Value.ShouldEqual(SemanticValue.Text(Canonical));
    [Fact] void should_bind_the_read_path_operand() => ((SemanticComparison)Condition(_results[6])).Left.Read.ShouldEqual("View");

    static SemanticCondition Condition(CompilationResult<SemanticCompilation> result) =>
        result.Value!.Model.Application.Modules.Single().Features.Single().Slices.First().Commands.Single().Requirements.Single().Condition;
}

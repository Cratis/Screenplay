// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_uuid_comparisons;

public class with_invalid_literals : given.a_uuid_comparison
{
    readonly List<CompilationResult<SemanticCompilation>> _results = [];

    void Because()
    {
        foreach (var literal in new[] { Canonical.ToUpperInvariant(), $"{{{Canonical}}}", Canonical.Replace("-", string.Empty, StringComparison.Ordinal), "not-a-uuid", string.Empty })
        {
            foreach (var op in Operators)
            {
                foreach (var subject in new[] { "requestedBy", "raw", "View.memberId" })
                    _results.Add(Bind(Model($"{subject} {op} \"{literal}\"")));
                _results.Add(Bind(Model("note != \"\"", $"requestedBy {op} \"{literal}\"")));
            }
        }
    }

    [Fact] void should_check_every_non_canonical_spelling_in_both_positions() => _results.Count.ShouldEqual(40);
    [Fact] void should_refuse_them_all() => _results.TrueForAll(result => !result.Success).ShouldBeTrue();
    [Fact] void should_report_the_invalid_literal() => _results.TrueForAll(result => result.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidSemanticBinding) == 1).ShouldBeTrue();
    [Fact] void should_not_report_the_admission_diagnostic() => _results.TrueForAll(result => result.Diagnostics.All(diagnostic => diagnostic.Code != DiagnosticCodes.UnsupportedSemanticSyntax)).ShouldBeTrue();
}

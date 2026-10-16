// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_uuid_comparisons;

public class with_uuid_validation_rules : given.a_uuid_comparison
{
    readonly List<CompilationResult<SemanticCompilation>> _results = [];

    void Because()
    {
        foreach (var op in Operators)
        {
            _results.Add(Bind(Model($"raw {op} other").Replace($"require raw {op} other", $"raw {op} \"{Canonical}\"", StringComparison.Ordinal)));
            _results.Add(Bind(Model($"raw {op} other").Replace($"require raw {op} other", $"requestedBy {op} \"{Canonical}\"", StringComparison.Ordinal)));
            _results.Add(Bind($"concept Badge : Uuid\n  validate\n    {op} \"{Canonical}\"\n" + Model("raw == other")));
        }
    }

    [Fact] void should_examine_command_property_and_concept_rules() => _results.Count.ShouldEqual(6);
    [Fact] void should_refuse_them_all() => _results.TrueForAll(result => !result.Success).ShouldBeTrue();
    [Fact] void should_report_only_the_admission_diagnostic() => _results.SelectMany(result => result.Diagnostics).ShouldEachConformTo(diagnostic => diagnostic.Code == DiagnosticCodes.UnsupportedSemanticSyntax);
}

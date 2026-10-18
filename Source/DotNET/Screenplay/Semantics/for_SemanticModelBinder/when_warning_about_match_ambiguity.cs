// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_warning_about_match_ambiguity : given.a_semantic_binder
{
    CompilationResult<SemanticCompilation> _result;
    Diagnostic _warning;

    void Because()
    {
        _result = Bind("concept Code : String\n  validate\n    matches \"\\\\d(a+)+$\"");
        _warning = _result.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.AmbiguousMatchPattern);
    }

    [Fact] void should_remain_executable() => _result.Success.ShouldBeTrue();
    [Fact] void should_be_a_warning() => _warning.Severity.ShouldEqual(DiagnosticSeverity.Warning);
    [Fact] void should_map_decoded_offsets_through_source_escapes() => _warning.Location.Column.ShouldEqual(17);
    [Fact] void should_name_the_ambiguous_subexpression() => _warning.Message.ShouldEqual("Match subexpression '(a+)+' at offset 2 is exponentially ambiguous and can cause excessive browser backtracking. Use 'a+' instead.");
}

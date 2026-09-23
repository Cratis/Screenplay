// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_structured_specification_values;

public class and_an_object_member_is_unknown : given.a_structured_specification
{
    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Compile(Source.Replace("\"sku\":\"A-1\"", "\"unknown\":\"A-1\"", StringComparison.Ordinal));

    [Fact] void should_report_play0292_at_the_member() =>
        _result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownStructuredValueMember && diagnostic.Location.Column > 10).ShouldBeTrue();

    [Fact] void should_fail_binding() => _result.Success.ShouldBeFalse();
}

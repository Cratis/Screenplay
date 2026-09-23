// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_structured_specification_values;

public class and_the_value_has_the_wrong_shape : given.a_structured_specification
{
    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Compile(Source.Replace("tags = []", "tags = {}", StringComparison.Ordinal));

    [Fact] void should_report_play0293_at_the_value() =>
        _result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.IncompatibleStructuredValue && diagnostic.Location.Column > 10).ShouldBeTrue();
}

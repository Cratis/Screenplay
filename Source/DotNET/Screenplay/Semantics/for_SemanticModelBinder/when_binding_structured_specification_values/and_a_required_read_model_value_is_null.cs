// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_structured_specification_values;

public class and_a_required_read_model_value_is_null : given.a_structured_specification
{
    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Compile(Source.Replace("tags = []", "tags = null", StringComparison.Ordinal));

    [Fact] void should_report_play0353_at_the_null() =>
        _result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidSpecificationNull && diagnostic.Location.Column > 10).ShouldBeTrue();
}

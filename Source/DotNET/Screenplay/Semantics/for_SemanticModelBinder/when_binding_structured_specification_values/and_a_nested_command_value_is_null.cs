// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_structured_specification_values;

public class and_a_nested_command_value_is_null : given.a_structured_specification
{
    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Compile(Source.Replace("enabled Bool", "enabled Bool?", StringComparison.Ordinal)
        .Replace("lines = [{\"sku\":\"A-1\",\"detail\":{\"enabled\":true}}]", "lines = [{\"sku\":\"A-1\",\"detail\":{\"enabled\":null}}]", StringComparison.Ordinal));

    [Fact] void should_report_play0350_at_nested_command_and_event_values() =>
        _result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.NullSpecificationFact && diagnostic.Location.Column > 10).ShouldBeTrue();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_structured_specification_values;

public class and_a_required_member_is_missing : given.a_structured_specification
{
    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Compile(Source.Replace("\"sku\":\"A-1\",", string.Empty, StringComparison.Ordinal));

    [Fact] void should_report_play0354_at_the_object() =>
        _result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.MissingStructuredValueMember && diagnostic.Location.Column > 10).ShouldBeTrue();
}

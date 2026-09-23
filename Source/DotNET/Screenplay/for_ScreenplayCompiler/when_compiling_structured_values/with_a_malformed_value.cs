// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_compiling_structured_values;

public class with_a_malformed_value : given.a_structured_specification
{
    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = Compiler.Compile(Source.Replace("[{\"sku\":\"A-1\",\"status\":\"open\"}]", "[{sku:1}]", StringComparison.Ordinal));

    [Fact] void should_report_the_invalid_json() => _result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidStructuredValue).ShouldBeTrue();
    [Fact] void should_fail_compilation() => _result.Success.ShouldBeFalse();
}

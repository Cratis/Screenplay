// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_compiling_structured_values;

public class with_a_wrong_shape : given.a_structured_specification
{
    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = Compiler.Compile(Source.Replace("lines = [{\"sku\":\"A-1\",\"status\":\"open\"}]", "lines = 3", StringComparison.Ordinal));

    [Fact] void should_report_the_expected_list() => _result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.IncompatibleStructuredValue).ShouldBeTrue();
}

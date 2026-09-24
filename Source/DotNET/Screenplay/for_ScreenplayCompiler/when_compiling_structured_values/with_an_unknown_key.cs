// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_compiling_structured_values;

public class with_an_unknown_key : given.a_structured_specification
{
    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = Compiler.Compile(Source.Replace("\"sku\":", "\"unknown\":", StringComparison.Ordinal));

    [Fact] void should_report_the_unknown_member() => _result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownStructuredValueMember).ShouldBeTrue();
}

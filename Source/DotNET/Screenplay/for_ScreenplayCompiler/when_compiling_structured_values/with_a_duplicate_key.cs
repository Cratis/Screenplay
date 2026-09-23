// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_compiling_structured_values;

public class with_a_duplicate_key : given.a_structured_specification
{
    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = Compiler.Compile(Source.Replace("\"sku\":\"A-1\"", "\"sku\":\"A-1\",\"sku\":\"B-2\"", StringComparison.Ordinal));

    [Fact] void should_report_each_duplicate_at_the_key() =>
        _result.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.DuplicateStructuredValueMember && diagnostic.Location.Column > 10).ShouldEqual(2);

    [Fact] void should_reject_the_document() => _result.Success.ShouldBeFalse();
}

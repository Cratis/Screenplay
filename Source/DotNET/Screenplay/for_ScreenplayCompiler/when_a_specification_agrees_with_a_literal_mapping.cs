// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_a_specification_agrees_with_a_literal_mapping : given.a_consistent_model
{
    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source.Replace("note = note", "note = \"authored\"", StringComparison.Ordinal));

    [Fact] void should_accept_the_producible_literal_assertion() => _result.Success.ShouldBeTrue();
    [Fact] void should_report_no_consistency_error() => _result.Diagnostics.ShouldBeEmpty();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_a_specification_contradicts_a_literal_mapping : given.a_consistent_model
{
    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source.Replace("note = note", "note = \"literal\"", StringComparison.Ordinal));

    [Fact] void should_reject_the_unreachable_literal_assertion() => _result.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.UnreachableSpecificationOutcome);
    [Fact] void should_fail_compilation() => _result.Success.ShouldBeFalse();
}

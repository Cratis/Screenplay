// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_rejecting_duplicate_enum_member_numbers : Specification
{
    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = new ScreenplayCompiler().Parse("concept Status : Enum\n  draft = 0\n  issued = -0");

    [Fact] void should_treat_negative_zero_as_zero() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_the_distinct_duplicate_number_error() => _result.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.DuplicateEnumerationNumber);
    [Fact] void should_locate_the_duplicate() => _result.Diagnostics.Single().Location.Line.ShouldEqual(3);
}

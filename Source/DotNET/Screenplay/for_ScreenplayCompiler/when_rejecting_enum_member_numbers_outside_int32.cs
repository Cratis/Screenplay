// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_rejecting_enum_member_numbers_outside_int32 : Specification
{
    CompilationResult<ApplicationSyntax>[] _results;

    void Because() => _results = [.. new[] { "2147483648", "-2147483649", "999999999999999999999999999999999999999999999" }.Select(number => new ScreenplayCompiler().Parse($"concept Status : Enum\n  draft = {number}"))];

    [Fact] void should_refuse_every_overflow() => _results.All(result => !result.Success).ShouldBeTrue();
    [Fact] void should_report_the_distinct_range_error() => _results.All(result => result.Diagnostics.Single().Code == DiagnosticCodes.EnumerationNumberOutsideInt32).ShouldBeTrue();
}

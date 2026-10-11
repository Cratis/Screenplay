// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_rejecting_malformed_enum_member_numbers : Specification
{
    CompilationResult<ApplicationSyntax>[] _results;

    void Because() => _results = [.. new[] { "", "+1", "01", "-01", "1.0", "1e2", "-", "1 2", "one", "1 = 2", "١" }.Select(number => new ScreenplayCompiler().Parse($"concept Status : Enum\n  draft = {number}"))];

    [Fact] void should_refuse_every_non_integer_spelling() => _results.All(result => !result.Success).ShouldBeTrue();
    [Fact] void should_report_the_distinct_malformed_literal_error() => _results.All(result => result.Diagnostics.Single().Code == DiagnosticCodes.MalformedEnumerationNumber).ShouldBeTrue();
}

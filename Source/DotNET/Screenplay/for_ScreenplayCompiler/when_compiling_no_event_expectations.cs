// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_no_event_expectations : given.a_compiler
{
    CompilationResult<SpecificationSyntax> _explicit;
    CompilationResult<SpecificationSyntax> _omitted;

    void Because()
    {
        _explicit = _compiler.CompileSpecification("specification NothingHappens\n  when DoNothing\n  then no events");
        _omitted = _compiler.CompileSpecification("specification NothingHappens\n  when DoNothing");
    }

    [Fact] void should_accept_an_explicit_no_event_expectation() => _explicit.Success.ShouldBeTrue();
    [Fact] void should_retain_the_explicit_assertion() => _explicit.Value!.ThenNoEvents.ShouldBeTrue();
    [Fact] void should_not_invent_an_event_type() => _explicit.Value!.ThenEvents.ShouldBeEmpty();
    [Fact] void should_keep_omission_distinct_in_syntax() => _omitted.Value!.ThenNoEvents.ShouldBeFalse();

    [Theory]
    [InlineData("when append Happened\n  then no events")]
    [InlineData("when DoNothing\n  then no events\n  then Happened")]
    [InlineData("when DoNothing\n  then Happened\n  then no events")]
    [InlineData("when DoNothing\n  then no events\n  then events in any order")]
    [InlineData("when DoNothing\n  then no events\n  then error")]
    [InlineData("when DoNothing\n  then denied\n  then no events")]
    [InlineData("when DoNothing\n  then no events\n  then no events")]
    [InlineData("when DoNothing\n  then no events exactly")]
    [InlineData("when DoNothing\n  then no events\n    value = 1")]
    void should_reject_invalid_no_event_expectations(string body) =>
        _compiler.CompileSpecification($"specification Invalid\n  {body}").Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidNoEventsExpectation).ShouldBeTrue();
}

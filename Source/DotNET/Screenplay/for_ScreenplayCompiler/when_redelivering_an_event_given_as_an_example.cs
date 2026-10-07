// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_redelivering_an_event_given_as_an_example : given.a_compiler
{
    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile("""
        module Billing
          feature Payments
            slice Automation Claiming
              event Approved
                id String
              example FirstApproval : Approved
                for "a"
                id = "original"
              reaction Claimer
                when Approved
              specification Redelivering
                given FirstApproval id = "a"
                given FirstApproval
                  for "b"
                  id = "b"
                when redelivered Approved to Claimer
                  for "a"
                  id = "a"
                then no events
        """);

    [Fact] void should_match_the_expanded_given_and_its_override() => _result.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
    [Fact] void should_keep_the_authored_example_reference() => _result.Value!.Modules.Single().Features.Single().Slices.Single().Specifications.Single().Given.First().EventType.ShouldEqual("FirstApproval");
    [Fact] void should_keep_the_explicit_no_event_assertion() => SpecificationExamples.Expand(_result.Value!).Specifications.Single().Effective.ThenNoEvents.ShouldBeTrue();
}

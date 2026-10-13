// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_combining_unadmitted_specification_features : Specification
{
    McpAuthoringReadiness _readiness;
    SpecificationSyntax _specification;

    void Establish()
    {
        var application = new ScreenplayCompiler().Parse(
            """
            module M
              feature F
                slice StateChange S
                  event E
                  command C
                  specification X
                    given E
                      for "account"
                      stream Account.Events
                    when C
                    then no events
            """).Value!;
        _specification = application.Modules.Single().Features.Single().Slices.Single().Specifications.Single();
        _readiness = new(application);
    }

    [Fact] void should_leave_the_specification_admitted() => _readiness.SyntaxOnly(_specification).ShouldBeFalse();
    [Fact] void should_not_report_no_events_as_unadmitted() => _readiness.ExecutionReadiness(_specification).ShouldNotContain("PLAY0268");

    [Fact]
    void should_admit_redelivery_independently_of_refusal_branches()
    {
        var application = new ScreenplayCompiler().Parse("module M\n  feature F\n    slice Automation S\n      event E\n      reaction Observer\n        when E\n      specification T\n        given E\n        when redelivered E to Observer\n        then E").Value!;
        var specification = application.Modules.Single().Features.Single().Slices.Single().Specifications.Single();
        var readiness = new McpAuthoringReadiness(application);
        readiness.SyntaxOnly(specification).ShouldBeFalse();
        readiness.ExecutionReadiness(specification).ShouldNotContain("PLAY0268");
    }
}

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

    [Fact] void should_disclose_the_specification_as_syntax_only() => _readiness.SyntaxOnly(_specification).ShouldBeTrue();
    [Fact] void should_disclose_no_event_admission() => _readiness.ExecutionReadiness(_specification).ShouldContain("explicit no-event assertions (#433)");
    [Fact] void should_disclose_route_admission() => _readiness.ExecutionReadiness(_specification).ShouldContain("specification event routes (#457)");
}

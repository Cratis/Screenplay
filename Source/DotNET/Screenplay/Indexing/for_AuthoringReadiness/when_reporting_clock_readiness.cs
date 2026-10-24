// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Indexing.for_AuthoringReadiness;

public class when_reporting_clock_readiness
{
    readonly ApplicationSyntax _syntax = new ScreenplayCompiler().Parse(
        """
        module M
          feature F
            slice StateChange S
              command C
                occurred DateTime
              screen Details
                action "Schedule"
                  when date of item.occurred < date of now + 2 hours execute C
        """).Value!;

    [Fact]
    void should_disclose_clock_operands_nested_in_guarded_actions()
    {
        var readiness = new AuthoringReadiness(_syntax);
        var slice = _syntax.Modules.Single().Features.Single().Slices.Single();
        readiness.ModelSyntaxOnly.ShouldBeTrue();
        readiness.ModelExecutionReadiness!.ShouldContain("(#824)");
        readiness.SyntaxOnly(slice.Screens.Single()).ShouldBeTrue();
        readiness.ExecutionReadiness(slice.Screens.Single())!.ShouldContain("(#824)");
        readiness.SyntaxOnly(slice.Commands.Single()).ShouldBeFalse();
        readiness.ExecutionReadiness(slice.Commands.Single()).ShouldBeNull();
    }
}

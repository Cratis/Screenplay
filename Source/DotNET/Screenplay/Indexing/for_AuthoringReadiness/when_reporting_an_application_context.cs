// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Indexing.for_AuthoringReadiness;

public class when_reporting_an_application_context : Specification
{
    [Theory]
    [InlineData("context\n  businessTimeZone time zone\n")]
    [InlineData("context\n")]
    public void should_disclose_the_declaration_as_unadmitted(string source)
    {
        var syntax = new ScreenplayCompiler().Parse(source).Value!;
        var readiness = new AuthoringReadiness(syntax);
        readiness.ModelSyntaxOnly.ShouldBeTrue();
        readiness.ModelExecutionReadiness!.ShouldContain("#824");
        readiness.SyntaxOnly(syntax.Context!).ShouldBeTrue();
        foreach (var member in syntax.Context!.Members)
        {
            readiness.SyntaxOnly(member).ShouldBeTrue();
            readiness.ExecutionReadiness(member)!.ShouldContain("#824");
        }
    }
}

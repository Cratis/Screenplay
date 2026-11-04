// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Indexing.for_AuthoringReadiness;

public class when_reporting_an_application_context : Specification
{
    [Theory]
    [InlineData("context\n  businessTimeZone time zone\n")]
    [InlineData("context\n")]
    public void should_report_the_declaration_as_admitted(string source)
    {
        var syntax = new ScreenplayCompiler().Parse(source).Value!;
        var readiness = new AuthoringReadiness(syntax);
        readiness.ModelSyntaxOnly.ShouldBeFalse();
        readiness.ModelExecutionReadiness.ShouldBeNull();
        readiness.SyntaxOnly(syntax.Context!).ShouldBeFalse();
        foreach (var member in syntax.Context!.Members)
        {
            readiness.SyntaxOnly(member).ShouldBeFalse();
            readiness.ExecutionReadiness(member).ShouldBeNull();
        }
    }
}

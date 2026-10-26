// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Indexing.for_AuthoringReadiness;

public class when_reporting_attached_handler_readiness
{
    [Theory]
    [InlineData("produces E", false)]
    [InlineData("produces E when true", true)]
    [InlineData("", true)]
    void should_distinguish_the_result_contract_from_unadmitted_syntax(string production, bool syntaxOnly)
    {
        var syntax = new ScreenplayCompiler().Parse("module M\n  feature F\n    slice StateChange S\n      event E\n      command C\n        " + production + "\n        handler\n          file C.cs\n").Value!;
        var command = syntax.Modules.Single().Features.Single().Slices.Single().Commands.Single();
        new AuthoringReadiness(syntax).SyntaxOnly(command).ShouldEqual(syntaxOnly);
    }
}

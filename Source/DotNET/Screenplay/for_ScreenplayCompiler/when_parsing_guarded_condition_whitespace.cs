// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_parsing_guarded_condition_whitespace
{
    [Theory]
    [InlineData("", "item.status\u0085==\u0085\"open\"")]
    [InlineData("numbers exact\n", "item.status\u0085==\u0085\"open\"")]
    [InlineData("numbers exact\n", "item.count >= 1e+2\u0085and item.count < 2e+2")]
    public void should_accept_dotnet_whitespace_separators(string preamble, string condition) => Parse(preamble, condition).Diagnostics.ShouldBeEmpty();

    [Theory]
    [InlineData("")]
    [InlineData("numbers exact\n")]
    public void should_reject_a_byte_order_mark_as_a_separator(string preamble)
    {
        var result = Parse(preamble, "item.status\ufeff==\ufeff\"open\"");
        result.Success.ShouldBeFalse();
        result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContainOnly(DiagnosticCodes.UnsupportedActionConditionOperand, DiagnosticCodes.GuardedActionWithoutAlternatives);
        result.Diagnostics.First().Message.ShouldEqual("Guarded action conditions contain unsupported character '\ufeff'");
    }

    static CompilationResult<Syntax.ApplicationSyntax> Parse(string preamble, string condition) => new ScreenplayCompiler().Parse(preamble + "module Work\n  feature Items\n    slice StateView Details\n      screen Details\n        action \"Again\"\n          when " + condition + " execute Retry");
}

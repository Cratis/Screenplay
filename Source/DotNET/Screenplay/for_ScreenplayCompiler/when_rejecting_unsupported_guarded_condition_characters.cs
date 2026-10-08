// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_rejecting_unsupported_guarded_condition_characters
{
    [Theory]
    [InlineData("", "[\"open\"]", "[")]
    [InlineData("", "\"open\";", ";")]
    [InlineData("", "{\"open\"", "{")]
    [InlineData("numbers exact\n", "[\"open\"]", "[")]
    [InlineData("numbers exact\n", "\"open\";", ";")]
    [InlineData("numbers exact\n", "{\"open\"", "{")]
    public void should_reject_punctuation_instead_of_changing_the_condition(string preamble, string operand, string character)
    {
        var result = new ScreenplayCompiler().Parse(preamble + "module Work\n  feature Items\n    slice StateView Details\n      screen Details\n        action \"Again\"\n          when item.status == " + operand + " execute Retry");
        result.Success.ShouldBeFalse();
        result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContainOnly(DiagnosticCodes.UnsupportedActionConditionOperand, DiagnosticCodes.GuardedActionWithoutAlternatives);
        result.Diagnostics.First().Message.ShouldEqual($"Guarded action conditions contain unsupported character '{character}'");
    }
}

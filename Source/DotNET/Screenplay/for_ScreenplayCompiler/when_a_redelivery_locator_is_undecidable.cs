// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_a_redelivery_locator_is_undecidable : given.a_compiler
{
    const string Source = """
        module Billing
          feature Claims
            slice Automation Handling
              event Approved
                id String
              command Approve
                id String identifier
                produces Approved
                  id = id
              reaction Claimer
                when Approved
              specification Recovery
        """;

    [Theory]
    [InlineData("        given Approved\n          id = \"a\"", "          for \"a\"")]
    [InlineData("        given Approved\n          for unknownSource\n          id = \"a\"", "          for \"a\"")]
    [InlineData("        given Approved\n          id = unknownValue", "          id = \"a\"")]
    [InlineData("        given Approved\n          id = \"a\"", "          id = unknownValue")]
    void should_report_cannot_locate_instead_of_zero_occurrences(string given, string locator)
    {
        var result = _compiler.Compile(Source + $"\n{given}\n        when redelivered Approved to Claimer\n{locator}");
        var diagnostic = result.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.UnmatchedRedeliveredOccurrence);
        diagnostic.Message.ShouldContain("Cannot locate redelivery");
        diagnostic.Message.Contains("matches 0", StringComparison.Ordinal).ShouldBeFalse();
    }

    [Fact]
    void should_not_count_occurrences_after_an_unresolved_reaction_or_event()
    {
        var result = _compiler.Compile(Source + "\n        given Approved\n        when redelivered Missing to Unknown");
        result.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownRedeliveryReaction).ShouldEqual(1);
        result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.UnmatchedRedeliveredOccurrence).ShouldBeFalse();
    }
}

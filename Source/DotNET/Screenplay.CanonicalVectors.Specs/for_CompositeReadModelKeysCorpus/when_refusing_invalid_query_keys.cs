// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_CompositeReadModelKeysCorpus;

public class when_refusing_invalid_query_keys : given.a_flat_key_model
{
    [Theory]
    [InlineData("Row optional", "first String\nsecond String\nother String", "PLAY0662")]
    [InlineData("Row optional", "first String optional\nsecond String", "PLAY0662")]
    [InlineData("Row optional", "first String[]\nsecond String", "PLAY0662")]
    [InlineData("Row optional", "first Int\nsecond String", "PLAY0662")]
    [InlineData("Row[]", "first String\nsecond String", "PLAY0268")]
    [InlineData("Row", "first String\nsecond String", "PLAY0268")]
    [InlineData("Row optional", "first String from $context.eventSourceId\nsecond String", "PLAY0268")]
    public void should_refuse_the_query_with_its_declared_diagnostic(string returnType, string parts, string expected)
    {
        var source = Declarations + $"\n      query Invalid => {returnType}\n        by\n" + Indent(parts, 10);
        var result = Compile(source);
        result.Success.ShouldBeFalse();
        result.Value.ShouldBeNull();
        result.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).Select(diagnostic => diagnostic.Code).ShouldContainOnly(expected);
    }

    [Fact]
    void should_name_a_missing_part_in_an_otherwise_valid_by_block()
    {
        var source = Declarations
            .Replace("        quantity Int", "        third String key\n        quantity Int", StringComparison.Ordinal)
            .Replace("          second String", "          second String\n          third String", StringComparison.Ordinal)
            + "\n      query Incomplete => Row optional\n        by\n          first String\n          second String";
        var result = Compile(source);
        result.Success.ShouldBeFalse();
        result.Value.ShouldBeNull();
        result.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).Select(diagnostic => diagnostic.Code).ShouldContainOnly("PLAY0661");
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_compiling_structured_values;

public class with_a_non_finite_number : given.a_structured_specification
{
    [Theory]
    [InlineData("[1e400]")]
    [InlineData("[-1e400]")]
    [InlineData("{\"amount\":1e400}")]
    public void should_report_invalid_structured_values_without_throwing(string value)
    {
        var result = Compiler.Compile(Source
            .Replace("lines Line[]", "amounts Decimal[]", StringComparison.Ordinal)
            .Replace("lines = lines", "amounts = amounts", StringComparison.Ordinal)
            .Replace("lines = [{\"sku\":\"A-1\",\"status\":\"open\"}]", $"amounts = {value}", StringComparison.Ordinal));

        result.Success.ShouldBeFalse();
        result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidStructuredValue).ShouldBeTrue();
    }

    [Fact]
    public void should_report_a_309_digit_number_without_throwing() =>
        should_report_invalid_structured_values_without_throwing($"[1{new string('0', 309)}]");
}

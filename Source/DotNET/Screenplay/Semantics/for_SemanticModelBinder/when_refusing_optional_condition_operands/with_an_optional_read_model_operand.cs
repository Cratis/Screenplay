// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_refusing_optional_condition_operands;

public class with_an_optional_read_model_operand : when_binding_protected_reads.given.a_protected_read_model
{
    [Theory]
    [InlineData("View.value == \"active\"")]
    [InlineData("value == View.value")]
    void should_refuse_the_optional_enum_operand_at_the_comparison(string condition)
    {
        var source = "concept Status : Enum\n  active\n  inactive\n" + Source
            .Replace("require View.value != \"\"", $"require {condition}", StringComparison.Ordinal)
            .Replace("value = View.value", "value = value", StringComparison.Ordinal)
            .Replace("value String", "value Status", StringComparison.Ordinal)
            .Replace("id String key\n        value Status", "id String key\n        value Status optional", StringComparison.Ordinal);
        var result = Bind(source, "reads.play", "reads.play");
        result.Success.ShouldBeFalse();
        Assert.True(result.Diagnostics.Any(value => value.Code == DiagnosticCodes.UnsupportedSemanticSyntax), string.Join('\n', result.Diagnostics.Select(value => $"{value.Code}: {value.Message}")));
        var diagnostic = result.Diagnostics.Single(value => value.Code == DiagnosticCodes.UnsupportedSemanticSyntax);
        diagnostic.Message.ShouldContain("comparison operands must be required");
        diagnostic.Location.Path.ShouldEqual("reads.play");
        diagnostic.Location.Line.ShouldEqual(16);
        result.Diagnostics.Any(value => value.Code == DiagnosticCodes.InvalidSemanticBinding).ShouldBeFalse();
    }
}

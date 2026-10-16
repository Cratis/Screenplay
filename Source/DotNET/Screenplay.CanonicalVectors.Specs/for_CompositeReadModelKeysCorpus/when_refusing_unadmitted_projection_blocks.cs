// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_CompositeReadModelKeysCorpus;

public class when_refusing_unadmitted_projection_blocks : given.a_flat_key_model
{
    [Theory]
    [InlineData("every")]
    [InlineData("all")]
    [InlineData("join detail on first\n  with Recorded")]
    [InlineData("children rows identified by first\n  from Recorded")]
    [InlineData("nested detail\n  from Recorded")]
    [InlineData("clear with Removed")]
    [InlineData("remove via join on Removed")]
    [InlineData("variant Row\n  enters on Recorded")]
    public void should_report_the_composite_shape_refusal_at_binding(string block)
    {
        // Parse preserves even a root clear-with node; this pins BindScope's independent refusal,
        // without weakening the existing source rule that clear-with belongs inside nested.
        var result = Bind(Declarations + "\n      projection Rows => Row\n" + Indent(block, 8));
        result.Success.ShouldBeFalse();
        result.Value.ShouldBeNull();
        var errors = result.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();
        errors.Select(diagnostic => diagnostic.Code).ShouldContainOnly(DiagnosticCodes.UnsupportedSemanticSyntax);
        errors.All(diagnostic => diagnostic.Message.Contains("composite", StringComparison.OrdinalIgnoreCase)).ShouldBeTrue();
    }

    [Fact]
    void should_refuse_a_reducer_over_the_composite_view()
    {
        var result = Bind(Declarations + "\n      reducer Reduce => Row\n        on Recorded\n          file Reduce.cs\n");
        result.Success.ShouldBeFalse();
        result.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).Select(diagnostic => diagnostic.Code).ShouldContainOnly(DiagnosticCodes.UnsupportedSemanticSyntax);
    }
}

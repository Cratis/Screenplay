// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_protected_reads;

public class with_optional_view_properties : given.a_protected_read_model
{
    [Fact]
    void should_map_optional_properties_to_optional_event_properties()
    {
        var source = Source.Replace("value String", "value String optional", StringComparison.Ordinal)
            .Replace("        validate\n          require View.value != \"\"\n", string.Empty, StringComparison.Ordinal);
        var result = Bind(source);
        Assert.True(result.Success, string.Join('\n', result.Diagnostics.Select(diagnostic => diagnostic.Message)));
    }

    [Fact]
    void should_apply_existing_optional_comparison_rules()
    {
        var source = Source.Replace("value String", "value String optional", StringComparison.Ordinal);
        var read = Bind(source);
        var input = Bind(source.Replace("        reads View by id\n", string.Empty, StringComparison.Ordinal).Replace("View.value", "value", StringComparison.Ordinal));
        read.Success.ShouldBeFalse();
        input.Success.ShouldBeFalse();
        read.Diagnostics.Last(diagnostic => diagnostic.Severity == Diagnostics.DiagnosticSeverity.Error).Message.ShouldEqual(input.Diagnostics.Last(diagnostic => diagnostic.Severity == Diagnostics.DiagnosticSeverity.Error).Message);
    }
}

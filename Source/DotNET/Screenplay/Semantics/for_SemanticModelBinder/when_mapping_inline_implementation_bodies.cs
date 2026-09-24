// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_mapping_inline_implementation_bodies : given.a_semantic_binder
{
    const string Prefix = "module Projects\n  feature Registration\n    slice StateChange Register\n      command Register\n        handler\n";
    const string Info = "          ```csharp\n            return 1;\n\n            \treturn 2;\n          ```";

    [Fact] void should_map_every_line_including_blank_lines_and_tabs_in_a_nested_body()
    {
        const string source = Prefix + Info;
        var requirement = Bind(source).ImplementationRequirements.Single();
        var lines = requirement.BodyLines;
        lines.Length.ShouldEqual(3);
        lines[0].Line.ShouldEqual(7);
        lines[0].Column.ShouldEqual(11);
        lines[1].Line.ShouldEqual(8);
        lines[1].Column.ShouldEqual(1);
        lines[2].Line.ShouldEqual(9);
        lines[2].Column.ShouldEqual(11);
        source.Split('\n')[lines[2].Line - 1][(lines[2].Column - 1)..].ShouldEqual("  \treturn 2;");
        var span = requirement.BodySpan!.Value;
        span.Start.ShouldEqual(source.IndexOf("  return 1;", StringComparison.Ordinal));
        span.End.ShouldEqual(source.IndexOf("\n          ```", span.Start, StringComparison.Ordinal));
        span.StartLine.ShouldEqual(7);
        span.EndLine.ShouldEqual(9);
        span.EndColumn.ShouldEqual(23);
    }

    [Fact] void should_count_crlf_as_two_offsets_but_one_line_ending()
    {
        var source = (Prefix + Info).Replace("\n", "\r\n", StringComparison.Ordinal);
        var requirement = Bind(source).ImplementationRequirements.Single();
        var first = requirement.BodyLines[0];
        var span = requirement.BodySpan!.Value;
        first.Line.ShouldEqual(7);
        first.Column.ShouldEqual(11);
        span.Start.ShouldEqual(source.IndexOf("  return 1;", StringComparison.Ordinal));
        span.End.ShouldEqual(source.IndexOf("\r\n          ```", span.Start, StringComparison.Ordinal));
    }

    [Fact] void should_map_the_deprecated_tag_line_form()
    {
        var source = Prefix.Replace("handler\n", "validate csharp\n", StringComparison.Ordinal) + "          ```\n            return true;\n          ```";
        var requirement = Bind(source).ImplementationRequirements.Single();
        requirement.BodyLines[0].Line.ShouldEqual(7);
        requirement.BodyLines[0].Column.ShouldEqual(11);
        requirement.BodySpan!.Value.Start.ShouldEqual(source.IndexOf("  return true;", StringComparison.Ordinal));
    }

    [Fact] void should_map_a_last_body_line_before_a_closing_fence_at_eof()
    {
        const string source = Prefix + "          ```csharp\n            return 1;\n          ```";
        var requirement = Bind(source).ImplementationRequirements.Single();
        requirement.BodySpan!.Value.End.ShouldEqual(source.IndexOf("\n          ```", source.IndexOf("return 1;", StringComparison.Ordinal), StringComparison.Ordinal));
        requirement.BodyLines.Single().Line.ShouldEqual(7);
    }

    [Fact] void should_map_an_empty_body_to_the_start_of_its_closing_fence()
    {
        const string source = Prefix + "          ```csharp\n          ```";
        var requirement = Bind(source).ImplementationRequirements.Single();
        requirement.BodyLines.IsEmpty.ShouldBeTrue();
        requirement.BodySpan!.Value.Start.ShouldEqual(source.LastIndexOf("          ```", StringComparison.Ordinal));
        requirement.BodySpan!.Value.End.ShouldEqual(requirement.BodySpan.Value.Start);
    }

    [Fact] void should_keep_positions_out_of_structural_syntax_json()
    {
        const string source = Prefix + Info;
        var syntax = new ScreenplayCompiler().Parse(source).Value!;
        var code = syntax.Modules.Single().Features.Single().Slices.Single().Commands.Single().Handler!.Code!;
        var relocated = code with { BodyStartOffset = 42, BodyLines = [new CodeBlockSourceLine(99, 4)] };
        SyntaxJson.StructurallyEqual(code, relocated).ShouldBeTrue();
        SyntaxJson.Serialize(code).TryGetProperty("bodyLines", out _).ShouldBeFalse();
    }
}

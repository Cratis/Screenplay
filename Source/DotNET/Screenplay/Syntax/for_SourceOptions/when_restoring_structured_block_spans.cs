// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Syntax.for_SourceOptions;

public class when_restoring_structured_block_spans
{
    readonly ScreenplayCompiler _compiler = new();

    [Theory]
    [InlineData("")]
    [InlineData("numbers exact\n")]
    void should_keep_structured_block_and_utf16_nested_spans_in_both_modes(string prefix)
    {
        const string mapping = "    amount\n      café = \"😀\"\n      n\n        item 1\n        item\n          v = 2";
        var result = _compiler.CompileSpecification(prefix + "specification T\n  when C\n" + mapping + "\n");
        result.Success.ShouldBeTrue();
        var source = (ObjectExpressionSyntax)result.Value!.When!.Values.Single().Source;
        source.Location.Column.ShouldEqual(5);
        var first = source.Members.First();
        first.RawLocation!.Column.ShouldEqual(7);
        first.RawLength.ShouldEqual("café".Length);
        var list = (ListExpressionSyntax)source.Members.Last().Value;
        var literal = (LiteralExpressionSyntax)list.Items.First();
        literal.RawLocation!.Column.ShouldEqual("        item ".Length + 1);
        literal.RawLength.ShouldEqual(1);
        var nested = (LiteralExpressionSyntax)((ObjectExpressionSyntax)list.Items.Last()).Members.Single().Value;
        nested.RawLocation!.Column.ShouldEqual("          v = ".Length + 1);
    }

    [Theory]
    [InlineData("")]
    [InlineData("numbers exact\n")]
    void should_patch_only_the_nested_literal_in_the_actual_source(string prefix)
    {
        var text = prefix + "module M\n  feature F\n    slice StateChange S\n      specification T\n        when C\n          amount\n            café = \"ok\"\n            n\n              item 1 // keep\n";
        var original = WorkspaceDocument.Create("input", PortablePlayPath.Parse("input.play"), Encoding.UTF8.GetBytes(text));
        var intended = _compiler.Parse(text.Replace("item 1", "item 2", StringComparison.Ordinal)).Value!;
        WorkspaceTriviaPrinter.Print(original, intended).Text.ShouldEqual(text.Replace("item 1", "item 2", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("")]
    [InlineData("numbers exact\n")]
    void should_report_duplicate_and_malformed_members_at_their_lines(string prefix)
    {
        const string duplicate = "    amount\n      n\n        item 1\n      n = 2";
        var parsed = _compiler.CompileSpecification(prefix + "specification T\n  when C\n" + duplicate + "\n");
        var error = parsed.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.DuplicateStructuredValueMember);
        error.Location.Line.ShouldEqual(prefix.Length == 0 ? 6 : 7);
        error.Location.Column.ShouldEqual(7);
        var malformed = _compiler.CompileSpecification(prefix + "specification T\n  when C\n    amount\n      n : 1\n");
        malformed.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidStructuredValue).Location.Column.ShouldEqual(7);
    }

    [Theory]
    [InlineData("")]
    [InlineData("numbers exact\n")]
    void should_refuse_inline_json(string prefix)
    {
        var parsed = _compiler.CompileSpecification(prefix + "specification T\n  when C\n    amount = {\"n\":[1]}\n");
        var error = parsed.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.InlineJsonValue);
        error.Location.Column.ShouldEqual(14);
    }
}

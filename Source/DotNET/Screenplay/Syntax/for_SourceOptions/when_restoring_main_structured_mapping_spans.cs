// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Syntax.for_SourceOptions;

public class when_restoring_main_structured_mapping_spans
{
    readonly ScreenplayCompiler _compiler = new();

    [Theory]
    [InlineData("")]
    [InlineData("numbers exact\n")]
    void should_keep_main_structured_rhs_and_utf16_nested_spans_in_both_modes(string prefix)
    {
        const string mapping = "    amount = {\"café\":\"😀\",\"n\":[1,{\"v\":2}]}";
        var result = _compiler.CompileSpecification(prefix + "specification T\n  when C\n" + mapping + "\n");
        result.Success.ShouldBeTrue();
        var source = (ObjectExpressionSyntax)result.Value!.When!.Values.Single().Source;
        source.Location.Column.ShouldEqual(mapping.IndexOf('{') + 1);
        var first = source.Members.First();
        first.RawLocation!.Column.ShouldEqual(mapping.IndexOf("\"café\"", StringComparison.Ordinal) + 1);
        var list = (ListExpressionSyntax)source.Members.Last().Value;
        var literal = (LiteralExpressionSyntax)list.Items.First();
        literal.RawLocation!.Column.ShouldEqual(mapping.IndexOf("[1", StringComparison.Ordinal) + 2);
        literal.RawLength.ShouldEqual(1);
        var nested = (LiteralExpressionSyntax)((ObjectExpressionSyntax)list.Items.Last()).Members.Single().Value;
        nested.RawLocation!.Column.ShouldEqual(mapping.IndexOf(":2", StringComparison.Ordinal) + 2);
    }

    [Theory]
    [InlineData("")]
    [InlineData("numbers exact\n")]
    void should_patch_only_the_structured_rhs_in_the_actual_source(string prefix)
    {
        var text = prefix + "module M\n  feature F\n    slice StateChange S\n      specification T\n        when C\n          amount = {\"café\":\"ok\",\"n\":[1]} // keep\n";
        var original = WorkspaceDocument.Create("input", PortablePlayPath.Parse("input.play"), Encoding.UTF8.GetBytes(text));
        var intended = _compiler.Parse(text.Replace("[1]", "[2]", StringComparison.Ordinal)).Value!;
        WorkspaceTriviaPrinter.Print(original, intended).Text.ShouldEqual(text.Replace("[1]", "[2]", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("")]
    [InlineData("numbers exact\n")]
    void should_report_duplicate_and_malformed_structured_rhs_at_main_positions(string prefix)
    {
        const string duplicate = "    amount = {\"n\":[1],\"n\":2}";
        var parsed = _compiler.CompileSpecification(prefix + "specification T\n  when C\n" + duplicate + "\n");
        var error = parsed.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.DuplicateStructuredValueMember);
        error.Location.Column.ShouldEqual(duplicate.LastIndexOf("\"n\"", StringComparison.Ordinal) + 1);
        var malformed = _compiler.CompileSpecification(prefix + "specification T\n  when C\n    amount = {\"n\":[}\n");
        malformed.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidStructuredValue).Location.Column.ShouldEqual(14);
    }
}

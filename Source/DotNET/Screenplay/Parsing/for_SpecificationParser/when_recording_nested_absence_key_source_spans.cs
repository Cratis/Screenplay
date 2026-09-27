// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_SpecificationParser;

public class when_recording_nested_absence_key_source_spans : Specification
{
    const string Source = "specification NoItem\n  then no readmodel InvoiceView for {\"id\":{\"part\":\"old\"},\"scope\":\"fixed\"}";
    LiteralExpressionSyntax _literal = null!;

    void Because()
    {
        var result = new ScreenplayCompiler().CompileSpecification(Source);
        result.Success.ShouldBeTrue();
        var key = (ObjectExpressionSyntax)result.Value!.ThenAbsentReadModels.Single().Key;
        var id = (ObjectExpressionSyntax)key.Members.Single(member => member.Name == "id").Value;
        _literal = (LiteralExpressionSyntax)id.Members.Single(member => member.Name == "part").Value;
    }

    [Fact] void should_record_the_nested_literals_actual_column() => _literal.RawLocation!.Column.ShouldEqual(51);
    [Fact] void should_record_the_nested_literals_authored_length() => _literal.RawLength.ShouldEqual(5);
    [Fact] void should_record_a_scalar_absence_key_source_span()
    {
        const string source = "specification NoItem\n  then no readmodel InvoiceView for \"first\"  // keep this";
        var result = new ScreenplayCompiler().CompileSpecification(source);
        result.Success.ShouldBeTrue();
        var scalar = (LiteralExpressionSyntax)result.Value!.ThenAbsentReadModels.Single().Key;
        scalar.RawLocation!.Column.ShouldEqual(source.Split('\n')[1].IndexOf("\"first\"", StringComparison.Ordinal) + 1);
        scalar.RawLength.ShouldEqual(7);
    }
}

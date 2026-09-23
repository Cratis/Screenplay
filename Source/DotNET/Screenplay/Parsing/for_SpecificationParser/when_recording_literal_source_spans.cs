// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_SpecificationParser;

public class when_recording_literal_source_spans : Specification
{
    const string Source = "module Shop\r\n  feature Orders\r\n    slice StateChange Place\r\n      specification Placing\r\n        when Place\r\n          channel =   \"wé\\\"b\"  // \"web\" 🍰\r\n          lines = [{\"sku\":1}]\r\n";
    PropertyMappingSyntax[] _values = [];

    void Because() => _values = [.. new ScreenplayCompiler().Parse(Source, "Orders.play").Value!.Modules.Single().Features.Single().Slices.Single().Specifications.Single().When!.Values];

    [Fact] void should_record_the_exact_authored_literal() => Text(((LiteralExpressionSyntax)_values[0].Source).RawLocation!, ((LiteralExpressionSyntax)_values[0].Source).RawLength!.Value).ShouldEqual("\"wé\\\"b\"");
    [Fact] void should_record_the_mapping_source_span() => Text(_values[0].SourceLocation!, _values[0].SourceLength!.Value).ShouldEqual("\"wé\\\"b\"");
    [Fact] void should_record_an_opaque_mapping_source_span() => Text(_values[1].SourceLocation!, _values[1].SourceLength!.Value).ShouldEqual("[{\"sku\":1}]");
    [Fact] void should_keep_the_literal_node_location() => _values[0].Source.Location.ShouldEqual(_values[0].Location);

    static string Text(Diagnostics.SourceLocation location, int length) => Source.Split('\n')[location.Line - 1].Substring(location.Column - 1, length);
}

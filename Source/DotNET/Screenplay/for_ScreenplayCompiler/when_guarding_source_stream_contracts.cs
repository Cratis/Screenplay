// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_guarding_source_stream_contracts : given.a_compiler
{
    [Theory]
    [InlineData("module Broken\n  description\n    ```text\neventsource Account", true)]
    [InlineData("module Broken\n  description\n    ```text\neventsource Account\n    ```", false)]
    [InlineData("module Broken\n  feature F\n    slice StateChange S\n      command C\n        invalid directive", false)]
    [InlineData("module Broken\n  feature F\n    slice StateChange S\n      command C\n        handler\n          ```csharp\neventsource Account", true)]
    void should_distinguish_unclosed_fences_from_closed_fences_and_command_body_errors(string text, bool unknown)
    {
        var parsed = _compiler.Parse(text, "other.play");
        EventSourceReadConfidence.HasUnknownExtent(parsed.Diagnostics).ShouldEqual(unknown);
        if (unknown) parsed.Diagnostics.Any(diagnostic => diagnostic.Code == "PLAY0164" && diagnostic.Location.Path == "other.play").ShouldBeTrue();
    }

    [Theory]
    [InlineData("𐐀")]
    [InlineData("\uD801")]
    [InlineData("\uDC00")]
    [InlineData("\u0903")]
    [InlineData("\u00B2")]
    void should_reject_names_outside_the_supported_utf16_word_alphabet(string suffix)
    {
        var name = "Account" + suffix;
        _compiler.Parse($"eventsource {name}\n  stream Transactions").Diagnostics.Any(diagnostic => diagnostic.Code == "PLAY0503").ShouldBeTrue();
        _compiler.Parse($"eventsource Account\n  stream {name}").Diagnostics.Any(diagnostic => diagnostic.Code == "PLAY0503").ShouldBeTrue();
        var source = _compiler.Parse("eventsource Account\n  stream Transactions").Value!.EventSources.Single();
        foreach (var node in new SyntaxNode[] { source with { Name = name }, source.Streams.Single() with { Name = name }, new CommandStreamSyntax(name, "Transactions", source.Location), new CommandStreamSyntax("Account", name, source.Location) })
        {
            Catch.Exception(() => SyntaxJson.Serialize(node)).ShouldBeOfExactType<InvalidSyntaxJson>();
        }
    }

    [Theory]
    [InlineData("ß")]
    [InlineData("α")]
    [InlineData("\u01C5")]
    [InlineData("\u02B0")]
    [InlineData("\u4E00")]
    [InlineData("\u0301")]
    [InlineData("\u0661")]
    [InlineData("\u203F")]
    void should_keep_each_supported_bmp_word_category(string suffix)
    {
        var result = _compiler.Parse($"eventsource Account{suffix}\n  stream Transactions{suffix}");
        result.Success.ShouldBeTrue();
        SyntaxJson.StructurallyEqual(result.Value!, SyntaxJson.Deserialize(SyntaxJson.Serialize(result.Value!))).ShouldBeTrue();
    }

    [Theory]
    [InlineData("null")]
    [InlineData("[null]")]
    [InlineData("{}")]
    [InlineData("[{}]")]
    [InlineData("[42]")]
    [InlineData("[{\"kind\":\"TypeRefSyntax\",\"name\":\"String\",\"isOptional\":false,\"isCollection\":false}]")]
    void should_reject_malformed_source_and_stream_collections_when_reading_json(string malformed)
    {
        var application = JsonNode.Parse(SyntaxJson.Serialize(_compiler.Parse("eventsource Account\n  stream Transactions").Value!).GetRawText())!;
        var source = application["eventSources"]![0]!;
        foreach (var (node, member) in new[] { (application, "eventSources"), (source, "streams") })
        {
            var invalid = node.DeepClone();
            invalid[member] = JsonNode.Parse(malformed);
            Catch.Exception(() => SyntaxJson.Deserialize(JsonSerializer.SerializeToElement(invalid))).ShouldBeOfExactType<InvalidSyntaxJson>();
        }
    }

    [Fact]
    void should_reject_null_collection_elements_when_writing_and_keep_legacy_omissions()
    {
        var application = _compiler.Parse("eventsource Account\n  stream Transactions").Value!;
        var source = application.EventSources.Single();
        foreach (var node in new SyntaxNode[] { application with { EventSources = null! }, application with { EventSources = [null!] }, source with { Streams = null! }, source with { Streams = [null!] } })
        {
            Catch.Exception(() => SyntaxJson.Serialize(node)).ShouldBeOfExactType<InvalidSyntaxJson>();
        }
        var empty = _compiler.Parse(string.Empty).Value!;
        var old = JsonNode.Parse(SyntaxJson.Serialize(empty).GetRawText())!;
        old.AsObject().Remove("eventSources");
        SyntaxJson.Serialize(SyntaxJson.Deserialize(JsonSerializer.SerializeToElement(old))).GetRawText().ShouldEqual(SyntaxJson.Serialize(empty).GetRawText());
    }
}

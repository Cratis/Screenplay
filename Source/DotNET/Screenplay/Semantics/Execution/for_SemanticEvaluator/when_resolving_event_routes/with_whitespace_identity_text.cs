// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_resolving_event_routes;

public class with_whitespace_identity_text : given.event_route_models
{
    [Theory]
    [InlineData("   ")]
    [InlineData("\t\n")]
    [InlineData("\u0085")]
    [InlineData("\u00a0")]
    [InlineData("\u3000")]
    void should_preserve_whitespace_as_an_event_source_and_stream_id(string text)
    {
        var request = SemanticExecutionRequest.Create(Command.Id,
            [new(Identity.Id, SemanticValue.Text(text)), new(Key.Id, SemanticValue.Text(text))], []);
        var result = new SemanticEvaluator().Execute(Plan(), SemanticWorld.Empty, request);
        result.ShouldBeOfExactType<SemanticAccepted>();
        var fact = ((SemanticAccepted)result).Facts.Single();
        fact.Destination.ShouldEqual(SemanticValue.Text(text));
        fact.Route!.StreamId.ShouldEqual(text);
    }

    [Fact]
    void should_preserve_whitespace_in_composite_stream_id_parts()
    {
        var stream = Stream with { StreamIdType = null, StreamIdParts = [new("key", Text), new("other", Text)] };
        var command = Command with
        {
            Route = new(Source.Id, Stream.Id)
            {
                StreamIdParts = [new("key", Property(Key)), new("other", SemanticExpression.FromValue(SemanticValue.Text("\u0085")))]
            }
        };
        var result = new SemanticEvaluator().Execute(Plan(command, Source with { Streams = [stream] }), SemanticWorld.Empty, Request(SemanticValue.Text("   ")));
        result.ShouldBeOfExactType<SemanticAccepted>();
        ((SemanticAccepted)result).Facts.Single().Route!.StreamId.ShouldEqual("   |\u0085");
    }
}

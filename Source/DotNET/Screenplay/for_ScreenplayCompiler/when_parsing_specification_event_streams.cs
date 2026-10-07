// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_parsing_specification_event_streams : given.a_compiler
{
    const string Source = """
        specification Routed
          given E
            for "other"
            stream Account.Transactions
              streamId = "p-1:2026-10"
            stream = 5
          when append E
            for "other"
            stream Account.Profile
          then E
            no stream
            streamId = 6
        """;

    CompilationResult<SpecificationSyntax> _result;

    void Because() => _result = _compiler.CompileSpecification(Source);

    [Fact] void should_accept_event_routes() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_read_the_keyed_route() => _result.Value.Given.Single().Stream!.Stream.ShouldEqual("Transactions");
    [Fact] void should_read_the_unkeyed_route() => _result.Value.WhenAppended!.Stream!.Stream.ShouldEqual("Profile");
    [Fact] void should_read_the_unrouted_marker() => _result.Value.ThenEvents.Single().NoStream.ShouldNotBeNull();
    [Fact] void should_keep_stream_as_payload() => _result.Value.Given.Single().Values.Single().Property.ShouldEqual("stream");
    [Fact] void should_keep_stream_id_as_payload() => _result.Value.ThenEvents.Single().Values.Single().Property.ShouldEqual("streamId");
}

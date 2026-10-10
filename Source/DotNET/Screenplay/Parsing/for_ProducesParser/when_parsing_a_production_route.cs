// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_ProducesParser;

public class when_parsing_a_production_route : Specification
{
    [Theory]
    [InlineData("produces Recorded\n  stream Account.Transactions")]
    [InlineData("produces when enabled == true\n  Recorded\n    stream Account.Transactions")]
    [InlineData("produces event Recorded\n  stream Account.Transactions\n  value String = value")]
    void should_parse_plain_conditional_and_inline_routes(string production)
    {
        var source = "module Banking\n  feature Posting\n    slice StateChange Record\n      command Record\n" + string.Join('\n', production.Split('\n').Select(line => "        " + line));
        var result = new ScreenplayCompiler().Parse(source);
        result.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        result.Value!.Modules.Single().Features.Single().Slices.Single().Commands.Single().Produces.Single().Stream!.EventSource.ShouldEqual("Account");
    }

    [Fact]
    void should_refuse_two_routes()
    {
        var parsed = new ScreenplayCompiler().Parse("module Banking\n  feature Posting\n    slice StateChange Record\n      command Record\n        produces Recorded\n          stream Account.Transactions\n          stream Account.Notes");
        parsed.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidCommandStream).ShouldBeTrue();
    }

    [Theory]
    [InlineData("stream = value")]
    [InlineData("@stream = value")]
    void should_keep_payload_named_stream_as_a_mapping(string mapping)
    {
        var parsed = new ScreenplayCompiler().Parse("module Banking\n  feature Posting\n    slice StateChange Record\n      command Record\n        produces Recorded\n          " + mapping);
        var production = parsed.Value!.Modules.Single().Features.Single().Slices.Single().Commands.Single().Produces.Single();
        production.Stream.ShouldBeNull();
        production.Mappings.Single().Property.ShouldEqual("stream");
    }

    [Fact]
    void should_refuse_a_reaction_production_route()
    {
        var parsed = new ScreenplayCompiler().Parse("module Banking\n  feature Posting\n    slice Automation Follow\n      reaction Follow\n        when Recorded\n          produces Followed\n            stream Account.Transactions");
        parsed.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.ProductionRouteOutsideCommand).ShouldBeTrue();
    }
}

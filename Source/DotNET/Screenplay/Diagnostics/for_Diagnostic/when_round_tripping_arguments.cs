// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Diagnostics.for_Diagnostic;

public class when_round_tripping_arguments : Specification
{
    readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web);
    Diagnostic _original;
    Diagnostic _copy;
    Diagnostic _legacy;
    Diagnostic _reordered;

    void Establish()
    {
        _original = Diagnostic.Error("PLAY0524", "Missing", new(1, 1), DiagnosticArguments.Empty
            .With("specification", DiagnosticArgument.Declaration("specification", ["M", "F", "S", "Authored"]))
            .With("step", DiagnosticArgument.Keyword("when"))
            .With("stepIndex", DiagnosticArgument.Index(0))
            .With("property", DiagnosticArgument.Text("amount")));
        _reordered = _original with { Arguments = new(_original.Arguments.Reverse()) };
    }

    void Because()
    {
        _copy = JsonSerializer.Deserialize<Diagnostic>(JsonSerializer.Serialize(_original, _options), _options)!;
        _legacy = JsonSerializer.Deserialize<Diagnostic>("{\"severity\":2,\"code\":\"PLAY0001\",\"message\":\"Legacy\",\"location\":{\"line\":1,\"column\":1}}", _options)!;
    }

    [Fact] void should_round_trip_the_typed_values() => _copy.ShouldEqual(_original);
    [Fact] void should_preserve_hashes_across_round_trips() => _copy.GetHashCode().ShouldEqual(_original.GetHashCode());
    [Fact] void should_ignore_argument_order_for_equality() => _reordered.ShouldEqual(_original);
    [Fact] void should_ignore_argument_order_for_hashing() => _reordered.GetHashCode().ShouldEqual(_original.GetHashCode());
    [Fact] void should_read_old_diagnostics_as_an_empty_map() => _legacy.Arguments.ShouldBeEmpty();
    [Fact] void should_write_an_empty_map_as_an_object() => JsonDocument.Parse(JsonSerializer.Serialize(_legacy, _options)).RootElement.GetProperty("arguments").GetRawText().ShouldEqual("{}");
    [Fact] void should_preserve_the_four_parameter_deconstructor() => Deconstruct(_legacy).ShouldEqual("PLAY0001");
    [Fact] void should_keep_the_three_argument_helpers() => Diagnostic.Warning("PLAY0001", "Legacy", new(1, 1)).Arguments.ShouldBeEmpty();

    static string Deconstruct(Diagnostic diagnostic)
    {
        var (_, code, _, _) = diagnostic;
        return code;
    }
}

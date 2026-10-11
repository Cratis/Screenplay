// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_event_routes;

public class admission : given.a_semantic_binder
{
    const string Prefix = "concept Key : Int\neventsource Account\n  identifier String\n  stream Ledger\n    streamId Key\nmodule M\n  feature F\n    slice StateChange S\n";
    const string Command = "      command C\n        id String identifier\n        key Key\n        stream Account.Ledger\n          streamId = key\n        produces event E\n          value Decimal = 9007199254740991\n";

    [Theory]
    [InlineData(9007199254740991L)]
    [InlineData(9007199254740990L)]
    [InlineData(-9007199254740991L)]
    [InlineData(-9007199254740990L)]
    void should_preserve_integers_adjacent_to_the_bounds(long value)
    {
        var result = Bind(Prefix + Command.Replace("streamId = key", $"streamId = {value}"));
        result.Success.ShouldBeTrue();
        var route = result.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single().Commands.Single().Route!;
        ((SemanticNumberValue)((SemanticValueExpression)route.StreamId!).Value).Value.ShouldEqual(value);
    }

    [Fact]
    void should_select_v10_for_exact_payload_literal_lowering()
    {
        var result = Bind(Prefix + Command);
        result.Success.ShouldBeTrue();
        result.Value!.Model.LanguageVersion.ShouldEqual(LanguageVersion.V10);
        result.Value.Model.SemanticVersion.ShouldEqual(SemanticVersion.V10);
        var command = result.Value.Model.Application.Modules.Single().Features.Single().Slices.Single().Commands.Single();
        ((SemanticNumberValue)((SemanticValueExpression)command.Produces.Single().Mappings.Single().Source).Value).Value.ShouldEqual(9007199254740991m);
    }

    [Fact]
    void should_keep_existing_payload_lowering_when_the_exact_value_is_unchanged()
    {
        var result = Bind(Prefix + Command.Replace("9007199254740991", "9007199254740990"));
        result.Success.ShouldBeTrue();
        result.Value!.Model.LanguageVersion.ShouldEqual(LanguageVersion.V8);
        result.Value.Model.SemanticVersion.ShouldEqual(SemanticVersion.V8);
        var command = result.Value.Model.Application.Modules.Single().Features.Single().Slices.Single().Commands.Single();
        ((SemanticNumberValue)((SemanticValueExpression)command.Produces.Single().Mappings.Single().Source).Value).Value.ShouldEqual(9007199254740990m);
    }

    [Theory]
    [InlineData("key.part", DiagnosticCodes.UnsupportedSemanticSyntax)]
    [InlineData("generatedKey", DiagnosticCodes.InvalidSemanticBinding)]
    [InlineData("optionalKey", DiagnosticCodes.InvalidSemanticBinding)]
    [InlineData("keys", DiagnosticCodes.InvalidSemanticBinding)]
    void should_refuse_mappings_that_are_not_admitted_direct_inputs(string mapping, string code)
    {
        var command = Command.Replace("        key Key\n", "        key Key\n        generatedKey GeneratedKey generated\n        optionalKey Key optional\n        keys Key[]\n");
        var result = Bind("concept GeneratedKey : Uuid\n" + Prefix + command.Replace("streamId = key", $"streamId = {mapping}"));
        result.Success.ShouldBeFalse();
        result.Diagnostics.Any(diagnostic => diagnostic.Code == code).ShouldBeTrue();
    }

    [Fact]
    void should_refuse_a_routed_handler_command()
    {
        var result = Bind(Prefix + Command + "        handler\n          file \"handler.cs\"\n");
        result.Success.ShouldBeFalse();
        result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.UnsupportedSemanticSyntax).ShouldBeTrue();
    }

    [Theory]
    [InlineData("eventsource Other\n  id \"Account\"\n  stream Other\n")]
    [InlineData("eventsource Other\n  id \"stored\"\n  stream Other\neventsource Third\n  id \"stored\"\n  stream Other\n")]
    [InlineData("eventsource Default\n  stream All\n")]
    [InlineData("eventsource Other\n  id \"Default\"\n  stream All\n")]
    [InlineData("eventsource Other\n  stream First\n    id \"Second\"\n  stream Second\n")]
    void should_refuse_colliding_or_reserved_stored_names(string declaration)
    {
        var result = Bind(declaration + Prefix + Command);
        result.Success.ShouldBeFalse();
        result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidSemanticBinding).ShouldBeTrue();
    }

    [Fact]
    void should_report_source_and_stream_descriptions()
    {
        var result = Bind(Prefix.Replace("  identifier String", "  description \"Source\"\n  identifier String").Replace("    streamId Key", "    description \"Stream\"\n    streamId Key") + Command);
        result.Success.ShouldBeTrue();
        result.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.ReportOnlySemanticSyntax && diagnostic.Message.Contains("description", StringComparison.Ordinal)).ShouldEqual(2);
    }

    [Fact]
    void should_select_the_event_routes_version_only_when_used()
    {
        Bind(Prefix + Command.Replace("9007199254740991", "9007199254740990")).Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V8);
        Bind("eventsource Account\n  stream All\n").Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V8);
        Bind("module M\n  feature F\n    slice StateChange S\n      command C\n        value String\n").Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V1);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_bind_routed_history_independently_of_producer_types(bool producer)
    {
        var source = "eventsource Account\n  identifier String\n  stream All\nmodule M\n  feature F\n    slice StateView S\n      event E\n        value String\n";
        if (producer) source += "      command C\n        id Uuid identifier\n        value String\n        produces E\n          for id\n          value = value\n";
        source += "      specification History\n        given E\n          for \"other\"\n          stream Account.All\n          value = \"before\"\n        when append E\n          for \"other\"\n          stream Account.All\n          value = \"after\"\n        then E\n          stream Account.All\n          value = \"after\"\n";
        var result = Bind(source);
        Assert.True(result.Success, string.Join('\n', result.Diagnostics.Select(diagnostic => diagnostic.Message)));
        var spec = result.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single().Specifications.Single();
        spec.GivenEvents.Single().EventSource!.Type.ShouldEqual(SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text));
        spec.WhenAppended!.Route.ShouldNotBeNull();
        spec.ThenEvents.Single().Route.ShouldNotBeNull();
    }

    [Fact]
    void should_order_route_parts_by_the_declaration()
    {
        var source = Prefix.Replace("    streamId Key", "    streamId\n      first Key\n      second String") + Command.Replace("          streamId = key", "          streamId\n            second = id\n            first = key");
        var result = Bind(source);
        result.Success.ShouldBeTrue();
        var route = result.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single().Commands.Single().Route!;
        route.StreamIdParts.Select(part => part.Part).ShouldEqual(["first", "second"]);
    }

    [Fact]
    void should_preserve_fixture_route_integers_with_exact_v10_payload_lowering()
    {
        var result = Bind(Prefix + Command.Replace("        stream Account.Ledger\n          streamId = key\n", string.Empty) + "      specification History\n        given E\n          for \"other\"\n          stream Account.Ledger\n            streamId = 9007199254740991\n          value = 1\n        when C\n          id = \"other\"\n          key = 1\n        then E\n          no stream\n          value = 9007199254740991\n");
        result.Success.ShouldBeTrue();
        result.Value!.Model.LanguageVersion.ShouldEqual(LanguageVersion.V10);
        result.Value.Model.SemanticVersion.ShouldEqual(SemanticVersion.V10);
        var spec = result.Value.Model.Application.Modules.Single().Features.Single().Slices.Single().Specifications.Single();
        ((SemanticNumberValue)spec.GivenEvents.Single().Route!.StreamId!).Value.ShouldEqual(9007199254740991m);
        spec.ThenEvents.Single().Unrouted.ShouldBeTrue();
        ((SemanticNumberValue)spec.ThenEvents.Single().Values.Single().Value).Value.ShouldEqual(9007199254740991m);
    }
}

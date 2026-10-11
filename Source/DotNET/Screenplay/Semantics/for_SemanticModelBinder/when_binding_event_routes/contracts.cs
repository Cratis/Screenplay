// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_event_routes;

public class contracts : given.a_semantic_binder
{
    const string Source = "concept Key : Int\neventsource Account\n  identifier String\n  stream Ledger\n    streamId Key\nmodule M\n  feature F\n    slice StateChange S\n      command C\n        id String identifier\n        key Key\n        payload Decimal\n        stream Account.Ledger\n          streamId = key\n        produces event E\n          value Decimal = payload\n      specification History\n        when C\n          id = \"other\"\n          key = 9007199254740991\n          payload = 9007199254740991\n        then E\n          stream Account.Ledger\n            streamId = 9007199254740991\n          value = 9007199254740991\n";

    [Fact]
    void should_lower_route_inputs_and_v10_payload_literals_losslessly()
    {
        var result = Bind(Source);
        Assert.True(result.Success, string.Join('\n', result.Diagnostics.Select(diagnostic => diagnostic.Message)));
        result.Value!.Model.LanguageVersion.ShouldEqual(LanguageVersion.V10);
        result.Value.Model.SemanticVersion.ShouldEqual(SemanticVersion.V10);
        var slice = result.Value.Model.Application.Modules.Single().Features.Single().Slices.Single();
        var command = slice.Commands.Single();
        var inputs = slice.Specifications.Single().When!.Values;
        ((SemanticNumberValue)inputs.Single(value => value.TargetProperty == command.Properties.Single(property => property.Name == "key").Id).Value).Value.ShouldEqual(9007199254740991m);
        ((SemanticNumberValue)inputs.Single(value => value.TargetProperty == command.Properties.Single(property => property.Name == "payload").Id).Value).Value.ShouldEqual(9007199254740991m);
    }

    [Fact]
    void should_keep_v8_for_lossless_route_inputs_without_changed_payload_lowering()
    {
        var result = Bind(Source.Replace("payload = 9007199254740991", "payload = 42").Replace("value = 9007199254740991", "value = 42"));
        result.Success.ShouldBeTrue();
        result.Value!.Model.LanguageVersion.ShouldEqual(LanguageVersion.V8);
        result.Value.Model.SemanticVersion.ShouldEqual(SemanticVersion.V8);
        var slice = result.Value.Model.Application.Modules.Single().Features.Single().Slices.Single();
        var command = slice.Commands.Single();
        var inputs = slice.Specifications.Single().When!.Values;
        ((SemanticNumberValue)inputs.Single(value => value.TargetProperty == command.Properties.Single(property => property.Name == "key").Id).Value).Value.ShouldEqual(9007199254740991m);
        ((SemanticNumberValue)inputs.Single(value => value.TargetProperty == command.Properties.Single(property => property.Name == "payload").Id).Value).Value.ShouldEqual(42m);
    }

    [Fact]
    void should_not_admit_v10_for_only_already_lossless_route_inputs()
    {
        var result = Bind(Source.Replace("payload = 9007199254740991", "payload = 42").Replace("value = 9007199254740991", "value = 42"));
        result.Success.ShouldBeTrue();
        Catch.Exception(() => ExecutableSemanticModel.Create(LanguageVersion.V10, SemanticVersion.V10, result.Value!.Model.Application)).ShouldBeOfExactType<InvalidSemanticContract>();
    }

    [Fact]
    void should_register_source_and_stream_catalog_addresses()
    {
        var result = Bind(Source);
        result.Success.ShouldBeTrue();
        var application = result.Value!.Model.Application;
        var index = SemanticCompilationIndex.Create(application, _applicationIdentity);
        var address = SemanticAddress.ForEventSource(_applicationIdentity, "Account");
        index.Declarations[address].ShouldEqual(application.EventSources.Single().Id);
        index.Declarations[SemanticAddress.ForEventStream(address, "Ledger")].ShouldEqual(application.EventSources.Single().Streams.Single().Id);
        result.Value.SourceMap.Entries.Any(entry => entry.SemanticId == application.EventSources.Single().Id).ShouldBeTrue();
    }

    [Fact]
    void should_refuse_a_fixture_without_an_unambiguous_source_identifier_type()
    {
        var result = Bind("eventsource Account\n  stream All\nmodule M\n  feature F\n    slice StateView S\n      event E\n        value String\n      specification History\n        when append E\n          for \"other\"\n          stream Account.All\n          value = \"after\"\n        then E\n          value = \"after\"\n");
        result.Success.ShouldBeFalse();
        result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidSemanticBinding && diagnostic.Message.Contains("identifier on its event source", StringComparison.Ordinal)).ShouldBeTrue();
    }

    [Fact]
    void should_refuse_an_expected_route_that_contradicts_its_only_producer()
    {
        var result = Bind(Source.Replace("  stream Ledger", "  stream Other\n    streamId Key\n  stream Ledger").Replace("          stream Account.Ledger", "          stream Account.Other"));
        result.Success.ShouldBeFalse();
        result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.SpecificationStreamContradictsCommand).ShouldBeTrue();
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("String", "streamId = \"literal\"")]
    [InlineData("Uuid", "streamId = \"3FA85F64-5717-4562-B3FC-2C963F66AFA6\"")]
    void should_bind_scalar_and_unkeyed_routes(string type, string mapping)
    {
        var declaration = type.Length == 0 ? string.Empty : $"    streamId {type}\n";
        var route = mapping.Length == 0 ? string.Empty : $"          {mapping}\n";
        var result = Bind("eventsource Account\n  stream All\n" + declaration + "module M\n  feature F\n    slice StateChange S\n      command C\n        id String identifier\n        stream Account.All\n" + route);
        result.Success.ShouldBeTrue();
        result.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single().Commands.Single().Route.ShouldNotBeNull();
    }

    [Theory]
    [InlineData("streamId = 1\n")]
    [InlineData("streamId\n            first = 1\n")]
    [InlineData("streamId\n            first = 1\n            unknown = 2\n")]
    void should_refuse_incomplete_composite_mappings(string mapping)
    {
        var result = Bind("concept Key : Int\neventsource Account\n  stream Ledger\n    streamId\n      first Key\n      second Key\nmodule M\n  feature F\n    slice StateChange S\n      command C\n        stream Account.Ledger\n          " + mapping);
        result.Success.ShouldBeFalse();
        result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidSemanticBinding).ShouldBeTrue();
    }
}

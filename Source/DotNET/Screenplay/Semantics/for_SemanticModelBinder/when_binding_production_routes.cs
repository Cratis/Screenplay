// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_production_routes : given.a_semantic_binder
{
    const string Source = "eventsource Account\n  identifier String\n  stream Notes\n    streamId String\nmodule M\n  feature F\n    slice StateChange S\n      command Record\n        id String identifier\n        period String\n        produces event Recorded\n          stream Account.Notes\n            streamId = period\n";

    [Fact]
    void should_select_v10_only_when_a_production_overrides_a_route()
    {
        var result = Bind(Source);
        Assert.True(result.Success, string.Join(';', result.Diagnostics.Select(diagnostic => diagnostic.Message)));
        result.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V10);
        var command = result.Value.Model.Application.Modules.Single().Features.Single().Slices.Single().Commands.Single();
        command.Route.ShouldBeNull();
        command.Produces.Single().Route.ShouldNotBeNull();
        Bind(Source.Replace("          stream Account.Notes\n            streamId = period\n", string.Empty, StringComparison.Ordinal)).Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V8);
    }

    [Fact]
    void should_refuse_generated_override_inputs()
    {
        var source = Source.Replace("eventsource Account", "concept Period : Uuid\neventsource Account", StringComparison.Ordinal).Replace("streamId String", "streamId Period", StringComparison.Ordinal).Replace("period String", "period Period generated", StringComparison.Ordinal);
        Bind(source).Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidSemanticBinding).ShouldBeTrue();
    }

    [Fact]
    void should_refuse_path_override_inputs()
    {
        var source = "type Input\n  period String\n" + Source.Replace("period String", "input Input", StringComparison.Ordinal).Replace("streamId = period", "streamId = input.period", StringComparison.Ordinal);
        Bind(source).Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.UnsupportedSemanticSyntax).ShouldBeTrue();
    }

    [Fact]
    void should_allow_an_override_destination_with_its_own_nominal_type()
    {
        var source = "concept LedgerId : Uuid\n" + Source.Replace("identifier String", "identifier LedgerId", StringComparison.Ordinal).Replace("        period String", "        period String\n        ledgerId LedgerId", StringComparison.Ordinal).Replace("          stream Account.Notes", "          for ledgerId\n          stream Account.Notes", StringComparison.Ordinal);
        var result = Bind(source);
        Assert.True(result.Success, string.Join(';', result.Diagnostics.Select(diagnostic => diagnostic.Message)));
    }
}

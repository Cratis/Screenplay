// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_an_invocation_from_the_event_source : given.a_semantic_binder
{
    const string Source =
        """
        concept OrderId : String
        module Orders
          feature Fulfilment
            slice StateChange PlaceOrder
              command PlaceOrder
                orderId OrderId identifier
                produces OrderPlaced
              event OrderPlaced
            slice Automation StartPicking
              event PickingStarted
              command StartPicking
                orderId OrderId identifier
                produces PickingStarted
              reaction PickOnPlacement
                when OrderPlaced
                  invokes StartPicking
                    orderId = $eventSourceId
        """;

    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind(Source);

    [Fact] void should_bind() => _result.Success.ShouldBeTrue();
    [Fact] void should_keep_esm_v6() => _result.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V6);
    [Fact] void should_use_the_existing_typed_context_expression() => Mapping(_result.Value!.Model).ShouldEqual(new SemanticEventContextExpression(SemanticEventContextValueKind.EventSourceIdentity, SemanticTypeReference.ForConcept(_result.Value.Model.Application.Concepts.Single().Id)));
    [Fact] void should_round_trip_the_existing_wire_shape() => Mapping(SemanticModelSerializer.Deserialize(SemanticModelSerializer.Serialize(_result.Value!.Model))).ShouldEqual(Mapping(_result.Value.Model));

    [Theory]
    [InlineData("when Startup")]
    [InlineData("every 1 day")]
    [InlineData("when ExternalSignal")]
    void should_reject_occurrences_without_an_event_source(string trigger)
    {
        var result = Bind("trigger ExternalSignal\n" + Source.Replace("when OrderPlaced", trigger, StringComparison.Ordinal));
        result.Success.ShouldBeFalse();
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidSemanticBinding && diagnostic.Message.Contains("$eventSourceId", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("String?")]
    [InlineData("String[]")]
    void should_reject_non_required_scalar_targets(string type)
    {
        var result = Bind(Source.Replace("orderId OrderId identifier", $"orderId {type}", StringComparison.Ordinal));
        result.Success.ShouldBeFalse();
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidSemanticBinding && diagnostic.Message.Contains("$eventSourceId", StringComparison.Ordinal));
    }

    [Fact]
    void should_bind_the_event_context_alias_to_the_same_expression()
    {
        var alias = Bind(Source.Replace("$eventSourceId", "$eventContext.eventSourceId", StringComparison.Ordinal));
        alias.Success.ShouldBeTrue();
        Mapping(alias.Value!.Model).ShouldEqual(Mapping(_result.Value!.Model));
    }

    [Fact]
    void should_bind_both_spellings_in_the_shared_conformance_vector()
    {
        var result = Bind(File.ReadAllText(SharedVector()));
        result.Success.ShouldBeTrue();
        var mappings = result.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single(slice => slice.Name == "Picking").Reactions.Single().Triggers.Single().Invokes.SelectMany(invocation => invocation.Mappings).ToArray();
        mappings.Length.ShouldEqual(2);
        mappings.Select(mapping => mapping.Source).Distinct().Count().ShouldEqual(1);
        mappings[0].Source.ShouldBeOfExactType<SemanticEventContextExpression>();
    }

    static string SharedVector([CallerFilePath] string path = "") => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path)!, "../../../../Screenplay/Compiler/Conformance/reaction-event-source.play"));

    static SemanticExpression Mapping(ExecutableSemanticModel model) => model.Application.Modules.Single().Features.Single().Slices.Single(slice => slice.Name == "StartPicking").Reactions.Single().Triggers.Single().Invokes.Single().Mappings.Single().Source;
}

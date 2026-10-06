// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner.given;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator;

public class when_combining_generated_and_allocated_destinations : a_v6_scenario
{
    const string Generated = "11111111-1111-1111-1111-111111111111";
    const string Allocated = "22222222-2222-2222-2222-222222222222";

    void Establish() => Compile("concept Id : Uuid\nmodule Billing\n  feature F\n    slice StateChange S\n      command C\n        id Id generated identifier\n        produces A\n          for id\n        produces B\n        returns id\n      event A\n      event B");

    SemanticCommand Command => _plan.Commands.Values.Single();
    SemanticProperty Identifier => Command.Properties.Single();
    SemanticExecutionRequest Request => SemanticExecutionRequest.Create(Command.Id, [], []) with
    {
        GeneratedValues = [new(Identifier.Id, SemanticValue.Text(Generated))]
    };

    SemanticExecutionPlan Plan(bool programmaticDefault)
    {
        if (!programmaticDefault) return _plan;
        var application = _plan.Model.Application;
        var module = application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        var command = Command with { Destination = new(Identifier.Type, SemanticExpression.Property(SemanticExpressionRootKind.Command, Identifier.Id)) };
        application = application with { Modules = [module with { Features = [feature with { Slices = [slice with { Commands = [command] }] }] }] };
        var model = ExecutableSemanticModel.Create(_plan.Model.LanguageVersion, _plan.Model.SemanticVersion, application);
        return SemanticExecutionPlan.Compile(model).Plan!;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_require_allocation_for_the_plain_sibling(bool programmaticDefault)
    {
        var result = new SemanticEvaluator().Execute(Plan(programmaticDefault), SemanticWorld.Empty, Request);
        ((SemanticUnsupported)result).Capability.ShouldEqual(SemanticExecutionCapability.IdentityAllocation);
        result.World.Facts.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_route_each_production_through_its_own_channel(bool programmaticDefault)
    {
        var request = Request with
        {
            AllocatedIdentities = ImmutableDictionary<SemanticId, SemanticValue>.Empty.Add(Command.Id, SemanticValue.Text(Allocated)),
            AllocatedEventSourceType = Identifier.Type
        };
        var result = (SemanticAccepted)new SemanticEvaluator().Execute(Plan(programmaticDefault), SemanticWorld.Empty, request);
        result.Facts.Single(fact => fact.EventContract == _plan.Events.Values.Single(value => value.Name == "A").Id).Destination.ShouldEqual(SemanticValue.Text(Generated));
        result.Facts.Single(fact => fact.EventContract == _plan.Events.Values.Single(value => value.Name == "B").Id).Destination.ShouldEqual(SemanticValue.Text(Allocated));
        ((SemanticScalarExecutionResponse)result.Response!).Value.ShouldEqual(SemanticValue.Text(Generated));
    }

    [Fact]
    void should_omit_the_ambiguous_command_default_in_canonical_bytes()
    {
        Command.Destination.ShouldBeNull();
        var bytes = SemanticModelSerializer.Serialize(_plan.Model);
        using var json = JsonDocument.Parse(bytes);
        var command = json.RootElement.GetProperty("application").GetProperty("modules")[0].GetProperty("features")[0].GetProperty("slices")[0].GetProperty("commands")[0];
        command.TryGetProperty("destination", out _).ShouldBeFalse();
        command.GetProperty("produces")[1].GetProperty("destination").ValueKind.ShouldEqual(JsonValueKind.Null);
        SemanticModelSerializer.Deserialize(bytes).Revision.ShouldEqual(_plan.Model.Revision);
    }
}

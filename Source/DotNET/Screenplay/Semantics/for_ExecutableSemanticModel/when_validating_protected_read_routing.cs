// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.for_ExecutableSemanticModel;

public class when_validating_protected_read_routing : Specification
{
    [Fact]
    void should_refuse_a_handled_event_routed_by_a_command_producer()
    {
        var model = canonical_serialization_golden_vectors.CreateSemanticModelV25();
        var application = ChangeSlices(model.Application, slice => slice with
        {
            Commands = [.. slice.Commands.Select(command => command with
            {
                Produces = [command.Produces[0] with { Route = new(command.Id, command.Id) }]
            })]
        });
        Refusal(application, "NamedSourceOrStream");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_refuse_a_handled_event_routed_by_a_reaction_including_a_refusal_branch(bool refusalBranch)
    {
        var model = canonical_serialization_golden_vectors.CreateSemanticModelV25();
        var command = model.Application.Modules.Single().Features.Single().Slices.SelectMany(slice => slice.Commands).Single();
        var produced = command.Produces.Single() with { Route = new(command.Id, command.Id) };
        var sliceAddress = SemanticAddress.ForSlice(ApplicationIdentity.Create("Projects"), "M", "F", "Follow");
        var reactionId = SemanticId.Create(SemanticAddress.ForReaction(sliceAddress, "Follow"));
        var trigger = new SemanticReactionTrigger(SemanticReactionTriggerKind.Event)
        {
            Source = produced.EventContract,
            Produces = refusalBranch ? [] : [produced],
            Invokes = refusalBranch ? [new(command.Id, []) { OnRefused = [new("any", null, [produced])] }] : []
        };
        var automation = new SemanticSlice(SemanticId.Create(sliceAddress), "Follow", SemanticSliceKind.Automation, [], [], [], [], [], [])
        {
            Reactions = [new(reactionId, "Follow", [trigger])]
        };
        Refusal(AddSlice(model.Application, automation), "NamedSourceOrStream");
    }

    static SemanticApplication AddSlice(SemanticApplication application, SemanticSlice slice) => application with
    {
        Modules = [.. application.Modules.Select(module => module with
        {
            Features = [.. module.Features.Select(feature => feature with { Slices = feature.Slices.Add(slice) })]
        })]
    };

    static SemanticApplication ChangeSlices(SemanticApplication application, Func<SemanticSlice, SemanticSlice> transform) => application with
    {
        Modules = [.. application.Modules.Select(module => module with
        {
            Features = [.. module.Features.Select(feature => feature with { Slices = [.. feature.Slices.Select(transform)] })]
        })]
    };

    static void Refusal(SemanticApplication application, string reason)
    {
        var error = Catch.Exception(() => ExecutableSemanticModel.Create(LanguageVersion.V25, SemanticVersion.V25, application));
        error.ShouldBeOfExactType<InvalidSemanticContract>();
        error.Message.ShouldContain($"unprotectable: {reason}.");
    }
}

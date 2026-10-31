// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelValidator.when_validating_handler_result_v2;

public class when_checking_the_result_contract : for_SemanticModelBinder.when_binding_possible_events.given.a_possible_events_binder
{
    [Fact] void should_accept_v35() => Create(command => command).SemanticVersion.ShouldEqual(SemanticVersion.V35);
    [Fact] void should_refuse_v34() => Refuse(command => command, true);
    [Fact] void should_refuse_legacy_productions() => Refuse(command => command with { Produces = command.Handler.PossibleEvents });
    [Fact] void should_refuse_an_empty_result_set() => Refuse(command => command with { Handler = command.Handler with { PossibleEvents = [] } });
    [Fact] void should_refuse_possible_events_on_v1() => Refuse(command => command with { Handler = command.Handler with { ResultVersion = 1 }, Produces = command.Handler.PossibleEvents });
    [Fact] void should_refuse_unknown_events() => Refuse(command => ChangeEntry(command, entry => entry with { EventContract = command.Id }));
    [Fact] void should_refuse_one_of_a_scalar() => Refuse(command => ChangeEntry(command, entry => entry with { Destination = new SemanticOneOfExpression(command.Properties.First(property => !property.Type.IsCollection).Id) }));
    [Fact] void should_refuse_duplicate_events() => Refuse(command => command with { Handler = command.Handler with { PossibleEvents = [command.Handler.PossibleEvents[0], command.Handler.PossibleEvents[0]] } });

    [Fact] void should_refuse_a_scalar_destination_from_a_collection() => Refuse(command => ChangeEntry(command, entry => entry with { Destination = new SemanticResolvedExpression(SemanticExpressionRootKind.Command, SemanticExpressionSourceKind.Property, command.Properties.First(property => property.Type.IsCollection).Id) }));
    [Fact] void should_refuse_v35_without_a_v2_handler() => Refuse(command => command with { Handler = command.Handler with { ResultVersion = 1, PossibleEvents = [] }, Produces = [command.Handler.PossibleEvents[0]] });

    static SemanticCommand ChangeEntry(SemanticCommand command, Func<SemanticProducedEvent, SemanticProducedEvent> change) =>
        command with { Handler = command.Handler with { PossibleEvents = [change(command.Handler.PossibleEvents[0]), .. command.Handler.PossibleEvents.Skip(1)] } };

    void Refuse(Func<SemanticCommand, SemanticCommand> change, bool earlier = false) => Catch.Exception(() => Create(change, earlier)).ShouldBeOfExactType<InvalidSemanticContract>();

    ExecutableSemanticModel Create(Func<SemanticCommand, SemanticCommand> change, bool earlier = false)
    {
        var model = BindPossible(Source).Value.Model;
        var application = model.Application with
        {
            Modules = [.. model.Application.Modules.Select(module => module with
            {
                Features = [.. module.Features.Select(feature => feature with
                {
                    Slices = [.. feature.Slices.Select(slice => slice with { Commands = [.. slice.Commands.Select(change)] })]
                })]
            })]
        };
        return ExecutableSemanticModel.Create(earlier ? LanguageVersion.V34 : LanguageVersion.V35, earlier ? SemanticVersion.V34 : SemanticVersion.V35, application);
    }
}

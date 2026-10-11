// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

internal static partial class SemanticModelValidator
{
    private sealed partial class ValidationContext
    {
        static bool IsActionEvent(SemanticSpecification specification, HashSet<SemanticId>? producedEvents, SemanticSpecificationEvent value) =>
            producedEvents?.Contains(value.EventContract) == true || specification.WhenAppended?.EventContract == value.EventContract;

        static bool AssertsPresence(SemanticSpecification specification, SemanticSpecificationAbsentReadModel absentState)
        {
            bool IsSame(SemanticSpecificationReadModel present) =>
                present.ReadModel == absentState.ReadModel && SemanticValueRules.AreEqual(present.Key, absentState.Key);
            return specification.ThenReadModels.Any(IsSame) || specification.ThenQueries.Any(query => query.Results.Any(IsSame));
        }

        static void ValidateSpecificationOutcomes(SemanticSpecification specification)
        {
            var acted = HasAction(specification);
            var deniedQuery = IsDeniedQuery(specification, acted);
            var hasRejection = specification.ThenErrors.Length > 0 || specification.ThenDenied;
            var hasSuccessOutcome = HasPositiveOutcome(specification) || (specification.ThenQueries.Length > 0 && !deniedQuery);
            if (hasRejection)
            {
                RequireSingleRejection(specification, hasSuccessOutcome);
            }

            RequireActionForOutcomes(specification, acted, deniedQuery);
            if (!hasRejection && !hasSuccessOutcome)
            {
                throw new InvalidSemanticContract("A success specification must contain at least one success outcome.");
            }
        }

        static void RequireActionForOutcomes(SemanticSpecification specification, bool acted, bool deniedQuery)
        {
            if (!acted && AssertsMoreThanReadState(specification, deniedQuery))
            {
                throw new InvalidSemanticContract("A specification without a command requires a read model or query outcome and cannot assert events or errors.");
            }
        }

        static bool IsDeniedQuery(SemanticSpecification specification, bool acted) =>
            specification.ThenDenied && !acted && specification.ThenQueries.Length == 1 && specification.ThenQueries[0].Results.IsEmpty;

        static void RequireSingleRejection(SemanticSpecification specification, bool hasSuccessOutcome)
        {
            var rejections = specification.ThenErrors.Length + (specification.ThenDenied ? 1 : 0);
            if (rejections != 1 || hasSuccessOutcome)
            {
                throw new InvalidSemanticContract("A rejection specification must contain exactly one rejection and no success outcomes.");
            }
        }

        static bool HasAction(SemanticSpecification specification) =>
            new object?[] { specification.When, specification.WhenAppended, specification.WhenClock, specification.WhenTrigger, specification.WhenCapture }
                .Any(action => action is not null);

        static bool HasReadModelOutcome(SemanticSpecification specification) =>
            specification.ThenReadModels.Length > 0 || specification.ThenAbsentReadModels.Length > 0;

        static bool HasPositiveOutcome(SemanticSpecification specification) =>
            specification.ThenReturns is not null || specification.ThenEvents.Length > 0 || HasReadModelOutcome(specification);

        static bool AssertsMoreThanReadState(SemanticSpecification specification, bool deniedQuery) =>
            specification.ThenErrors.Length > 0 || specification.ThenEvents.Length > 0 ||
            (specification.ThenDenied && !deniedQuery) ||
            (!HasReadModelOutcome(specification) && specification.ThenQueries.Length == 0);

        void ValidateSpecification(SemanticSpecification specification)
        {
            ValidateAutomationSpecification(specification);
            var command = ValidateSpecificationCommand(specification);
            ValidateSpecificationAppend(specification);
            ValidateThenReturns(specification, command);
            ValidateSpecificationEvents(specification, command);
            ValidateSpecificationReadModels(specification);
            foreach (var result in specification.ThenQueries)
            {
                ValidateSpecificationQuery(result);
            }

            RejectDuplicateQueryExpectations(specification.ThenQueries);

            foreach (var error in specification.ThenErrors)
            {
                ValidateSpecificationError(error);
            }

            if (specification.GivenCaller is { } caller)
            {
                RequireObjects(caller.Roles, nameof(caller.Roles), "caller role");
                RequireObjects(caller.Claims, nameof(caller.Claims), "caller claim");
            }

            ValidateSpecificationOutcomes(specification);
        }

        SemanticCommand? ValidateSpecificationCommand(SemanticSpecification specification)
        {
            if (specification.When is null)
            {
                return null;
            }

            if (!_commands.TryGetValue(specification.When.Command, out var command))
            {
                throw new InvalidSemanticContract($"Specification command '{specification.When.Command}' is unresolved.");
            }

            if (specification.When.EventSource is not null)
            {
                if (_semanticVersion == SemanticVersion.V1) throw new InvalidSemanticContract("A specification command event source requires ESM v2.");
                ValidateEventSource(
                    specification.When.EventSource,
                    command.Destination?.Type ?? command.Properties.SingleOrDefault(property => property.IsIdentifier)?.Type);
            }

            ValidatePropertyValues(specification.When.Values, [.. command.Properties.Where(property => !property.IsGenerated)], true, RoutedInputs(command));
            ValidateGeneratedValues(specification.When, command);
            return command;
        }

        void ValidateSpecificationAppend(SemanticSpecification specification)
        {
            if (specification.WhenAppended is not { } appended)
            {
                return;
            }

            if (specification.When is not null) throw new InvalidSemanticContract("A specification cannot state both command and append actions.");
            if (!_events.TryGetValue(appended.EventContract, out var eventContract)) throw new InvalidSemanticContract("An appended specification event is unresolved.");
            ValidatePropertyValues(appended.Values, eventContract.Properties, true);
            ValidateFixtureRoute(appended.Route, false, false, appended.EventSource, appended.EventContract);
            if (appended.EventSource is not null)
            {
                if (_semanticVersion == SemanticVersion.V1) throw new InvalidSemanticContract("An appended event source requires ESM v2.");
                SemanticTypeReference destinationType;
                try
                {
                    destinationType = FixtureEventSourceType(appended.Route, appended.EventContract);
                }
                catch (InvalidSemanticContract)
                {
                    throw new InvalidSemanticContract("An appended event source requires one unambiguous scalar destination type.");
                }

                ValidateEventSource(appended.EventSource, destinationType);
            }
        }

        void ValidateSpecificationEvents(SemanticSpecification specification, SemanticCommand? command)
        {
            var producedEvents = command?.Produces.Select(_ => _.EventContract).ToHashSet();
            foreach (var value in specification.GivenEvents)
            {
                ValidateSpecificationEvent(value);
            }

            foreach (var value in specification.ThenEvents)
            {
                // Since ESM v6 reactions and captures append too, so any event can follow an action.
                if (!_semanticVersion.IsAtLeast(SemanticVersion.V6) && !IsActionEvent(specification, producedEvents, value))
                {
                    throw new InvalidSemanticContract("A specification expects an event the command does not produce.");
                }

                ValidateSpecificationEvent(value, true);
            }
        }

        void ValidateSpecificationReadModels(SemanticSpecification specification)
        {
            foreach (var state in specification.GivenReadModels)
            {
                ValidateSpecificationReadModel(state, true, true);
            }

            RejectDuplicateReadModelStates(specification.GivenReadModels, "given read model");
            foreach (var state in specification.ThenReadModels)
            {
                ValidateSpecificationReadModel(state, false, true);
            }

            RejectDuplicateReadModelStates(specification.ThenReadModels, "expected read model");
            foreach (var absentState in specification.ThenAbsentReadModels)
            {
                ValidateAbsentReadModel(specification, absentState);
            }

            RejectDuplicateAbsentReadModels(specification.ThenAbsentReadModels);
        }

        void ValidateAbsentReadModel(SemanticSpecification specification, SemanticSpecificationAbsentReadModel absentState)
        {
            if (!_readModels.TryGetValue(absentState.ReadModel, out var readModel))
            {
                throw new InvalidSemanticContract($"Specification absent read model '{absentState.ReadModel}' is unresolved.");
            }

            ValidateValue(absentState.Key, IdentifierProperty(readModel).Type, "specification absent read model key");
            if (AssertsPresence(specification, absentState))
            {
                throw new InvalidSemanticContract("A specification cannot assert both presence and absence for the same read model and key.");
            }
        }
    }
}

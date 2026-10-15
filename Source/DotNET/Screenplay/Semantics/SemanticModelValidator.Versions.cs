// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics;

internal static partial class SemanticModelValidator
{
    static IEnumerable<SemanticSlice> SlicesOf(SemanticApplication application) =>
        application.Modules.SelectMany(module => module.Features).SelectMany(AllSlices);

    static void ValidateDecisionReadVersion(SemanticApplication application, SemanticVersion version)
    {
        if (application.CommandReadMode is { } mode && mode != SemanticCommandReadMode.Protected)
            throw new InvalidSemanticContract("Command read mode must be protected.");
        ValidateFeatureVersion(
            application.CommandReadMode is not null,
            version,
            SemanticVersion.V24,
            "An ESM v24 model must explicitly select protected command reads.",
            "Protected command reads require ESM v24.");
        if (application.CommandReadMode is null && SlicesOf(application).SelectMany(slice => slice.Commands).Any(command => !command.Reads.IsDefaultOrEmpty))
            throw new InvalidSemanticContract("Command reads require the protected command read mode.");
    }

    static void ValidateEventSourceVersion(SemanticApplication application, SemanticVersion semanticVersion)
    {
        if (semanticVersion == SemanticVersion.V2 && !SlicesOf(application).Any(UsesEventSource))
        {
            throw new InvalidSemanticContract("An ESM v2 model must contain a typed destination, a specification event source, or an occurrence context mapping.");
        }
    }

    static bool UsesEventSource(SemanticSlice slice) =>
        slice.Commands.Any(HasDestinationOrContextMapping) || slice.Specifications.Any(SpecifiesEventSource);

    static bool HasDestinationOrContextMapping(SemanticCommand command) =>
        command.Destination is not null ||
        command.Produces.Any(produced => produced.Mappings.Any(mapping => mapping.Source is SemanticEventContextExpression));

    static bool SpecifiesEventSource(SemanticSpecification specification) =>
        ActsOnEventSource(specification) ||
        specification.GivenEvents.Any(value => value.EventSource is not null) ||
        specification.ThenEvents.Any(value => value.EventSource is not null);

    static bool ActsOnEventSource(SemanticSpecification specification) =>
        specification.When?.EventSource is not null || specification.WhenAppended?.EventSource is not null;

    static void ValidateEventLineageVersion(SemanticApplication application, SemanticVersion semanticVersion)
    {
        var evolved = SlicesOf(application)
            .SelectMany(slice => slice.Events).Any(@event => !@event.PriorRevisions.IsDefaultOrEmpty);
        ValidateFeatureVersion(
            evolved,
            semanticVersion,
            SemanticVersion.V4,
            "An ESM v4 model must contain a multi-generation event contract.",
            "Event contract lineage requires ESM v4.");
    }

    static void ValidateReadModelAbsenceVersion(SemanticApplication application, SemanticVersion semanticVersion)
    {
        var absent = SlicesOf(application)
            .SelectMany(slice => slice.Specifications).Any(specification => !specification.ThenAbsentReadModels.IsDefaultOrEmpty);
        ValidateFeatureVersion(
            absent,
            semanticVersion,
            SemanticVersion.V5,
            "An ESM v5 model must contain a keyed read-model absence assertion.",
            "A keyed read-model absence assertion requires ESM v5.");
    }

    // A model at the version that introduced a feature must use it; a model before that version cannot.
    static void ValidateFeatureVersion(bool featureUsed, SemanticVersion semanticVersion, SemanticVersion introduced, string requiredMessage, string unsupportedMessage)
    {
        if (semanticVersion == introduced && !featureUsed)
        {
            throw new InvalidSemanticContract(requiredMessage);
        }

        if (!semanticVersion.IsAtLeast(introduced) && featureUsed)
        {
            throw new InvalidSemanticContract(unsupportedMessage);
        }
    }

    static void ValidateImplementationAttachmentVersion(SemanticApplication application, SemanticVersion semanticVersion)
    {
        if (semanticVersion == SemanticVersion.V3 && !HasImplementationAttachments(application))
        {
            throw new InvalidSemanticContract("An ESM v3 model must contain implementation attachments.");
        }
    }

    static bool HasImplementationAttachments(SemanticApplication application) =>
        application.Policies.Any(policy => policy.Condition is SemanticOpaquePolicyCondition) ||
        application.Concepts.Any(concept => concept.Validations.Any(IsPredicateAttachment)) ||
        !SlicesOf(application).All(slice => slice.Reducers.IsEmpty && slice.Commands.All(HasNoCommandAttachments));

    static bool IsPredicateAttachment(SemanticValidationRule validation) =>
        validation.Kind is SemanticValidationRuleKind.RulePredicate or SemanticValidationRuleKind.CodeValidation;

    static bool HasNoCommandAttachments(SemanticCommand command) =>
        command.CodeValidations.IsEmpty && command.Validations.All(validation => validation.Kind != SemanticValidationRuleKind.RulePredicate);
}

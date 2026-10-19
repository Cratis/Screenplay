// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics;

internal static partial class SemanticModelValidator
{
    internal static bool UsesCompositeReadModelKeys(SemanticApplication application) =>
        SlicesOf(application).SelectMany(slice => slice.ReadModels).Any(model => model.Properties.Count(property => property.IsIdentifier) > 1);

    internal static bool UsesDateComparisons(SemanticApplication application) =>
        SlicesOf(application).SelectMany(slice => slice.Commands).Any(command => SemanticDateComparisons.Participants(application, command).Any());

    internal static bool UsesUuidComparisons(SemanticApplication application)
    {
        var readModels = SlicesOf(application).SelectMany(slice => slice.ReadModels).ToDictionary(view => view.Id);
        var concepts = application.Concepts.ToDictionary(concept => concept.Id, concept => concept.Primitive);
        return SlicesOf(application).SelectMany(slice => slice.Commands).Any(command =>
            command.Requirements.Select(requirement => requirement.Condition).Concat(command.Produces.Select(production => production.When).OfType<SemanticCondition>())
                .SelectMany(Comparisons).Any(comparison => IsUuid(command, comparison.Left) || IsUuid(command, comparison.Right)));

        bool IsUuid(SemanticCommand command, SemanticConditionOperand operand)
        {
            SemanticTypeReference? type = null;
            if (operand.Read is { } read)
            {
                var view = command.Reads.FirstOrDefault(value => value.Name == read) is { } declared && readModels.TryGetValue(declared.ReadModel, out var model) ? model : null;
                type = view?.Properties.FirstOrDefault(property => property.Id == operand.Property)?.Type;
            }
            else if (operand.Property.IsSet)
            {
                type = command.Properties.FirstOrDefault(property => property.Id == operand.Property)?.Type;
            }

            return type switch
            {
                { Kind: SemanticTypeReferenceKind.Primitive } => type.Primitive == SemanticPrimitiveType.Uuid,
                { Kind: SemanticTypeReferenceKind.Concept } => concepts.GetValueOrDefault(type.Target) == SemanticPrimitiveType.Uuid,
                _ => false
            };
        }
    }

    internal static bool UsesDecimalPlaces(SemanticApplication application) =>
        application.Concepts.SelectMany(concept => concept.Validations)
            .Concat(SlicesOf(application).SelectMany(slice => slice.Commands).SelectMany(command => command.Validations))
            .Any(rule => rule.Kind == SemanticValidationRuleKind.DecimalPlaces);

    /// <summary>
    /// Gets whether the model uses any construct admitted by decision 0098 (ESM v29).
    /// </summary>
    /// <param name="application">The application.</param>
    /// <returns><c>true</c> when ESM v29 is required.</returns>
    internal static bool UsesAcmeAdmissions(SemanticApplication application) => UsesDecimalPlaces(application) || UsesProjectionProducts(application);

    internal static bool UsesProjectionProducts(SemanticApplication application) =>
        SlicesOf(application).SelectMany(slice => slice.Projections).Any(projection => ProjectionMappings(projection).Any(mapping => mapping.Factor is not null));

    internal static bool UsesTextRules(SemanticApplication application) =>
        application.Concepts.SelectMany(concept => concept.Validations)
            .Concat(SlicesOf(application).SelectMany(slice => slice.Commands).SelectMany(command => command.Validations))
            .Any(rule => TextRuleVersion.IsTextRule(rule.Kind));

    internal static bool UsesDescribedHandlers(SemanticApplication application) =>
        SlicesOf(application).SelectMany(slice => slice.Commands).Any(command => command.Handler is not null);

    // ESM v29 is the umbrella of decision 0098. Some of its constructs (a concept converted to another over the same primitive)
    // are not detectable from the model alone, so a v29 model need not use any particular one.
    static void ValidateAcmeAdmissionVersion(SemanticApplication application, SemanticVersion version)
    {
        if (!version.IsAtLeast(SemanticVersion.V29) && UsesAcmeAdmissions(application))
        {
            throw new InvalidSemanticContract("Decimal places, projection products and the other constructs of decision 0098 require ESM v29.");
        }
    }

    static IEnumerable<SemanticProjectionMapping> ProjectionMappings(SemanticProjection projection) =>
        projection.Scope is null ? [] : ScopeMappings(projection.Scope);

    static IEnumerable<SemanticProjectionMapping> ScopeMappings(SemanticProjectionScope scope) =>
        scope.From.SelectMany(from => from.Mappings)
            .Concat(scope.Joins.SelectMany(join => join.Mappings))
            .Concat(scope.Every?.Mappings ?? [])
            .Concat(scope.Children.SelectMany(child => ScopeMappings(child.Scope)))
            .Concat(scope.Nested.SelectMany(nested => ScopeMappings(nested.Scope)));

    static IEnumerable<SemanticComparison> Comparisons(SemanticCondition condition) => condition switch
    {
        SemanticComparison comparison => [comparison],
        SemanticLogicalCondition logical => Comparisons(logical.Left).Concat(Comparisons(logical.Right)),
        _ => []
    };

    static void ValidateCompositeReadModelKeyVersion(SemanticApplication application, SemanticVersion version) =>
        ValidateFeatureVersion(
            UsesCompositeReadModelKeys(application),
            version,
            SemanticVersion.V27,
            "A composite read-model key ESM must contain a read model with several identifier parts.",
            "Flat composite read-model keys require their admitting ESM version.");

    static void ValidateDateComparisonVersion(SemanticApplication application, SemanticVersion version) =>
        ValidateFeatureVersion(
            UsesDateComparisons(application),
            version,
            SemanticVersion.V26,
            "An ESM v26 model must use a Date or DateTime command comparison.",
            "Chronological command date comparisons require ESM v26.");

    static void ValidateUuidComparisonVersion(SemanticApplication application, SemanticVersion version) =>
        ValidateFeatureVersion(
            UsesUuidComparisons(application),
            version,
            SemanticVersion.V28,
            "An ESM v28 model must use a Uuid command comparison.",
            "Uuid command comparisons require ESM v28.");

    static void ValidateTextRuleVersion(SemanticApplication application, SemanticVersion version) =>
        ValidateFeatureVersion(
            UsesTextRules(application),
            version,
            SemanticVersion.V24,
            "A text-rule ESM must contain a first-class text rule.",
            $"First-class text rules require ESM v{TextRuleVersion.Major} or later.");

    static IEnumerable<SemanticSlice> SlicesOf(SemanticApplication application) =>
        application.Modules.SelectMany(module => module.Features).SelectMany(AllSlices);

    static void ValidateDecisionReadVersion(SemanticApplication application, SemanticVersion version)
    {
        if (application.CommandReadMode is { } mode && mode != SemanticCommandReadMode.Protected)
            throw new InvalidSemanticContract("Command read mode must be protected.");
        ValidateFeatureVersion(
            application.CommandReadMode is not null,
            version,
            SemanticVersion.V25,
            "An ESM v25 model must explicitly select protected command reads.",
            "Protected command reads require ESM v25.");
        if (application.CommandReadMode is null && SlicesOf(application).SelectMany(slice => slice.Commands).Any(command => !command.Reads.IsDefaultOrEmpty))
            throw new InvalidSemanticContract("Command reads require the protected command read mode.");
    }
    static void ValidateDescribedHandlerVersion(SemanticApplication application, SemanticVersion version) =>
        ValidateFeatureVersion(
            UsesDescribedHandlers(application),
            version,
            SemanticVersion.V23,
            "An ESM v23 model must contain a described command.",
            "Described command handlers require ESM v23 or later.");

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

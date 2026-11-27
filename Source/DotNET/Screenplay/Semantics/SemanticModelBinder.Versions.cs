// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics;

public sealed partial class SemanticModelBinder
{
    // First match wins. Keep the feature checks in descending version order.
    static readonly ImmutableArray<(Func<SemanticApplication, bool> Uses, LanguageVersion Language, SemanticVersion Semantic)> _featureVersions =
    [
        (SemanticModelValidator.UsesAbilityInvocations, LanguageVersion.V51, SemanticVersion.V51),
        (application => !application.Systems.IsDefaultOrEmpty, LanguageVersion.V50, SemanticVersion.V50),
        (SemanticModelValidator.UsesCallerIdentity, LanguageVersion.V49, SemanticVersion.V49),
        (application => application.Agents is not null, LanguageVersion.V48, SemanticVersion.V48),
        (SemanticModelValidator.UsesNarrowing, LanguageVersion.V47, SemanticVersion.V47),
        (SemanticModelValidator.UsesPolicyInputs, LanguageVersion.V46, SemanticVersion.V46),
        (SemanticModelValidator.UsesTextOperators, LanguageVersion.V45, SemanticVersion.V45),
        (SemanticModelValidator.UsesStatePolicies, LanguageVersion.V44, SemanticVersion.V44),
        (SemanticModelValidator.UsesProjectionParity, LanguageVersion.V43, SemanticVersion.V43),
        (SemanticModelValidator.UsesGraphForEach, LanguageVersion.V41, SemanticVersion.V41),
        (SemanticModelValidator.UsesGraphQueries, LanguageVersion.V40, SemanticVersion.V40),
        (SemanticModelValidator.UsesGraphs, LanguageVersion.V38, SemanticVersion.V38),
        (SemanticClockUsage.Uses, LanguageVersion.V37, SemanticVersion.V37),
        (SemanticModelValidator.UsesLifecycles, LanguageVersion.V36, SemanticVersion.V36),
        (SemanticModelValidator.UsesHandlerResultV2, LanguageVersion.V35, SemanticVersion.V35),
        (SemanticModelValidator.UsesThenInvocations, LanguageVersion.V34, SemanticVersion.V34),
        (SemanticModelValidator.UsesEventSourceCaptureKeys, LanguageVersion.V33, SemanticVersion.V33),
        (SemanticModelValidator.UsesTypedCaptureKeys, LanguageVersion.V32, SemanticVersion.V32),
        (SemanticModelValidator.UsesAnonymousPolicies, LanguageVersion.V31, SemanticVersion.V31),
        (SemanticModelValidator.UsesAcmeSecondAdmissions, LanguageVersion.V30, SemanticVersion.V30),
        (SemanticModelValidator.UsesAcmeAdmissions, LanguageVersion.V29, SemanticVersion.V29),
        (SemanticModelValidator.UsesUuidComparisons, LanguageVersion.V28, SemanticVersion.V28),
        (SemanticModelValidator.UsesCompositeReadModelKeys, LanguageVersion.V27, SemanticVersion.V27),
        (SemanticModelValidator.UsesDateComparisons, LanguageVersion.V26, SemanticVersion.V26),
        (application => application.CommandReadMode is not null, LanguageVersion.V25, SemanticVersion.V25),
        (SemanticModelValidator.UsesTextRules, LanguageVersion.V24, SemanticVersion.V24),
        (SemanticModelValidator.UsesDescribedHandlers, LanguageVersion.V23, SemanticVersion.V23),
        (application => application.ReactionProductionMode is not null, LanguageVersion.V22, SemanticVersion.V22),
        (SemanticModelValidator.UsesPhase3Generation, LanguageVersion.V21, SemanticVersion.V21),
        (SemanticModelValidator.UsesGeneratedConceptRules, LanguageVersion.V20, SemanticVersion.V20),
        (SemanticModelValidator.UsesRefusalBranches, LanguageVersion.V19, SemanticVersion.V19),
        (SemanticModelValidator.UsesQueryRejections, LanguageVersion.V18, SemanticVersion.V18),
        (SemanticModelValidator.UsesRecoverySpecifications, LanguageVersion.V17, SemanticVersion.V17)
    ];

    // These features take precedence over exact literals but not stored event names (v16).
    static readonly ImmutableArray<(Func<SemanticApplication, bool> Uses, LanguageVersion Language, SemanticVersion Semantic)> _versionsBeforeStoredEventNames =
    [
        (SemanticModelValidator.UsesConceptGeneration, LanguageVersion.V15, SemanticVersion.V15),
        (SemanticModelValidator.UsesQueryFilters, LanguageVersion.V14, SemanticVersion.V14),
        (SemanticModelValidator.UsesConceptCompliance, LanguageVersion.V13, SemanticVersion.V13),
        (SemanticModelValidator.UsesEnumMemberNumbers, LanguageVersion.V12, SemanticVersion.V12)
    ];

    static readonly ImmutableArray<(Func<BindingContext, bool> Uses, LanguageVersion Language, SemanticVersion Semantic)> _initialVersions =
    [
        (context => context is { UsesV10: true } or { UsesReactionIdentity: true }, LanguageVersion.V10, SemanticVersion.V10),
        (context => context is { UsesPublicEvents: true }, LanguageVersion.V9, SemanticVersion.V9),
        (context => context is { UsesEventRoutes: true }, LanguageVersion.V8, SemanticVersion.V8),
        (context => context is { UsesV7: true }, LanguageVersion.V7, SemanticVersion.V7),
        (context => context is { UsesV6: true }, LanguageVersion.V6, SemanticVersion.V6),
        (context => context is { UsesV5: true }, LanguageVersion.V5, SemanticVersion.V5),
        (context => context is { UsesV4: true }, LanguageVersion.V4, SemanticVersion.V4),
        (context => context is { UsesV3: true }, LanguageVersion.V3, SemanticVersion.V3),
        (context => context is { UsesV2: true }, LanguageVersion.V2, SemanticVersion.V2)
    ];

    static (SemanticApplication Application, LanguageVersion Language, SemanticVersion Semantic, ImmutableArray<SemanticTypedContextDescriptor> Descriptors) ApplyRequiredVersions(
        BindingContext context,
        SemanticApplication application,
        ImmutableArray<SemanticTypedContextDescriptor> descriptors)
    {
        var (languageVersion, semanticVersion) = InitialBoundVersions(context);
        if (SelectBoundVersions(application, semanticVersion, context.UsesStoredEventNames) is { } required)
        {
            (languageVersion, semanticVersion) = required;
            (application, descriptors) = PromoteDestinationsAndRefreshDescriptors(context, application);
        }

        // A v30 floor can require a second promotion and descriptor refresh.
        if (context.UsesV30 && !semanticVersion.IsAtLeast(SemanticVersion.V30))
        {
            languageVersion = LanguageVersion.V30;
            semanticVersion = SemanticVersion.V30;
            (application, descriptors) = PromoteDestinationsAndRefreshDescriptors(context, application);
        }

        return (application, languageVersion, semanticVersion, descriptors);
    }

    static (SemanticApplication Application, ImmutableArray<SemanticTypedContextDescriptor> Descriptors) PromoteDestinationsAndRefreshDescriptors(
        BindingContext context,
        SemanticApplication application)
    {
        application = BindingContext.PromoteV2Destinations(application);
        return (application, SemanticTypedContextCatalog.Create(application, context.ImplementationRequirements, true));
    }

    static (LanguageVersion Language, SemanticVersion Semantic, ImmutableArray<SemanticModelValidator.SemanticValidationFailure> Failures) ValidateAtRequiredVersion(
        SemanticApplication application,
        LanguageVersion languageVersion,
        SemanticVersion semanticVersion,
        int utcAssessmentYear)
    {
        EsmSchemaV60Support.EnsureSupported(languageVersion, semanticVersion);
        var failures = SemanticModelValidator.Collect(application, semanticVersion, utcAssessmentYear);
        if (failures.IsEmpty || semanticVersion.IsAtLeast(SemanticVersion.V29))
        {
            return (languageVersion, semanticVersion, failures);
        }

        // Decision 0098 admits conversion between concepts over the same primitive starting at ESM v29.
        return SemanticModelValidator.Collect(application, SemanticVersion.V29, utcAssessmentYear).IsEmpty
            ? (LanguageVersion.V29, SemanticVersion.V29, [])
            : (languageVersion, semanticVersion, failures);
    }

    static (LanguageVersion Language, SemanticVersion Semantic)? SelectBoundVersions(SemanticApplication application, SemanticVersion semanticVersion, bool usesStoredEventNames) =>
        EsmSchemaV60Support.SelectBoundVersions(application)
        ?? FirstRequiredVersion(application, _featureVersions)
        ?? (usesStoredEventNames ? (LanguageVersion.V16, SemanticVersion.V16) : SelectNumberVersions(application, semanticVersion));

    static (LanguageVersion Language, SemanticVersion Semantic)? FirstRequiredVersion<T>(
        T subject,
        ImmutableArray<(Func<T, bool> Uses, LanguageVersion Language, SemanticVersion Semantic)> versions)
    {
        var match = versions.FirstOrDefault(version => version.Uses(subject));
        return match.Uses is null ? null : (match.Language, match.Semantic);
    }

    static (LanguageVersion Language, SemanticVersion Semantic)? SelectNumberVersions(SemanticApplication application, SemanticVersion semanticVersion) =>
        FirstRequiredVersion(application, _versionsBeforeStoredEventNames) ?? SelectExactNumberVersion(application, semanticVersion);

    // Number lowering is pure. Select v11 from the retained graph with the validator's own
    // value walk, after examples and routed fixtures have reached their final bound shape.
    static (LanguageVersion Language, SemanticVersion Semantic)? SelectExactNumberVersion(SemanticApplication application, SemanticVersion semanticVersion) =>
        !semanticVersion.IsAtLeast(SemanticVersion.V12) && SemanticModelValidator.UsesExactNumberLiterals(application, semanticVersion)
            ? (LanguageVersion.V11, SemanticVersion.V11)
            : null;

    static (LanguageVersion Language, SemanticVersion Semantic) InitialBoundVersions(BindingContext context) =>
        FirstRequiredVersion(context, _initialVersions) ?? (LanguageVersion.V1, SemanticVersion.V1);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Represents the immutable, versioned executable semantic model.
/// </summary>
public sealed record ExecutableSemanticModel
{
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<ExecutableSemanticModel, CachedDeprecations> _deprecations = [];

    ExecutableSemanticModel(
        LanguageVersion languageVersion,
        SemanticVersion semanticVersion,
        SemanticRevision revision,
        SemanticApplication application)
    {
        LanguageVersion = languageVersion;
        SemanticVersion = semanticVersion;
        Revision = revision;
        Application = application;
    }

    /// <summary>
    /// Gets the source language version.
    /// </summary>
    public LanguageVersion LanguageVersion { get; }

    /// <summary>
    /// Gets the portable execution semantic version.
    /// </summary>
    public SemanticVersion SemanticVersion { get; }

    /// <summary>
    /// Gets the deterministic revision over canonical semantic content, excluding this revision field.
    /// </summary>
    public SemanticRevision Revision { get; }

    /// <summary>
    /// Gets the semantic application graph.
    /// </summary>
    public SemanticApplication Application { get; }

    /// <summary>
    /// Gets deprecation warnings for legacy projection transition cardinalities. ESM has no source locations;
    /// these model-level diagnostics point at the beginning of the document.
    /// </summary>
    /// <remarks>Only flat projection transitions carry these cardinalities. Query cardinality is unaffected.</remarks>
    public ImmutableArray<Diagnostic> DeprecationDiagnostics => _deprecations.GetValue(this, static model => new CachedDeprecations(
    [
        .. model.Application.Modules.SelectMany(module => module.Features).SelectMany(AllSlices)
            .SelectMany(slice => slice.Projections)
            .SelectMany(projection => projection.Transitions
                .Where(transition => transition.AffectedInstance.Cardinality is AffectedInstanceCardinality.ZeroOrOne or AffectedInstanceCardinality.Many)
                .Select(transition => Diagnostic.Warning(
                    DiagnosticCodes.DeprecatedProjectionTransitionCardinality,
                    $"Projection '{projection.Name}' event contract '{transition.EventContract}' uses deprecated affected-instance cardinality '{transition.AffectedInstance.Cardinality}' on a transition; Chronicle routes one key per transition. Use a join for many affected instances.",
                    SourceLocation.Start)))
    ])).Values;

    /// <summary>
    /// Creates a validated model and computes its deterministic revision.
    /// </summary>
    /// <param name="languageVersion">The source language version.</param>
    /// <param name="semanticVersion">The portable execution semantic version.</param>
    /// <param name="application">The application graph.</param>
    /// <returns>The validated executable semantic model.</returns>
    /// <exception cref="InvalidSemanticContract">The model is malformed or contains unresolved references.</exception>
    public static ExecutableSemanticModel Create(
        LanguageVersion languageVersion,
        SemanticVersion semanticVersion,
        SemanticApplication application) => Create(languageVersion, semanticVersion, application, DateTimeOffset.UtcNow.Year);

    /// <summary>
    /// Creates a model using an explicit UTC year for generation support assessment; the year is not serialized.
    /// </summary>
    /// <param name="languageVersion">The source language version.</param>
    /// <param name="semanticVersion">The semantic version.</param>
    /// <param name="application">The application graph.</param>
    /// <param name="utcAssessmentYear">The UTC year whose complete 100-year horizon is assessed.</param>
    /// <returns>The validated model.</returns>
    public static ExecutableSemanticModel Create(LanguageVersion languageVersion, SemanticVersion semanticVersion, SemanticApplication application, int utcAssessmentYear)
    {
        EsmSchemaV27Support.EnsureSupported(languageVersion, semanticVersion);
        SemanticModelValidator.Validate(application, semanticVersion, utcAssessmentYear);
        var withoutRevision = SemanticModelCanonicalJson.SerializeWithoutRevision(languageVersion, semanticVersion, application);
        var revision = SemanticRevision.Compute(withoutRevision);
        return new(languageVersion, semanticVersion, revision, application);
    }

    static IEnumerable<SemanticSlice> AllSlices(SemanticFeature feature) =>
        feature.Slices.Concat(feature.Features.SelectMany(AllSlices));

    sealed record CachedDeprecations(ImmutableArray<Diagnostic> Values);
}

internal static partial class SemanticModelValidator
{
    public static void Validate(SemanticApplication application, SemanticVersion semanticVersion = default, int? utcAssessmentYear = null)
    {
        if (application is null)
        {
            throw new InvalidSemanticContract("The semantic application cannot be null.");
        }

        var assessmentYear = utcAssessmentYear ?? DateTimeOffset.UtcNow.Year;
        var context = new ValidationContext(semanticVersion == default ? SemanticVersion.V1 : semanticVersion, application, assessmentYear);
        context.RegisterApplication(application);
        if (application.ReactionProductionMode is { } mode &&
            (mode != SemanticReactionProductionMode.Atomic || !semanticVersion.IsAtLeast(SemanticVersion.V22)))
        {
            throw new InvalidSemanticContract("Reaction production mode must be atomic and requires ESM v22 or later.");
        }

        ValidateDecisionReadVersion(application, semanticVersion);
        ValidateDateComparisonVersion(application, semanticVersion);
        ValidateUuidComparisonVersion(application, semanticVersion);
        ValidateDescribedHandlerVersion(application, semanticVersion);
        ValidateTextRuleVersion(application, semanticVersion);
        ValidateQueryRejectionVersion(application, semanticVersion);
        ValidateRefusalVersion(application, semanticVersion);
        ValidateRecoverySpecificationVersion(application, semanticVersion);
        ValidateStoredEventNames(application, semanticVersion);
        ValidateGenerationVersion(application, semanticVersion, assessmentYear);
        ValidateComplianceVersion(application, semanticVersion);
        ValidateQueryFilterVersion(application, semanticVersion);
        ValidateEnumMemberNumbersVersion(application, semanticVersion);
        ValidateEventRoutesVersion(application, semanticVersion);
        ValidatePublicEventsVersion(application, semanticVersion);
        ValidateReactionIdentityVersion(application, semanticVersion);
        context.ValidateReferences(application);
        ValidateNumberLiteralsVersion(semanticVersion, context.UsesExactNumberLiterals);
        ValidateEventSourceVersion(application, semanticVersion);
        ValidateEventLineageVersion(application, semanticVersion);
        ValidateReadModelAbsenceVersion(application, semanticVersion);
        ValidateAutomationVersion(application, semanticVersion);
        ValidateResponseVersion(application, semanticVersion);
        ValidateImplementationAttachmentVersion(application, semanticVersion);
    }

    static void RejectDuplicateRequirementIds(IEnumerable<string?> requirementIds, string owner)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var requirementId in requirementIds.Where(id => id is not null))
        {
            if (!seen.Add(requirementId!))
            {
                throw new InvalidSemanticContract($"{owner} has duplicate validation requirement identities.");
            }
        }
    }

    static IEnumerable<SemanticSlice> AllSlices(SemanticFeature feature) =>
        feature.Slices.Concat(feature.Features.SelectMany(AllSlices));
}

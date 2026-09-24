// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Execution;

/// <summary>
/// Defines why an otherwise valid ESM construct is not admitted by the current portable evaluator.
/// </summary>
public enum SemanticPlanIssueKind
{
    /// <summary>
    /// An unknown issue. Unknown values are never emitted.
    /// </summary>
    Unknown = -1,

    /// <summary>
    /// A validation rule has no portable evaluator implementation.
    /// </summary>
    UnsupportedValidation = 0,

    /// <summary>
    /// Reserved legacy issue kind (no longer emitted; conditional production is evaluated).
    /// </summary>
    ConditionalProduction = 1,

    /// <summary>
    /// An affected-instance cardinality is not admitted by the minimum evaluator.
    /// </summary>
    UnsupportedAffectedCardinality = 2,

    /// <summary>
    /// A query cardinality or delivery contract is not admitted by the minimum evaluator.
    /// </summary>
    UnsupportedQuery = 3,

    /// <summary>
    /// A projection block combination has no verified reference semantics.
    /// </summary>
    UnsupportedProjectionBlock = 4,

    /// <summary>
    /// A projection reads an event-context value the reference evaluator has no occurrence context for.
    /// </summary>
    UnsupportedEventContext = 5
}

/// <summary>
/// Represents one typed capability issue found before execution.
/// </summary>
/// <param name="Artifact">The semantic artifact requiring the capability.</param>
/// <param name="Kind">The unsupported capability kind.</param>
/// <param name="Details">Human-readable details.</param>
public sealed record SemanticPlanIssue(SemanticId Artifact, SemanticPlanIssueKind Kind, string Details);

/// <summary>
/// Represents the outcome of compiling ESM into the current portable execution plan.
/// </summary>
/// <param name="Plan">The plan, or <see langword="null"/> when unsupported reachable behavior blocks execution.</param>
/// <param name="Issues">The blocking capability issues.</param>
public sealed record SemanticExecutionPlanCompilation(
    SemanticExecutionPlan? Plan,
    ImmutableArray<SemanticPlanIssue> Issues)
{
    /// <summary>
    /// Gets a value indicating whether the executable plan was created.
    /// </summary>
    public bool Success => Plan is not null && Issues.IsEmpty;
}

/// <summary>
/// Represents an immutable, indexed, capability-admitted portable execution plan.
/// </summary>
public sealed class SemanticExecutionPlan
{
    SemanticExecutionPlan(
        ExecutableSemanticModel model,
        ImmutableDictionary<SemanticId, SemanticCommand> commands,
        ImmutableDictionary<SemanticId, SemanticEventContract> events,
        ImmutableDictionary<SemanticId, SemanticProjection> projections,
        ImmutableDictionary<SemanticId, SemanticReadModel> readModels,
        ImmutableDictionary<SemanticId, SemanticKeyedQuery> queries,
        ImmutableDictionary<SemanticId, SemanticSpecification> specifications,
        ImmutableDictionary<string, SemanticConstraint> constraints)
    {
        Model = model;
        Commands = commands;
        Events = events;
        Projections = projections;
        ReadModels = readModels;
        Queries = queries;
        Specifications = specifications;
        Constraints = constraints;
    }

    /// <summary>
    /// Gets the ESM revision this plan executes.
    /// </summary>
    public SemanticRevision Revision => Model.Revision;

    /// <summary>
    /// Gets the admitted executable semantic model.
    /// </summary>
    public ExecutableSemanticModel Model { get; }

    /// <summary>
    /// Gets commands by semantic identity.
    /// </summary>
    public ImmutableDictionary<SemanticId, SemanticCommand> Commands { get; }

    /// <summary>
    /// Gets event contracts by semantic identity.
    /// </summary>
    public ImmutableDictionary<SemanticId, SemanticEventContract> Events { get; }

    /// <summary>
    /// Gets projections by semantic identity.
    /// </summary>
    public ImmutableDictionary<SemanticId, SemanticProjection> Projections { get; }

    /// <summary>
    /// Gets read models by semantic identity.
    /// </summary>
    public ImmutableDictionary<SemanticId, SemanticReadModel> ReadModels { get; }

    /// <summary>
    /// Gets queries by semantic identity.
    /// </summary>
    public ImmutableDictionary<SemanticId, SemanticKeyedQuery> Queries { get; }

    /// <summary>
    /// Gets specifications by semantic identity.
    /// </summary>
    public ImmutableDictionary<SemanticId, SemanticSpecification> Specifications { get; }

    /// <summary>
    /// Gets append-time constraints by name, which is a constraint's identity.
    /// </summary>
    public ImmutableDictionary<string, SemanticConstraint> Constraints { get; }

    /// <summary>
    /// Compiles ESM into a plan only when every reachable capability is admitted.
    /// </summary>
    /// <param name="model">The validated executable semantic model.</param>
    /// <returns>The plan compilation and any blocking capability issues.</returns>
    public static SemanticExecutionPlanCompilation Compile(ExecutableSemanticModel model)
    {
        var issues = ImmutableArray.CreateBuilder<SemanticPlanIssue>();
        var slices = AllSlices(model.Application).ToArray();
        foreach (var concept in model.Application.Concepts)
        {
            foreach (var validation in concept.Validations.Where(_ => _.Kind is not (SemanticValidationRuleKind.RulePredicate or SemanticValidationRuleKind.CodeValidation) && !SemanticValidationRules.Evaluates(_.Kind)))
            {
                issues.Add(new(concept.Id, SemanticPlanIssueKind.UnsupportedValidation, $"Concept validation '{validation.Kind}' is not admitted by the minimum evaluator."));
            }
        }

        foreach (var command in slices.SelectMany(_ => _.Commands))
        {
            foreach (var validation in command.Validations.Where(_ => _.Kind is not (SemanticValidationRuleKind.RulePredicate or SemanticValidationRuleKind.CodeValidation) && !SemanticValidationRules.Evaluates(_.Kind)))
            {
                issues.Add(new(command.Id, SemanticPlanIssueKind.UnsupportedValidation, $"Validation '{validation.Kind}' is not admitted by the minimum evaluator."));
            }
        }

        foreach (var projection in slices.SelectMany(_ => _.Projections))
        {
            foreach (var transition in projection.Transitions.Where(_ => _.AffectedInstance.Cardinality != AffectedInstanceCardinality.One))
            {
                issues.Add(new(projection.Id, SemanticPlanIssueKind.UnsupportedAffectedCardinality, $"Affected cardinality '{transition.AffectedInstance.Cardinality}' is not admitted by the minimum evaluator."));
            }

            issues.AddRange(SemanticScopedProjectionIssues.For(projection));
        }

        foreach (var query in slices.SelectMany(_ => _.Queries).Where(_ => _.Cardinality != SemanticQueryCardinality.ZeroOrOne || _.Delivery != SemanticQueryDelivery.Snapshot))
        {
            issues.Add(new(query.Id, SemanticPlanIssueKind.UnsupportedQuery, $"Query '{query.Name}' must be an optional snapshot lookup in the minimum evaluator."));
        }

        if (issues.Count > 0)
        {
            return new(null, issues.ToImmutable());
        }

        return new(
            new(
                model,
                slices.SelectMany(_ => _.Commands).ToImmutableDictionary(_ => _.Id),
                slices.SelectMany(_ => _.Events).ToImmutableDictionary(_ => _.Id),
                slices.SelectMany(_ => _.Projections).ToImmutableDictionary(_ => _.Id),
                slices.SelectMany(_ => _.ReadModels).ToImmutableDictionary(_ => _.Id),
                slices.SelectMany(_ => _.Queries).ToImmutableDictionary(_ => _.Id),
                slices.SelectMany(_ => _.Specifications).ToImmutableDictionary(_ => _.Id),
                slices.SelectMany(_ => _.Constraints).ToImmutableDictionary(_ => _.Name, StringComparer.Ordinal)),
            []);
    }

    // A containment level or slice member this walk does not know would be skipped silently, so
    // for_SemanticExecutionPlan/when_inspecting_the_containment_it_traverses holds the ESM shape against it.
    static IEnumerable<SemanticSlice> AllSlices(SemanticApplication application) =>
        application.Modules.SelectMany(_ => AllSlices(_.Features));

    static IEnumerable<SemanticSlice> AllSlices(ImmutableArray<SemanticFeature> features) =>
        features.SelectMany(_ => _.Slices.Concat(AllSlices(_.Features)));
}

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
    UnsupportedEventContext = 5,

    /// <summary>
    /// A slice selection contains an identity that is not a slice in the model.
    /// </summary>
    InvalidSliceSelection = 6,

    /// <summary>
    /// Reserved legacy issue kind (no longer emitted; protected decision reads are evaluated).
    /// </summary>
    UnsupportedDecisionReads = 7
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

    /// <summary>
    /// Gets non-blocking capability issues owned by unselected slices; empty for whole-model compilation.
    /// </summary>
    public ImmutableArray<SemanticPlanIssue> UnselectedIssues { get; init; } = [];
}

/// <summary>
/// Represents an immutable, indexed portable execution plan with whole-model or scoped capability admission.
/// </summary>
public sealed class SemanticExecutionPlan
{
    internal SemanticExecutionPlan(
        ExecutableSemanticModel model,
        ImmutableDictionary<SemanticId, SemanticCommand> commands,
        ImmutableDictionary<SemanticId, SemanticEventContract> events,
        ImmutableDictionary<SemanticId, SemanticProjection> projections,
        ImmutableDictionary<SemanticId, SemanticReadModel> readModels,
        ImmutableDictionary<SemanticId, SemanticKeyedQuery> queries,
        ImmutableDictionary<SemanticId, SemanticSpecification> specifications,
        ImmutableDictionary<string, SemanticConstraint> constraints,
        ImmutableArray<SemanticReaction> reactions,
        ImmutableDictionary<SemanticId, SemanticCapture> captures,
        ImmutableHashSet<SemanticId> unadmittedArtifacts)
    {
        Model = model;
        Commands = commands;
        Events = events;
        Projections = projections;
        ReadModels = readModels;
        Queries = queries;
        Specifications = specifications;
        Constraints = constraints;
        Reactions = reactions;
        Captures = captures;
        UnadmittedArtifacts = unadmittedArtifacts;
    }

    /// <summary>
    /// Gets the ESM revision this plan executes.
    /// </summary>
    public SemanticRevision Revision => Model.Revision;

    /// <summary>
    /// Gets the original executable semantic model, including declarations deferred by scoped compilation.
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
    /// Gets the reactions in the order they run for one occurrence: by semantic identity, which canonical form preserves.
    /// </summary>
    public ImmutableArray<SemanticReaction> Reactions { get; }

    /// <summary>
    /// Gets the captures indexed by semantic identity.
    /// </summary>
    public ImmutableDictionary<SemanticId, SemanticCapture> Captures { get; }

    internal ImmutableHashSet<SemanticId> UnadmittedArtifacts { get; }

    /// <summary>
    /// Compiles ESM into a plan only when every reachable capability is admitted.
    /// </summary>
    /// <param name="model">The validated executable semantic model.</param>
    /// <returns>The plan compilation and any blocking capability issues.</returns>
    public static SemanticExecutionPlanCompilation Compile(ExecutableSemanticModel model) => CompileCore(model, null);

    /// <summary>
    /// Compiles a plan for selected slice identities without changing the model or its revision.
    /// Issues in selected slices and application-level declarations block compilation; other issues are deferred.
    /// An empty selection defers every slice-owned issue and blocks only on application-level issues.
    /// </summary>
    /// <param name="model">The validated executable semantic model.</param>
    /// <param name="selectedSlices">The selection of slice identities in the model, which may be empty.</param>
    /// <returns>The plan compilation, blocking issues, and non-blocking issues in unselected slices.</returns>
    public static SemanticExecutionPlanCompilation Compile(ExecutableSemanticModel model, IEnumerable<SemanticId> selectedSlices) =>
        CompileCore(model, [.. selectedSlices]);

    internal static bool IsBlockingIssue(
        SemanticPlanIssue issue,
        ImmutableHashSet<SemanticId>? selectedSlices,
        IReadOnlyDictionary<SemanticId, SemanticId> owners) =>
        issue.Kind == SemanticPlanIssueKind.InvalidSliceSelection || selectedSlices is null ||
        !owners.TryGetValue(issue.Artifact, out var owner) || selectedSlices.Contains(owner);

    internal static SemanticExecutionPlanCompilation CompileCore(ExecutableSemanticModel model, ImmutableHashSet<SemanticId>? selectedSlices)
    {
        var issues = ImmutableArray.CreateBuilder<SemanticPlanIssue>();
        var slices = AllSlices(model.Application).ToArray();
        if (selectedSlices is not null)
        {
            foreach (var selected in selectedSlices.Where(selected => !slices.Any(slice => slice.Id == selected)).OrderBy(selected => selected.ToString(), StringComparer.Ordinal))
            {
                issues.Add(new(selected, SemanticPlanIssueKind.InvalidSliceSelection, $"Selected slice '{selected}' is not in the model."));
            }
        }

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

        foreach (var query in slices.SelectMany(_ => _.Queries).Where(_ => _.Cardinality == SemanticQueryCardinality.One))
        {
            issues.Add(new(query.Id, SemanticPlanIssueKind.UnsupportedQuery, $"Query '{query.Name}' must be an optional lookup or a collection in the minimum evaluator."));
        }

        var owners = slices.SelectMany(
            slice => slice.Commands.Select(command => command.Id)
                .Concat(slice.Projections.Select(projection => projection.Id))
                .Concat(slice.Queries.Select(query => query.Id)),
            (slice, artifact) => (slice.Id, artifact))
            .ToDictionary(owner => owner.artifact, owner => owner.Id);
        var blocking = issues.Where(issue => IsBlockingIssue(issue, selectedSlices, owners)).ToImmutableArray();
        var unselected = issues.Where(issue => !IsBlockingIssue(issue, selectedSlices, owners)).ToImmutableArray();
        if (!blocking.IsEmpty)
        {
            return new(null, blocking) { UnselectedIssues = unselected };
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
                slices.SelectMany(_ => _.Constraints).ToImmutableDictionary(_ => _.Name, StringComparer.Ordinal),
                [.. slices.SelectMany(_ => _.Reactions).OrderBy(_ => _.Id.ToString(), StringComparer.Ordinal)],
                slices.SelectMany(_ => _.Captures).ToImmutableDictionary(_ => _.Id),
                [.. unselected.Select(issue => issue.Artifact)]),
            [])
        { UnselectedIssues = unselected };
    }

    internal bool IsUnadmittedReadModel(SemanticId readModel) =>
        UnadmittedArtifacts.Contains(readModel) || Projections.Values.Any(projection =>
            projection.Target == SemanticProjectionTargetKind.ReadModel && projection.ReadModel == readModel && UnadmittedArtifacts.Contains(projection.Id));

    internal SemanticEventRoute? FormatFixtureRoute(SemanticFixtureRoute? fixture)
    {
        if (fixture is null) return null;
        var (source, stream) = SemanticEventRouting.Resolve(Model.Application, fixture.Source, fixture.Stream);
        if (!SemanticEventRouting.TryFormat(source, stream, fixture.StreamId, fixture.StreamIdParts, Model.Application.Concepts, out var route, out var failure))
        {
            throw new InvalidSemanticContract(SemanticStreamIdFormatter.FailureMessage(failure));
        }

        return route;
    }

    internal SemanticTypeReference RoutedFactSourceType(SemanticFact fact)
    {
        if (!Model.SemanticVersion.IsAtLeast(SemanticVersion.V8) || fact.Context is null)
        {
            throw new InvalidSemanticContract("Fact event route requires admitted routes and a typed event source.");
        }

        var route = fact.Route!;
        var sources = Model.Application.EventSources.Where(source => string.Equals(source.SourceKind, route.SourceKind, StringComparison.Ordinal)).ToArray();
        if (sources.Length != 1)
        {
            throw new InvalidSemanticContract("Fact event route requires one declared stored source name.");
        }
        var streams = sources[0].Streams.Where(stream => string.Equals(stream.StreamKind, route.StreamKind, StringComparison.Ordinal)).ToArray();
        if (streams.Length != 1)
        {
            throw new InvalidSemanticContract("Fact event route requires one stream belonging to its stored source.");
        }
        var stream = streams[0];
        if (stream.StreamIdType is not null || !stream.StreamIdParts.IsDefaultOrEmpty)
        {
            if (route.StreamId is null)
            {
                throw new InvalidSemanticContract("A keyed fact event route requires a stream identity.");
            }
            if (!SemanticEventRouting.TryDecode(stream, route.StreamId, Model.Application.Concepts, out _, out var failure))
            {
                throw new InvalidSemanticContract(SemanticStreamIdFormatter.FailureMessage(failure));
            }
        }
        else if (route.StreamId is not null)
        {
            throw new InvalidSemanticContract("An unkeyed fact event route cannot carry a stream identity.");
        }

        return SemanticEventRouting.FixtureSourceType(Model.Application, new(sources[0].Id, stream.Id), fact.EventContract, out _)
            ?? throw new InvalidSemanticContract("A routed fact needs an identifier on its source or one unambiguous producer destination type.");
    }

    // A containment level or slice member this walk does not know would be skipped silently, so
    // for_SemanticExecutionPlan/when_inspecting_the_containment_it_traverses holds the ESM shape against it.
    static IEnumerable<SemanticSlice> AllSlices(SemanticApplication application) =>
        application.Modules.SelectMany(_ => AllSlices(_.Features));

    static IEnumerable<SemanticSlice> AllSlices(ImmutableArray<SemanticFeature> features) =>
        features.SelectMany(_ => _.Slices.Concat(AllSlices(_.Features)));
}

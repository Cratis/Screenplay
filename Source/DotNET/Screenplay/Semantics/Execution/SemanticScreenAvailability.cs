// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Execution;

/// <summary>
/// What a caller is offered of one element of a screen.
/// </summary>
public enum SemanticAvailability
{
    /// <summary>The caller satisfies what gates the element.</summary>
    Available = 0,

    /// <summary>The caller does not satisfy what gates the element, or the state it decides from is not recorded.</summary>
    Unavailable = 1,

    /// <summary>The model cannot say: the gate has an opaque predicate, or the artifact is not part of the executable model.</summary>
    Undecided = 2
}

/// <summary>
/// The kind of screen element whose availability is reported.
/// </summary>
public enum SemanticElementKind
{
    /// <summary>An action that issues a command.</summary>
    Action = 0,

    /// <summary>Data a query supplies.</summary>
    Data = 1
}

/// <summary>
/// One element of a screen and whether a caller is offered it.
/// </summary>
/// <param name="Screen">The screen name.</param>
/// <param name="Kind">Whether the element is an action or data.</param>
/// <param name="Artifact">The command or query that gates the element.</param>
/// <param name="Label">The element's label, when it has one.</param>
/// <param name="Availability">The verdict.</param>
/// <param name="Policies">The policies that gate the element, in authored order; empty when nothing gates it.</param>
/// <param name="Reason">Why the element is not available, or what could not be decided.</param>
public sealed record SemanticElementAvailability(
    string Screen,
    SemanticElementKind Kind,
    string Artifact,
    string? Label,
    SemanticAvailability Availability,
    ImmutableArray<string> Policies,
    string? Reason);

/// <summary>
/// Derives which elements of a screen a caller is offered from the authorization of the commands and queries the elements use.
/// </summary>
/// <remarks>
/// The report presents an authorization fact; it never replaces enforcement, and the command or query still decides when it runs.
/// A policy that decides from recorded state is evaluated against the read-model instances given, and an absent instance denies.
/// </remarks>
public static class SemanticScreenAvailability
{
    /// <summary>
    /// Reports the availability of every element of every screen, or of one screen.
    /// </summary>
    /// <param name="plan">The execution plan of the model.</param>
    /// <param name="caller">The caller.</param>
    /// <param name="context">Values of the data the screen is showing, by command or query property name, such as the asset's identifier.</param>
    /// <param name="readModels">The recorded read-model instances policies decide from.</param>
    /// <param name="screen">The screen to report, or null for all.</param>
    /// <returns>The elements and whether the caller is offered each.</returns>
    public static ImmutableArray<SemanticElementAvailability> Evaluate(
        SemanticExecutionPlan plan,
        SemanticCaller caller,
        IReadOnlyDictionary<string, SemanticValue>? context = null,
        ImmutableArray<SemanticReadModelInstance> readModels = default,
        string? screen = null)
    {
        context ??= new Dictionary<string, SemanticValue>(StringComparer.Ordinal);
        var result = ImmutableArray.CreateBuilder<SemanticElementAvailability>();
        foreach (var candidate in ScreensOf(plan).Where(candidate => screen is null || candidate.Name == screen))
        {
            foreach (var data in candidate.Data)
            {
                result.Add(Data(plan, candidate, data, caller, context, readModels));
            }

            foreach (var action in candidate.Actions)
            {
                result.Add(Action(plan, candidate, action, caller, context, readModels));
            }
        }

        return result.ToImmutable();
    }

    static IEnumerable<SemanticScreen> ScreensOf(SemanticExecutionPlan plan) =>
        plan.Model.Application.Modules.SelectMany(module => module.Features.SelectMany(SlicesOf)).SelectMany(slice => slice.Screens);

    static IEnumerable<SemanticSlice> SlicesOf(SemanticFeature feature) => feature.Slices.Concat(feature.Features.SelectMany(SlicesOf));

    static SemanticElementAvailability Action(
        SemanticExecutionPlan plan,
        SemanticScreen screen,
        SemanticScreenAction action,
        SemanticCaller caller,
        IReadOnlyDictionary<string, SemanticValue> context,
        ImmutableArray<SemanticReadModelInstance> readModels)
    {
        if (!plan.Commands.TryGetValue(action.Command, out var command))
        {
            return Undecided(screen, SemanticElementKind.Action, action.Command.ToString(), action.Label, [], "The command is not part of the executable model.");
        }

        var properties = command.Properties.Where(property => !property.IsGenerated).ToArray();
        var artifact = properties.Where(property => context.ContainsKey(property.Name)).ToDictionary(property => property.Name, property => context[property.Name], StringComparer.Ordinal);
        var subject = properties.Where(property => property.IsIdentifier).Select(property => artifact.GetValueOrDefault(property.Name)).FirstOrDefault(value => value is not null);
        return Decide(plan, screen, SemanticElementKind.Action, command.Name, action.Label, command.Authorization, caller, artifact, subject, properties, true, readModels);
    }

    static SemanticElementAvailability Data(
        SemanticExecutionPlan plan,
        SemanticScreen screen,
        SemanticScreenData data,
        SemanticCaller caller,
        IReadOnlyDictionary<string, SemanticValue> context,
        ImmutableArray<SemanticReadModelInstance> readModels)
    {
        if (!plan.Queries.TryGetValue(data.Query, out var query))
        {
            return Undecided(screen, SemanticElementKind.Data, data.Query.ToString(), null, [], "The query is not part of the executable model.");
        }

        var artifact = new Dictionary<string, SemanticValue>(StringComparer.Ordinal);
        SemanticValue? key = null;
        if (query.Argument is not null && context.TryGetValue(query.Argument.Name, out var value))
        {
            artifact[query.Argument.Name] = value;
            key = value;
        }

        var properties = query.Argument is null ? [] : new[] { new SemanticProperty(query.Argument.Id, query.Argument.Name, query.Argument.Type, false) };
        return Decide(plan, screen, SemanticElementKind.Data, query.Name, null, query.Authorization, caller, artifact, key, properties, query.Argument is not null && query.KeyParts.IsEmpty, readModels);
    }

    static SemanticElementAvailability Decide(
        SemanticExecutionPlan plan,
        SemanticScreen screen,
        SemanticElementKind kind,
        string artifactName,
        string? label,
        SemanticAuthorization? authorization,
        SemanticCaller caller,
        IReadOnlyDictionary<string, SemanticValue> artifact,
        SemanticValue? subject,
        IEnumerable<SemanticProperty> properties,
        bool hasPolicySubject,
        ImmutableArray<SemanticReadModelInstance> readModels)
    {
        var policies = PoliciesOf(authorization);
        var decision = SemanticPolicyEvaluation.Evaluate(authorization, plan, caller, artifact, subject, properties, hasPolicySubject: hasPolicySubject, readModels: readModels);
        return decision.Outcome switch
        {
            SemanticPolicyOutcome.Allow => new(screen.Name, kind, artifactName, label, SemanticAvailability.Available, policies, null),
            SemanticPolicyOutcome.Deny => new(screen.Name, kind, artifactName, label, SemanticAvailability.Unavailable, policies, DeniedBy(policies, decision.Details)),
            _ => Undecided(screen, kind, artifactName, label, policies, decision.Details ?? $"Policy '{decision.Policy}' has an opaque predicate.")
        };
    }

    static SemanticElementAvailability Undecided(SemanticScreen screen, SemanticElementKind kind, string artifact, string? label, ImmutableArray<string> policies, string reason) =>
        new(screen.Name, kind, artifact, label, SemanticAvailability.Undecided, policies, reason);

    static string DeniedBy(ImmutableArray<string> policies, string? details) =>
        details ?? (policies.IsEmpty ? "The caller is not authorized." : $"The caller does not satisfy {string.Join(" and ", policies)}.");

    static ImmutableArray<string> PoliciesOf(SemanticAuthorization? authorization) => authorization switch
    {
        SemanticPolicyReference reference => [reference.Name],
        SemanticLogicalAuthorization logical => [.. PoliciesOf(logical.Left), .. PoliciesOf(logical.Right)],
        _ => []
    };
}

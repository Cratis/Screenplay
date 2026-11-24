// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using DeclaredAvailability = Cratis.Screenplay.Semantics.SemanticAvailability;

namespace Cratis.Screenplay.Semantics.Execution;

/// <summary>
/// Decides the availability path of a command or a query: suspension first, then the gates down module, feature, slice and construct.
/// </summary>
/// <remarks>
/// Gates are three-valued. A gate holds, does not hold, or cannot be decided because it carries a <c>when</c> condition the model
/// does not evaluate; unknown never opens a gate.
/// </remarks>
sealed class SemanticAvailabilityPaths
{
    readonly Dictionary<SemanticId, List<(SemanticId Part, ImmutableArray<SemanticAvailabilityRequirement> Gates)>> _paths = [];
    readonly Dictionary<string, DeclaredAvailability> _availabilities;

    internal SemanticAvailabilityPaths(SemanticApplication application)
    {
        _availabilities = (application.Availabilities.IsDefault ? [] : application.Availabilities).ToDictionary(availability => availability.Name, StringComparer.Ordinal);
        foreach (var module in application.Modules)
        {
            List<(SemanticId, ImmutableArray<SemanticAvailabilityRequirement>)> trail = [(module.Id, module.Available)];
            foreach (var feature in module.Features)
            {
                Feature(feature, trail);
            }
        }
    }

    internal enum Verdict
    {
        Open,
        Closed,
        Suspended,
        Unknown
    }

    internal Verdict Decide(SemanticId construct, ImmutableArray<SemanticAvailabilityRequirement> own, SemanticAvailabilityState state)
    {
        var result = Verdict.Open;
        if (_paths.TryGetValue(construct, out var trail))
        {
            if (trail.Exists(step => state.Suspended.Contains(step.Part))) return Verdict.Suspended;
            foreach (var step in trail) result = Combine(result, Gates(step.Gates, state));
        }
        else if (state.Suspended.Contains(construct))
        {
            return Verdict.Suspended;
        }

        return Combine(result, Gates(own, state));
    }

    static Verdict Combine(Verdict left, Verdict right)
    {
        if (left == Verdict.Closed || right == Verdict.Closed) return Verdict.Closed;
        return left == Verdict.Unknown || right == Verdict.Unknown ? Verdict.Unknown : Verdict.Open;
    }

    static bool? And(bool? left, bool? right)
    {
        if (left == false || right == false) return false;
        return left is null || right is null ? null : true;
    }

    static bool? Or(bool? left, bool? right)
    {
        if (left == true || right == true) return true;
        return left is null || right is null ? null : false;
    }

    void Feature(SemanticFeature feature, List<(SemanticId, ImmutableArray<SemanticAvailabilityRequirement>)> inherited)
    {
        List<(SemanticId, ImmutableArray<SemanticAvailabilityRequirement>)> trail = [.. inherited, (feature.Id, feature.Available)];
        foreach (var nested in feature.Features)
        {
            Feature(nested, trail);
        }

        foreach (var slice in feature.Slices)
        {
            List<(SemanticId, ImmutableArray<SemanticAvailabilityRequirement>)> sliceTrail = [.. trail, (slice.Id, slice.Available)];
            foreach (var command in slice.Commands)
            {
                _paths[command.Id] = [.. sliceTrail];
            }

            foreach (var query in slice.Queries)
            {
                _paths[query.Id] = [.. sliceTrail];
            }
        }
    }

    Verdict Gates(ImmutableArray<SemanticAvailabilityRequirement> gates, SemanticAvailabilityState state)
    {
        var result = Verdict.Open;
        if (gates.IsDefaultOrEmpty) return result;
        foreach (var gate in gates)
        {
            result = Combine(result, Holds(gate, state, []) switch
            {
                true => Verdict.Open,
                false => Verdict.Closed,
                _ => Verdict.Unknown
            });
        }

        return result;
    }

    bool? Holds(SemanticAvailabilityRequirement requirement, SemanticAvailabilityState state, HashSet<string> expanding) => requirement switch
    {
        SemanticAudienceRequirement audience => state.Audiences.Contains(audience.Audience) != audience.Negated,
        SemanticLogicalAvailability { Operator: SemanticLogicalOperator.And } logical => And(Holds(logical.Left, state, expanding), Holds(logical.Right, state, expanding)),
        SemanticLogicalAvailability logical => Or(Holds(logical.Left, state, expanding), Holds(logical.Right, state, expanding)),
        SemanticAvailabilityReference reference when _availabilities.TryGetValue(reference.Availability, out var block) && !expanding.Contains(reference.Availability) =>
            block.Requirements.Aggregate((bool?)true, (all, inner) => And(all, Holds(inner, state, [.. expanding, reference.Availability]))),

        // A condition is read from the request context the model does not evaluate, and a reference nothing declares cannot be decided: unknown never opens a gate.
        _ => null
    };
}

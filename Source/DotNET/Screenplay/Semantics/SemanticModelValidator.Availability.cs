// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics;

internal static partial class SemanticModelValidator
{
    static readonly HashSet<string> _reservedProvideNames = new(StringComparer.Ordinal)
    {
        "tenant", "identity", "causedBy", "causation", "occurred", "audiences", "command", "arguments"
    };

    // ESM v53 constructs: audiences, availabilities, availability gates and provided values.
    internal static bool UsesAvailability(SemanticApplication application) =>
        !application.Audiences.IsDefaultOrEmpty ||
        !application.Availabilities.IsDefaultOrEmpty ||
        !application.Provides.IsDefaultOrEmpty ||
        application.Modules.Any(module => !module.Available.IsDefaultOrEmpty || !module.Provides.IsDefaultOrEmpty || module.Features.Any(UsesAvailability)) ||
        SlicesOf(application).Any(slice => !slice.Available.IsDefaultOrEmpty || !slice.Provides.IsDefaultOrEmpty ||
            slice.Commands.Any(command => !command.Available.IsDefaultOrEmpty) ||
            slice.Queries.Any(query => !query.Available.IsDefaultOrEmpty) ||
            slice.Screens.Any(screen => !screen.Available.IsDefaultOrEmpty));

    static bool UsesAvailability(SemanticFeature feature) =>
        !feature.Available.IsDefaultOrEmpty || !feature.Provides.IsDefaultOrEmpty || feature.Features.Any(UsesAvailability);

    static void ValidateAvailabilityVersion(SemanticApplication application, SemanticVersion version)
    {
        ValidateFeatureVersion(
            UsesAvailability(application),
            version,
            SemanticVersion.V53,
            "An ESM v53 model must declare an audience, an availability, an available gate or a provided value.",
            "Audiences, availability gates and provided values require ESM v53.");
        if (!UsesAvailability(application)) return;
        ValidateAvailabilityStructure(application);
    }

    static void ValidateAvailabilityStructure(SemanticApplication application)
    {
        RejectDuplicates(application.Audiences.Select(audience => audience.Name), "audience");
        RejectDuplicates(application.Availabilities.Select(availability => availability.Name), "availability");
        var audiences = application.Audiences.Select(audience => audience.Name).ToHashSet(StringComparer.Ordinal);
        var availabilities = application.Availabilities.ToDictionary(availability => availability.Name, StringComparer.Ordinal);
        var queries = SlicesOf(application).SelectMany(slice => slice.Queries).Select(query => query.Id).ToHashSet();

        foreach (var audience in application.Audiences) RequireName(audience.Name, "audience");
        foreach (var availability in application.Availabilities)
        {
            RequireName(availability.Name, "availability");
            if (availability.Requirements.IsDefault) throw new InvalidSemanticContract($"Availability '{availability.Name}' must carry its requirements.");
            foreach (var requirement in availability.Requirements) ValidateRequirement(requirement, availabilities);
        }

        ValidateAvailabilityCycles(availabilities);
        ValidateProvides(application.Provides, queries, "application");
        foreach (var module in application.Modules)
        {
            ValidateGates(module.Available, availabilities);
            ValidateProvides(module.Provides, queries, $"module '{module.Name}'");
            foreach (var feature in module.Features) ValidateFeatureAvailability(feature, availabilities, queries);
        }
    }

    static void ValidateFeatureAvailability(SemanticFeature feature, Dictionary<string, SemanticAvailability> availabilities, HashSet<SemanticId> queries)
    {
        ValidateGates(feature.Available, availabilities);
        ValidateProvides(feature.Provides, queries, $"feature '{feature.Name}'");
        foreach (var nested in feature.Features) ValidateFeatureAvailability(nested, availabilities, queries);
        foreach (var slice in feature.Slices)
        {
            ValidateGates(slice.Available, availabilities);
            ValidateProvides(slice.Provides, queries, $"slice '{slice.Name}'");
            foreach (var command in slice.Commands) ValidateGates(command.Available, availabilities);
            foreach (var query in slice.Queries) ValidateGates(query.Available, availabilities);
            foreach (var screen in slice.Screens) ValidateGates(screen.Available, availabilities);
        }
    }

    static void ValidateGates(ImmutableArray<SemanticAvailabilityRequirement> gates, Dictionary<string, SemanticAvailability> availabilities)
    {
        if (gates.IsDefaultOrEmpty) return;
        foreach (var gate in gates) ValidateRequirement(gate, availabilities);
    }

    static void ValidateRequirement(SemanticAvailabilityRequirement requirement, Dictionary<string, SemanticAvailability> availabilities)
    {
        switch (requirement)
        {
            case SemanticAudienceRequirement audience:
                RequireName(audience.Audience, "audience reference");
                break;
            case SemanticAvailabilityReference reference:
                if (!availabilities.ContainsKey(reference.Availability)) throw new InvalidSemanticContract($"An availability gate names unknown availability '{reference.Availability}'.");
                break;
            case SemanticConditionRequirement condition:
                ValidateAvailabilityCondition(condition.Condition);
                break;
            case SemanticLogicalAvailability logical:
                ValidateRequirement(logical.Left, availabilities);
                ValidateRequirement(logical.Right, availabilities);
                break;
            default: throw new InvalidSemanticContract("Unknown availability requirement.");
        }
    }

    static void ValidateAvailabilityCondition(SemanticAvailabilityCondition condition)
    {
        switch (condition)
        {
            case SemanticAvailabilityComparison comparison:
                RequireContextPath(comparison.Path);
                ValidateContextOperand(comparison.Operand);
                break;
            case SemanticLogicalAvailabilityCondition logical:
                ValidateAvailabilityCondition(logical.Left);
                ValidateAvailabilityCondition(logical.Right);
                break;
            default: throw new InvalidSemanticContract("Unknown availability condition.");
        }
    }

    static void ValidateContextOperand(SemanticContextOperand operand)
    {
        if ((operand.Path is null) == (operand.Value is null)) throw new InvalidSemanticContract("A context operand is either a context path or a literal.");
        if (operand.Path is not null) RequireContextPath(operand.Path);
    }

    static void RequireContextPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || path.StartsWith("$context", StringComparison.Ordinal) || path.StartsWith('.') || path.EndsWith('.'))
        {
            throw new InvalidSemanticContract($"'{path}' is not a context path; write it without the '$context.' prefix, for example 'vessel.hasReported'.");
        }
    }

    static void ValidateProvides(ImmutableArray<SemanticProvidedValue> provides, HashSet<SemanticId> queries, string owner)
    {
        if (provides.IsDefaultOrEmpty) return;
        RejectDuplicates(provides.Select(provided => provided.Name), $"provided value of {owner}");
        foreach (var provided in provides)
        {
            RequireName(provided.Name, "provided value");
            if ((provided.Type is null) == (provided.ReadModel is null)) throw new InvalidSemanticContract($"Provided value '{provided.Name}' needs exactly one of a type or a read model.");
            if (_reservedProvideNames.Contains(provided.Name)) throw new InvalidSemanticContract($"'{provided.Name}' is a built-in context name and cannot be provided.");
            switch (provided.Source)
            {
                case SemanticQueryProvideSource query:
                    if (!queries.Contains(query.Query)) throw new InvalidSemanticContract($"Provided value '{provided.Name}' names an unknown query.");
                    foreach (var operand in query.By.IsDefault ? [] : query.By) ValidateContextOperand(operand);
                    break;
                case SemanticHostProvideSource:
                    break;
                case SemanticValueProvideSource value:
                    ValidateContextOperand(value.Value);
                    break;
                default: throw new InvalidSemanticContract($"Provided value '{provided.Name}' has an unknown source.");
            }
        }
    }

    static void ValidateAvailabilityCycles(Dictionary<string, SemanticAvailability> availabilities)
    {
        var state = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var name in availabilities.Keys) Visit(name, availabilities, state);

        static void Visit(string name, Dictionary<string, SemanticAvailability> availabilities, Dictionary<string, int> state)
        {
            if (state.TryGetValue(name, out var seen))
            {
                if (seen == 1) throw new InvalidSemanticContract($"Availability '{name}' builds on itself.");
                return;
            }

            state[name] = 1;
            foreach (var requirement in availabilities[name].Requirements)
            {
                foreach (var reference in References(requirement).Where(availabilities.ContainsKey)) Visit(reference, availabilities, state);
            }

            state[name] = 2;
        }

        static IEnumerable<string> References(SemanticAvailabilityRequirement requirement) => requirement switch
        {
            SemanticAvailabilityReference reference => [reference.Availability],
            SemanticLogicalAvailability logical => References(logical.Left).Concat(References(logical.Right)),
            _ => []
        };
    }

    static void RejectDuplicates(IEnumerable<string> names, string description)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var name in names)
        {
            if (!seen.Add(name)) throw new InvalidSemanticContract($"The {description} '{name}' is declared more than once.");
        }
    }

    static void RequireName(string? name, string description)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new InvalidSemanticContract($"An {description} needs a name.");
    }
}

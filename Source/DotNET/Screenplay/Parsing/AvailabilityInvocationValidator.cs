// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Warns when a screen action, form or reaction <c>invokes</c> a command whose audiences can be off while the invoker's are on.
/// </summary>
/// <remarks>
/// Audiences are compared as sets. A <c>when</c> condition is opaque and is skipped, never guessed. A reaction is never gated,
/// because events and observers are unaffected by availability, so it is on for every audience combination.
/// </remarks>
internal static class AvailabilityInvocationValidator
{
    const int MaximumAudiences = 12;

    /// <summary>
    /// Validates the invocations of an application.
    /// </summary>
    /// <param name="application">The <see cref="ApplicationSyntax"/> to validate.</param>
    /// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
    internal static void Validate(ApplicationSyntax application, ParserContext context)
    {
        var blocks = application.Availabilities
            .GroupBy(availability => availability.Name, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        var collection = new Collection();
        foreach (var module in application.Modules)
        {
            collection.Module(module);
        }

        foreach (var invocation in collection.Invocations)
        {
            if (!collection.Commands.TryGetValue(invocation.Command, out var target) || target.Count == 0)
            {
                continue;
            }

            if (CanBeOffWhileOn(invocation.Gates, target, blocks))
            {
                context.Warning(
                    DiagnosticCodes.AvailabilityHidesInvokedCommand,
                    $"{invocation.Kind} invokes command '{invocation.Command}', which can be unavailable while this {invocation.Kind} is available - give it the same 'available' gate, or accept that the invocation may be refused as not available",
                    invocation.Location);
            }
        }
    }

    static bool CanBeOffWhileOn(IReadOnlyList<AvailableSyntax> invoker, IReadOnlyList<AvailableSyntax> target, Dictionary<string, AvailabilitySyntax> blocks)
    {
        var audiences = invoker.Concat(target)
            .SelectMany(gate => AudiencesOf(gate.Requirement, blocks, []))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (audiences.Count == 0 || audiences.Count > MaximumAudiences)
        {
            return false;
        }

        for (var mask = 0; mask < 1 << audiences.Count; mask++)
        {
            var on = new HashSet<string>(audiences.Where((_, index) => (mask & (1 << index)) != 0), StringComparer.Ordinal);
            if (invoker.All(gate => Holds(gate.Requirement, on, blocks, [])) && !target.All(gate => Holds(gate.Requirement, on, blocks, [])))
            {
                return true;
            }
        }

        return false;
    }

    static IEnumerable<string> AudiencesOf(AvailabilityRequirementSyntax requirement, Dictionary<string, AvailabilitySyntax> blocks, HashSet<string> expanding) => requirement switch
    {
        AudienceAvailabilityRequirementSyntax audience => [audience.Audience],
        LogicalAvailabilityRequirementSyntax logical => AudiencesOf(logical.Left, blocks, expanding).Concat(AudiencesOf(logical.Right, blocks, expanding)),
        AvailabilityReferenceSyntax reference when blocks.TryGetValue(reference.Availability, out var block) && expanding.Add(reference.Availability) =>
            [.. block.Requirements.SelectMany(inner => AudiencesOf(inner, blocks, expanding))],
        _ => []
    };

    static bool Holds(AvailabilityRequirementSyntax requirement, HashSet<string> on, Dictionary<string, AvailabilitySyntax> blocks, HashSet<string> expanding) => requirement switch
    {
        AudienceAvailabilityRequirementSyntax audience => on.Contains(audience.Audience),
        LogicalAvailabilityRequirementSyntax { Operator: LogicalOperator.And } logical => Holds(logical.Left, on, blocks, expanding) && Holds(logical.Right, on, blocks, expanding),
        LogicalAvailabilityRequirementSyntax logical => Holds(logical.Left, on, blocks, expanding) || Holds(logical.Right, on, blocks, expanding),
        AvailabilityReferenceSyntax reference when blocks.TryGetValue(reference.Availability, out var block) && !expanding.Contains(reference.Availability) =>
            block.Requirements.All(inner => Holds(inner, on, blocks, [.. expanding, reference.Availability])),

        // A condition is opaque and skipped; so is a reference nothing declares or that builds on itself.
        _ => true
    };

    sealed record Invocation(string Kind, string Command, IReadOnlyList<AvailableSyntax> Gates, SourceLocation Location);

    sealed class Collection
    {
        public Dictionary<string, IReadOnlyList<AvailableSyntax>> Commands { get; } = new(StringComparer.Ordinal);

        public List<Invocation> Invocations { get; } = [];

        public void Module(ModuleSyntax module)
        {
            IReadOnlyList<AvailableSyntax> gates = [.. module.Available];
            Forms(module.Forms, gates);
            foreach (var feature in module.Features)
            {
                Feature(feature, gates);
            }
        }

        void Feature(FeatureSyntax feature, IReadOnlyList<AvailableSyntax> inherited)
        {
            IReadOnlyList<AvailableSyntax> gates = [.. inherited, .. feature.Available];
            foreach (var slice in feature.Slices)
            {
                Slice(slice, gates);
            }

            foreach (var nested in feature.Features)
            {
                Feature(nested, gates);
            }
        }

        void Forms(IEnumerable<FormSyntax>? forms, IReadOnlyList<AvailableSyntax> gates)
        {
            foreach (var form in forms ?? [])
            {
                Invocations.Add(new("form", form.For, gates, form.Location));
            }
        }

        void Slice(SliceSyntax slice, IReadOnlyList<AvailableSyntax> inherited)
        {
            IReadOnlyList<AvailableSyntax> gates = [.. inherited, .. slice.Available];
            foreach (var command in slice.Commands)
            {
                Commands.TryAdd(command.Name, [.. gates, .. command.Available]);
            }

            foreach (var screen in slice.Screens)
            {
                IReadOnlyList<AvailableSyntax> screenGates = [.. gates, .. screen.Available];
                var actions = new ActionCollector();
                actions.VisitScreen(screen);
                foreach (var action in actions.Actions)
                {
                    Invocations.Add(new("screen action", action.Command, screenGates, action.Location));
                }
            }

            foreach (var invokes in slice.Reactions.SelectMany(reaction => reaction.Triggers).SelectMany(trigger => trigger.Invokes ?? []))
            {
                Invocations.Add(new("reaction", invokes.Command, [], invokes.Location));
            }
        }
    }

    sealed class ActionCollector : ScreenplaySyntaxWalker
    {
        public List<ScreenActionSyntax> Actions { get; } = [];

        public override void VisitScreenAction(ScreenActionSyntax syntax)
        {
            Actions.Add(syntax);
            base.VisitScreenAction(syntax);
        }
    }
}

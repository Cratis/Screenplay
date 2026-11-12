// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics;

public sealed partial class SemanticModelBinder
{
    private sealed partial class BindingContext
    {
        readonly Dictionary<string, SemanticId> _systemIds = new(StringComparer.Ordinal);
        string? _invocationBranch;
        AbilitySyntax? _invocationAbility;

        SemanticId SystemId(SystemSyntax system)
        {
            if (_systemIds.TryGetValue(system.Name, out var id)) return id;
            id = Resolve(SemanticAddress.ForSystem(_applicationIdentity, system.Name), system.Location);
            _systemIds[system.Name] = id;
            return id;
        }

        (SystemSyntax System, AbilitySyntax Ability)? FindAbility(string written)
        {
            var dot = written.IndexOf('.', StringComparison.Ordinal);
            if (dot <= 0) return null;
            var systemName = written[..dot];
            var abilityName = written[(dot + 1)..];
            var system = syntax.Systems.FirstOrDefault(candidate => candidate.Name == systemName);
            var ability = system?.Abilities.FirstOrDefault(candidate => candidate.Name == abilityName);
            return system is null || ability is null ? null : (system, ability);
        }

        SemanticInvocation? BindAbilityInvocation(
            ReactionSyntax reaction,
            SemanticReactionTriggerKind kind,
            InvokesSyntax invoked,
            SemanticExpressionRootKind root,
            Dictionary<string, SemanticProperty> values)
        {
            if (FindAbility(invoked.Command) is not { } found)
            {
                Error(DiagnosticCodes.InvalidSemanticBinding, $"Reaction '{reaction.Name}' invokes '{invoked.Command}', which is not one declared ability.", invoked.Location);
                return null;
            }

            var (system, ability) = found;
            var systemId = SystemId(system);
            var targets = ability.Inputs.ToDictionary(input => input.Name, StringComparer.Ordinal);
            var inputs = ImmutableArray.CreateBuilder<SemanticAbilityInputMapping>();
            foreach (var mapping in invoked.Mappings)
            {
                if (!targets.TryGetValue(mapping.Property, out var input))
                {
                    Error(DiagnosticCodes.InvalidSemanticBinding, $"Reaction '{reaction.Name}' gives '{invoked.Command}' the value '{mapping.Property}', which the ability does not declare.", mapping.Location);
                    continue;
                }

                var type = BindTypeReference(input.Type);
                if (!IsCompatibleMapping(mapping, values, new SemanticProperty(default, input.Name, type, false), $"ability '{invoked.Command}'")) continue;
                if (BindOccurrenceSource(mapping.Source, type, root, values, "ability input") is { } source) inputs.Add(new(input.Name, source));
            }

            var branches = ImmutableArray.CreateBuilder<SemanticInvocationBranch>();
            foreach (var branch in invoked.Branches)
            {
                _invocationBranch = branch.Selector;
                _invocationAbility = ability;
                try
                {
                    branches.Add(new(
                        branch.Selector,
                        [.. branch.Produces.Select(produced => BindReactionProduces(reaction, kind, produced, root, values)).OfType<SemanticProducedEvent>()]));
                }
                finally
                {
                    _invocationBranch = null;
                    _invocationAbility = null;
                }
            }

            return new(default, []) { System = systemId, Ability = ability.Name, Inputs = inputs.ToImmutable(), Branches = branches.ToImmutable() };
        }

        SemanticInvocationValueExpression? BindInvocationValue(InvocationValueExpressionSyntax value, SemanticTypeReference target)
        {
            var text = SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text);
            SemanticTypeReference? type = null;
            switch (value.Source)
            {
                case "outcome" when value.Member.Length == 0 && InvocationBranchScope.IsOutcome(_invocationBranch):
                case "failure" when InvocationBranchScope.IsFailureMember(value.Member) && InvocationBranchScope.Is(_invocationBranch, "failed"):
                    type = text;
                    break;
                case "result" when InvocationBranchScope.Is(_invocationBranch, "returned") && _invocationAbility?.Returns.FirstOrDefault(field => field.Name == value.Member) is { } returned:
                    type = BindTypeReference(returned.Type);
                    break;
            }

            if (type is null)
            {
                Error(DiagnosticCodes.InvocationValueOutsideBranch, $"'${value.Source}{(value.Member.Length == 0 ? string.Empty : "." + value.Member)}' is unavailable in this mapping scope.", value.Location);
                return null;
            }

            if (!SemanticTypeCompatibility.IsCompatibleSource(type, target))
            {
                Error(DiagnosticCodes.InvalidSemanticBinding, $"'${value.Source}{(value.Member.Length == 0 ? string.Empty : "." + value.Member)}' cannot be assigned to the target of its mapping.", value.Location);
                return null;
            }

            return new(value.Source, value.Member, type);
        }
    }
}

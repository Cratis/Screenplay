// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Parsing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics;

public sealed partial class SemanticModelBinder
{
    private sealed partial class BindingContext
    {
        static bool IsAbilityTarget(string? target) => target?.Contains('.', StringComparison.Ordinal) == true;

        /// <summary>Gets the node results (name to returned fields) a workflow's ability and compute nodes make readable.</summary>
        Dictionary<string, ImmutableArray<SemanticAbilityField>> WorkflowNodeResults(WorkflowSyntax workflow)
        {
            var results = new Dictionary<string, ImmutableArray<SemanticAbilityField>>(StringComparer.Ordinal);
            foreach (var node in workflow.Nodes)
            {
                if (node.NodeKind == WorkflowNodeKind.Compute)
                {
                    results[node.Name] = [.. node.Returns.Select(field => new SemanticAbilityField(field.Name, BindTypeReference(field.Type)))];
                }
                else if (node.NodeKind == WorkflowNodeKind.Action && IsAbilityTarget(node.Command) && FindAbility(node.Command!) is { } found)
                {
                    results[node.Name] = [.. found.Ability.Returns.Select(field => new SemanticAbilityField(field.Name, BindTypeReference(field.Type)))];
                }
            }

            return results;
        }

        /// <summary>Binds the ability call a workflow action or a state entry makes.</summary>
        (SemanticId System, string Ability, ImmutableArray<SemanticAbilityInputMapping> Inputs)? BindLifecycleAbility(
            string owner,
            string written,
            IEnumerable<PropertyMappingSyntax> mappings,
            SourceLocation location,
            Dictionary<string, SemanticProperty> subject,
            Dictionary<string, ImmutableArray<SemanticAbilityField>>? nodeResults)
        {
            if (FindAbility(written) is not { } found)
            {
                Error(DiagnosticCodes.InvalidSemanticBinding, $"{owner} invokes '{written}', which is not one declared ability.", location);
                return null;
            }

            var (system, ability) = found;
            var declared = ability.Inputs.ToDictionary(input => input.Name, StringComparer.Ordinal);
            var inputs = ImmutableArray.CreateBuilder<SemanticAbilityInputMapping>();
            foreach (var mapping in mappings)
            {
                if (!declared.TryGetValue(mapping.Property, out var input))
                {
                    Error(DiagnosticCodes.InvalidSemanticBinding, $"{owner} gives '{written}' the value '{mapping.Property}', which the ability does not declare.", mapping.Location);
                    continue;
                }

                if (BindLifecycleSource(mapping, BindTypeReference(input.Type), subject, nodeResults, $"ability '{written}' input '{input.Name}'") is { } source)
                {
                    inputs.Add(new(input.Name, source));
                }
            }

            return (SystemId(system), ability.Name, inputs.ToImmutable());
        }

        SemanticExpression? BindLifecycleSource(
            PropertyMappingSyntax mapping,
            SemanticTypeReference target,
            Dictionary<string, SemanticProperty> subject,
            Dictionary<string, ImmutableArray<SemanticAbilityField>>? nodeResults,
            string description)
        {
            switch (mapping.Source)
            {
                case LiteralExpressionSyntax literal:
                    return BindConcreteValue(literal, target, description, false, false) is { } value ? SemanticExpression.FromValue(value) : null;
                case PathExpressionSyntax path:
                    var dot = path.Path.IndexOf('.', StringComparison.Ordinal);
                    if (dot > 0 && nodeResults is not null && nodeResults.TryGetValue(path.Path[..dot], out var returned))
                    {
                        if (returned.FirstOrDefault(field => field.Name == path.Path[(dot + 1)..]) is not { } field)
                        {
                            Error(DiagnosticCodes.InvalidSemanticBinding, $"'{path.Path}' reads a field node '{path.Path[..dot]}' does not return.", mapping.Location);
                            return null;
                        }

                        return CompatibleInvocationValue(new("node", path.Path, field.Type), target, mapping, path.Path);
                    }

                    if (subject.TryGetValue(path.Path, out var property))
                    {
                        return CompatibleInvocationValue(new("subject", path.Path, property.Type), target, mapping, path.Path);
                    }

                    if (EnumerationMember(path.Path, target) is { } member) return SemanticExpression.FromValue(SemanticValue.Text(member));
                    Error(DiagnosticCodes.InvalidSemanticBinding, $"'{path.Path}' is neither a property of the subject nor a result of an earlier node.", mapping.Location);
                    return null;
                default:
                    Error(DiagnosticCodes.InvalidSemanticBinding, $"Mapping '{mapping.Property}' in {description} can only assign a literal, a subject property or a node result.", mapping.Location);
                    return null;
            }
        }

        SemanticInvocationValueExpression? CompatibleInvocationValue(
            SemanticInvocationValueExpression value,
            SemanticTypeReference target,
            PropertyMappingSyntax mapping,
            string written)
        {
            if (SemanticTypeCompatibility.IsCompatibleSource(value.Type, target) ||
                SemanticTypeCompatibility.ConvertsConcept(value.Type, target, id => _boundConcepts.GetValueOrDefault(id)))
            {
                return value;
            }

            Error(DiagnosticCodes.InvalidSemanticBinding, $"'{written}' cannot be assigned to '{mapping.Property}': the types are not compatible.", mapping.Location);
            return null;
        }

        string? RequireLifecycleBody(SemanticImplementationRole role, SemanticAddress? owner, string member, FileReferenceSyntax? file, CodeBlockSyntax? code)
        {
            if (owner is not { } address || (file is null && code is null)) return null;
            return RequireImplementation(role, address, file, code, member)?.RequirementId;
        }

        /// <summary>Reports an unadmitted node-result read in a decision condition.</summary>
        bool ReadsNodeResult(WorkflowNodeSyntax node, WorkflowSyntax workflow)
        {
            if (node.Condition is null) return false;
            var results = WorkflowNodeResults(workflow);
            var reads = results.Count > 0 && ConditionReads.Heads(node.Condition).Any(results.ContainsKey);
            if (reads)
            {
                Error(
                    DiagnosticCodes.UnsupportedSemanticSyntax,
                    $"Decide '{node.Name}' reads an ability or compute node result, which no supported executable model (ESM) version carries in a condition yet (#1252).",
                    node.Location,
                    DiagnosticArguments.ForIssue("Cratis/Screenplay#1252"));
            }

            return reads;
        }
    }
}

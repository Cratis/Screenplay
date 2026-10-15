// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Generation;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces;

internal static class WorkspaceGenerationRuleRepairs
{
    internal static ImmutableArray<WorkspaceDiagnosticRepair> Find(WorkspaceSyntaxIndex index, WorkspaceRevision revision, Diagnostic diagnostic, bool verify)
    {
        foreach (var entry in index.Entries.Where(entry => entry.Handle.Revision == revision && entry.Node is ConceptSyntax))
        {
            var concept = (ConceptSyntax)entry.Node;
            var blocks = (concept.Validations ?? []).OfType<DeclarativeValidateSyntax>().ToArray();
            var rules = blocks.SelectMany(block => block.Rules).Where(rule => rule.Location == diagnostic.Location).ToArray();
            if (rules.Length != 1) continue;
            var generation = Generation(concept.Generation);
            if (DerivedGenerationRepairPattern.Create(generation, out _) is not { } pattern) return [];
            var original = rules[0];
            var replacement = new ValidationRuleSyntax(ValidationRuleSyntax.ConceptValue, ValidationRuleKind.Matches, new LiteralExpressionSyntax(pattern, original.Location), original.Message, original.Location) { Severity = original.Severity };
            var replacements = ImmutableArray.Create(replacement);
            if (generation?.Checksum is { } checksum && !blocks.SelectMany(block => block.Rules).Any(rule => rule.Rule == ValidationRuleKind.Checksum && rule.Value is LiteralExpressionSyntax { Value: string algorithm } && algorithm == checksum))
            {
                replacements = replacements.Add(new(ValidationRuleSyntax.ConceptValue, ValidationRuleKind.Checksum, new LiteralExpressionSyntax(checksum, original.Location), null, original.Location));
            }
            var changed = concept with
            {
                Validations = [.. (concept.Validations ?? []).Select(block => block is DeclarativeValidateSyntax declarative
                    ? declarative with { Rules = [.. declarative.Rules.SelectMany(rule => rule == original ? replacements : [rule])] } : block)]
            };
            var repair = new WorkspaceDiagnosticRepair(diagnostic.Code, entry.Handle, [new ReplaceWorkspaceNode(entry.Handle, concept, changed)])
            {
                Title = $"Replace with the pattern derived from {generation?.Kind ?? "uuid"}{(replacements.Length > 1 ? " and add checksum validation" : string.Empty)} (also validates inputs and stored values)",
                CanFixAll = false
            };

            return WorkspaceRepairVerification.Discover(index, repair, verify);
        }

        return [];
    }

    internal static string? UnavailableReason(WorkspaceSyntaxIndex index, WorkspaceNodeHandle subject)
    {
        if (index.Find(subject)?.Node is not ConceptSyntax concept) return null;
        _ = DerivedGenerationRepairPattern.Create(Generation(concept.Generation), out var reason);

        return reason;
    }

    static SemanticGeneration? Generation(GenerationSyntax? generation) => generation is null ? null : new(generation.Strategy)
    {
        Prefix = generation.Prefix,
        Case = generation.Case,
        Size = generation.Strategy == "nanoid" ? generation.Size ?? 21 : generation.Size,
        Alphabet = generation.Alphabet,
        Count = generation.Count,
        Encoding = generation.Encoding,
        Pattern = generation.Pattern,
        Checksum = generation.Checksum
    };
}

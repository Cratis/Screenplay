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
            if (DerivedPattern.Create(generation) is not { } pattern) return [];
            var original = rules[0];
            var replacement = new ValidationRuleSyntax(ValidationRuleSyntax.ConceptValue, ValidationRuleKind.Matches, new LiteralExpressionSyntax(pattern, original.Location), original.Message, original.Location) { Severity = original.Severity };
            var changed = concept with
            {
                Validations = [.. (concept.Validations ?? []).Select(block => block is DeclarativeValidateSyntax declarative
                    ? declarative with { Rules = [.. declarative.Rules.Select(rule => rule == original ? replacement : rule)] } : block)]
            };
            var repair = new WorkspaceDiagnosticRepair(diagnostic.Code, entry.Handle, [new ReplaceWorkspaceNode(entry.Handle, concept, changed)])
            {
                Title = $"Replace with the pattern derived from {generation?.Kind ?? "uuid"} (also validates inputs)",
                CanFixAll = false
            };

            return WorkspaceRepairVerification.Discover(index, repair, verify);
        }

        return [];
    }

    static SemanticGeneration? Generation(GenerationSyntax? generation) => generation is null ? null : new(generation.Strategy)
    {
        Prefix = generation.Prefix,
        Case = generation.Case,
        Size = generation.Strategy == "nanoid" ? generation.Size ?? 21 : generation.Size,
        Alphabet = generation.Alphabet,
        Count = generation.Count,
        Encoding = generation.Encoding
    };
}

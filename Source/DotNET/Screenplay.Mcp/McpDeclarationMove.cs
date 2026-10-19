// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Mcp;

/// <summary>
/// Decides which specifications travel with a single declaration moved between slices, and refuses a move that
/// breaks specification obligations (decision 0100).
/// </summary>
static class McpDeclarationMove
{
    static readonly Dictionary<SemanticKind, string> _kinds = new()
    {
        [SemanticKind.Command] = "Command",
        [SemanticKind.EventContract] = "Event",
        [SemanticKind.ReadModel] = "ReadModel",
        [SemanticKind.Projection] = "Projection",
        [SemanticKind.Query] = "Query",
        [SemanticKind.Reaction] = "Reaction",
        [SemanticKind.Capture] = "Capture"
    };

    static readonly JsonElement _everything = JsonDocument.Parse("{}").RootElement;

    /// <summary>
    /// Whether the request moves one slice-level declaration to a slice.
    /// </summary>
    /// <param name="target">The move target.</param>
    /// <param name="newParent">The move destination.</param>
    /// <returns>True for a declaration move.</returns>
    internal static bool Applies(SemanticAddress target, SemanticAddress newParent) => _kinds.ContainsKey(target.Kind) && newParent.Kind == SemanticKind.Slice;

    /// <summary>
    /// Finds the specifications of the source slice that cover only the moved declaration.
    /// </summary>
    /// <param name="snapshot">The workspace before the move.</param>
    /// <param name="target">The declaration to move.</param>
    /// <returns>The specifications to carry; a refusal when a covering specification also covers a staying declaration.</returns>
    internal static (ImmutableArray<SemanticAddress> Carried, string? Refusal) Carried(McpSnapshot snapshot, SemanticAddress target)
    {
        var index = snapshot.Index;
        var path = Path(target);
        var slice = path[..path.LastIndexOf('.')];
        var members = index.Declarations.Where(declaration => string.Join('.', declaration.Scope) == slice).ToArray();
        var moved = members.Where(declaration => declaration.Address == path && declaration.Kind == _kinds[target.Kind]).ToArray();
        if (moved.Length != 1) return ([], null);
        var obligations = McpSpecificationObligations.Evaluate(snapshot, _everything);
        var sliceAddress = SemanticAddress.FromCanonical(SemanticKind.Slice, target.Parts[..^1]);
        var carried = ImmutableArray.CreateBuilder<SemanticAddress>();
        foreach (var specification in members.Where(declaration => declaration.Syntax is SpecificationSyntax))
        {
            var covered = obligations.Where(obligation => obligation.MetBy.Contains(specification.Owner)).Select(obligation => obligation.Declaration)
                .Concat(members.Where(member => Drives(index, specification, member)).Select(member => member.Owner))
                .Where(owner => members.Any(member => member.Owner == owner)).Distinct().ToArray();
            if (!covered.Contains(moved[0].Owner)) continue;
            var staying = covered.Where(owner => owner != moved[0].Owner).ToArray();
            if (staying.Length > 0)
            {
                return ([], $"Specification '{specification.Address}' also covers {string.Join(", ", staying.Select(owner => $"{owner.Kind} '{owner.Address}'"))}, which stay in '{slice}'. Split the specification, or move those declarations too.");
            }
            carried.Add(SemanticAddress.ForSpecification(sliceAddress, specification.Name));
        }
        return (carried.ToImmutable(), null);
    }

    /// <summary>
    /// Refuses a move after which a specification obligation is no longer met, or a new one is unmet.
    /// </summary>
    /// <param name="before">The workspace before the move.</param>
    /// <param name="after">The candidate workspace.</param>
    /// <param name="migrations">Every logical address the move rewrites, previous to current.</param>
    /// <returns>A refusal, or null when every obligation keeps its status.</returns>
    internal static string? Obligations(McpSnapshot before, McpSnapshot after, IReadOnlyDictionary<SemanticAddress, SemanticAddress> migrations)
    {
        var moved = migrations.Where(migration => _kinds.ContainsKey(migration.Key.Kind))
            .ToDictionary(migration => (_kinds[migration.Key.Kind], Path(migration.Key)), migration => Path(migration.Value));
        var previous = Status(before, (kind, address) => moved.GetValueOrDefault((kind, address)) ?? address);
        var current = Status(after, (_, address) => address);
        var broken = previous.Where(pair => pair.Value && !current.GetValueOrDefault(pair.Key)).Select(pair => pair.Key)
            .Concat(current.Where(pair => !pair.Value && !previous.ContainsKey(pair.Key)).Select(pair => pair.Key)).ToArray();
        return broken.Length == 0
            ? null
            : "The move would leave specification obligations unmet: " + string.Join("; ", broken.Select(key => $"{key.RuleId} of {key.Kind} '{key.Address}' ({key.Subject})")) + ". Move the covering specifications with the declaration, or write them first.";
    }

    static Dictionary<(string RuleId, string Kind, string Address, string Subject), bool> Status(McpSnapshot snapshot, Func<string, string, string> address) =>
        McpSpecificationObligations.Evaluate(snapshot, _everything)
            .GroupBy(obligation => (obligation.RuleId, obligation.Declaration.Kind, Address: address(obligation.Declaration.Kind, obligation.Declaration.Address), obligation.Subject))
            .ToDictionary(group => group.Key, group => group.Any(obligation => obligation.Met));

    static bool Drives(McpSyntaxIndex index, McpDeclaration specification, McpDeclaration member)
    {
        var syntax = (SpecificationSyntax)specification.Syntax;
        (string? Name, string Kind)[] subjects =
        [
            (syntax.When?.CommandType, "Command"), (syntax.WhenAppended?.EventType, "Event"), (syntax.WhenRedelivered?.Reaction, "Reaction"),
            (syntax.WhenTrigger?.Trigger, "Trigger"), (syntax.WhenCapture?.Capture, "Capture"), (syntax.WhenQuery?.Query, "Query")
        ];
        return subjects.Any(subject => subject.Name is not null && subject.Kind == member.Kind && McpReviewSelection.Resolves(index, specification, subject.Name, subject.Kind, member));
    }

    static string Path(SemanticAddress address) => string.Join('.', address.Parts.Skip(1).Select(part => part.Key));
}

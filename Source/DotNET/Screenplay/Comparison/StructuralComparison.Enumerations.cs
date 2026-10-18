// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Comparison;

internal static partial class StructuralComparison
{
    static void Enumerations(string? id, string kind, string previous, string next, SyntaxNode[] old, SyntaxNode[] current, bool stored, List<Change> changes, Action<Change> add)
    {
        if (old.Length != 1 || current.Length != 1 || old[0] is not ConceptSyntax { IsEnum: true } before || current[0] is not ConceptSyntax { IsEnum: true } after ||
            !ComparableEnumeration(before) || !ComparableEnumeration(after))
        {
            return;
        }

        var left = EnumerationNumbers(before);
        var right = EnumerationNumbers(after);
        var holders = right.ToDictionary(pair => pair.Value, pair => pair.Key);
        var beforeNumbering = before.ValueNumbers is null ? "positional" : "explicit";
        var afterNumbering = after.ValueNumbers is null ? "positional" : "explicit";
        foreach (var member in left)
        {
            string changeKind;
            string? afterMember;
            int? afterNumber;
            if (right.TryGetValue(member.Key, out var number))
            {
                if (number == member.Value) continue;
                changeKind = "enum-number-changed";
                afterMember = member.Key;
                afterNumber = number;
            }
            else if (holders.TryGetValue(member.Value, out var holder))
            {
                changeKind = beforeNumbering == "explicit" && afterNumbering == "explicit" && !left.ContainsKey(holder) ? "enum-member-renamed" : "enum-number-reused";
                afterMember = holder;
                afterNumber = member.Value;
            }
            else
            {
                changeKind = "enum-member-removed";
                afterMember = null;
                afterNumber = null;
            }
            var change = new Change("members", changeKind, id, kind, previous, next, member.Key, ContractBreaking: changeKind != "enum-member-renamed", AfterMember: afterMember, BeforeNumber: member.Value, AfterNumber: afterNumber, BeforeNumbering: beforeNumbering, AfterNumbering: afterNumbering, Stored: stored);
            if (id is null) changes.Add(change);
            else add(change);
        }
    }

    static bool ComparableEnumeration(ConceptSyntax concept)
    {
        var names = concept.Values.ToArray();
        var numbers = concept.ValueNumbers?.ToArray();
        return names.Distinct(StringComparer.Ordinal).Count() == names.Length &&
            (numbers is null || (numbers.Length == names.Length && numbers.Distinct().Count() == numbers.Length));
    }

    static Dictionary<string, int> EnumerationNumbers(ConceptSyntax concept)
    {
        var numbers = concept.ValueNumbers?.ToArray();
        return concept.Values.Select((name, index) => (name, number: numbers?[index] ?? index)).ToDictionary(member => member.name, member => member.number, StringComparer.Ordinal);
    }
}

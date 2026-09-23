// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Printing;

public sealed partial class ScreenplayPrinter
{
    static void AddMembers<T>(List<PrintableMember> members, IEnumerable<T> nodes, int kind, Action<T> print)
        where T : SyntaxNode
    {
        members.AddRange(nodes.Select(node => new PrintableMember(node, kind, () => print(node))));
    }

    static void AddSeparatedMembers<T>(List<PrintableMember> members, ScreenplayWriter writer, IEnumerable<T> nodes, int kind, Action<ScreenplayWriter, T> print)
        where T : SyntaxNode
    {
        AddMembers(members, nodes, kind, node =>
        {
            writer.Blank();
            print(writer, node);
        });
    }

    /// <summary>
    /// Prints members in their authored order when their positions share a document, otherwise in canonical order.
    /// </summary>
    /// <remarks>
    /// Source locations suffice for parsed siblings in one document: their lines are unique, and no public
    /// syntax constructor or JSON shape needs to change. A folder merge can combine different paths, whose
    /// line numbers cannot be compared; in that case the existing kind order is the deterministic fallback.
    /// Start/default locations mark newly authored nodes. Insert those after the last member of their kind,
    /// or before the first member of a later canonical kind if none exists. Replacing a declaration with a
    /// default-located node follows the same insertion rule; retaining its location retains its position.
    /// </remarks>
    static void WriteMembers(List<PrintableMember> members)
    {
        var located = members.Where(member => member.Node.Location is { Line: > 1, Column: > 0 }).ToList();
        var sameDocument = located.Count > 0 && located.TrueForAll(member =>
            string.Equals(member.Node.Location.Path, located[0].Node.Location.Path, StringComparison.Ordinal));

        if (!sameDocument)
        {
            foreach (var member in members.OrderBy(member => member.Kind))
            {
                member.Print();
            }

            return;
        }

        var ordered = located.OrderBy(member => member.Node.Location.Line)
            .ThenBy(member => member.Node.Location.Column).ToList();
        foreach (var member in members.Except(located))
        {
            var lastOfKind = ordered.FindLastIndex(existing => existing.Kind == member.Kind);
            var position = lastOfKind >= 0 ? lastOfKind + 1 : ordered.FindIndex(existing => existing.Kind > member.Kind);
            ordered.Insert(position < 0 ? ordered.Count : position, member);
        }

        foreach (var member in ordered)
        {
            member.Print();
        }
    }

    sealed record PrintableMember(SyntaxNode Node, int Kind, Action Print);
}

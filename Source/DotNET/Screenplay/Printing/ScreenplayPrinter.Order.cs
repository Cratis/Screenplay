// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
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
    /// Prints members in their authored order when their physical or layout positions share a document, otherwise in canonical order.
    /// </summary>
    /// <remarks>
    /// Source locations suffice for parsed siblings in one document: their lines are unique, and no public
    /// syntax constructor or JSON shape needs to change. A folder merge can combine different paths, whose
    /// line numbers cannot be compared; in that case the existing kind order is the deterministic fallback.
    /// Layout expansion can supply a temporary parent-relative import position without changing physical
    /// source locations or comment anchors; those positions participate in the same document-local order.
    /// Start/default locations mark newly authored nodes. Features under modules or features, slices and file imports
    /// are inserted before their next located sibling in that collection, enabling mid-list timeline moves and pins.
    /// If there is no next located sibling, use the existing insertion rule: after the last member of the kind,
    /// or before the first member of a later canonical kind if none exists. Every other collection keeps that
    /// existing rule exactly, including structurally identical occurrences with distinct comments.
    /// Retaining a declaration's location retains its authored position.
    /// </remarks>
    static void WriteMembers(List<PrintableMember> members)
    {
        var located = members.Where(member => member.Node.PrintingLocation is { Line: > 0, Column: > 0 } ||
            member.Node.Location is { Line: > 1, Column: > 0 } or { Line: 1, Column: > 0, Path: not null }).ToList();
        var sameDocument = located.Count > 0 && located.TrueForAll(member =>
            string.Equals(member.Position.Path, located[0].Position.Path, StringComparison.Ordinal));

        if (!sameDocument)
        {
            foreach (var member in members.OrderBy(member => member.Kind))
            {
                member.Print();
            }

            return;
        }

        var ordered = located.OrderBy(member => member.Position.Line)
            .ThenBy(member => member.Position.Column).ToList();
        foreach (var member in members.Except(located))
        {
            var nextSibling = member.Node is FeatureSyntax or SliceSyntax or FileImportSyntax
                ? members.Skip(members.IndexOf(member) + 1).FirstOrDefault(existing => existing.Kind == member.Kind && located.Contains(existing))
                : null;
            var lastOfKind = ordered.FindLastIndex(existing => existing.Kind == member.Kind);
            var position = lastOfKind >= 0 ? lastOfKind + 1 : ordered.FindIndex(existing => existing.Kind > member.Kind);
            if (nextSibling is not null)
            {
                position = ordered.IndexOf(nextSibling);
            }
            ordered.Insert(position < 0 ? ordered.Count : position, member);
        }

        foreach (var member in ordered)
        {
            member.Print();
        }
    }

    sealed record PrintableMember(SyntaxNode Node, int Kind, Action Print)
    {
        internal SourceLocation Position => Node.PrintingLocation ?? Node.Location;
    }
}

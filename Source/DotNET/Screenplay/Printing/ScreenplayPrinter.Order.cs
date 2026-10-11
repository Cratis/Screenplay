// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

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
    /// Start/default locations mark newly authored nodes. An unlocated member is inserted before its next located
    /// sibling in the same collection, so a typed add or move at an index keeps that index (timeline moves and pins,
    /// and moves of templates, forms and contributions alike). If there is no next located sibling, it goes after the
    /// last member of the kind, or before the first member of a later canonical kind if none exists. A typed move drops
    /// the moved node's location when it disagrees with the requested index, so it is placed by this rule.
    /// Whether a member is located is decided by <see cref="AuthoredPositions"/>, the definition AST edits, comment
    /// ownership and layout expansion share.
    /// Retaining a declaration's location retains its authored position.
    /// </remarks>
    static void WriteMembers(List<PrintableMember> members, SyntaxNode owner)
    {
        var resolved = AuthoredPositions.Resolve(owner.Location, [.. members.Select(member => (member.Node.Location, member.Node.PrintingLocation))]);
        var positions = members.Zip(resolved).Where(pair => pair.Second is not null)
            .ToDictionary(pair => pair.First, pair => pair.Second!, (IEqualityComparer<PrintableMember>)ReferenceEqualityComparer.Instance);
        var located = members.Where(positions.ContainsKey).ToList();
        var sameDocument = AuthoredPositions.ShareDocument([.. positions.Values]);

        if (!sameDocument)
        {
            foreach (var member in members.OrderBy(member => member.Kind))
            {
                member.Print();
            }

            return;
        }

        var ordered = located.OrderBy(member => positions[member].Line)
            .ThenBy(member => positions[member].Column).ToList();
        foreach (var member in members.Except(located))
        {
            var nextSibling = IsDistinguishable(member, located)
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

    // Timeline members always keep their index. Any other member does unless a located sibling of its kind is
    // structurally identical: then the index cannot tell the two apart and the existing kind rule decides.
    static bool IsDistinguishable(PrintableMember member, List<PrintableMember> located)
    {
        if (member.Node is FeatureSyntax or SliceSyntax or FileImportSyntax)
        {
            return true;
        }

        var shape = SyntaxJson.Serialize(member.Node).GetRawText();
        return !located.Exists(existing => existing.Kind == member.Kind && string.Equals(SyntaxJson.Serialize(existing.Node).GetRawText(), shape, StringComparison.Ordinal));
    }

    sealed record PrintableMember(SyntaxNode Node, int Kind, Action Print);
}

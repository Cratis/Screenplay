// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Indexing;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Captures;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Screenplay.Syntax.Serialization;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Workspaces;

internal sealed partial class WorkspacePropertyLinks
{
    static readonly HashSet<Type> _namedMemberDeclarations =
    [
        typeof(PropertySyntax), // Declares a field in its containing shape.
        typeof(QueryParameterSyntax), // Declares an input parameter of a query.
        typeof(SpecificationParameterSyntax), // Declares a parameter supplied by specification cases.
        typeof(BehaviorParameterSyntax) // Declares a parameter supplied by behavior attachments.
    ];

    static readonly Dictionary<Type, SyntaxMember[]> _textMembers = SyntaxKinds.All.ToDictionary(
        descriptor => descriptor.Type,
        descriptor => descriptor.Members.Where(member => member.Type == typeof(string) || member.ElementType == typeof(string)).ToArray());

    static IEnumerable<string> TextValues(object? value) => value switch
    {
        string text => [text],
        IEnumerable<string> texts => texts,
        _ => []
    };

    static bool NamesProperty(IEnumerable<string> texts, string name) => texts.SelectMany(WorkspaceOpaqueText.Identifiers).Contains(name, StringComparer.OrdinalIgnoreCase);

    IEnumerable<string> AuthoredTexts(WorkspaceSyntaxEntry entry) => _textMembers[entry.Node.GetType()]
        .Where(member => !DeclaresName(entry, member))
        .SelectMany(member => TextValues(member.Property.GetValue(entry.Node)));

    bool DeclaresName(WorkspaceSyntaxEntry entry, SyntaxMember member)
    {
        // A reads alias introduces the local name used by expressions in the command.
        if (member.Property.Name == nameof(ReadsSyntax.Alias)) return entry.Node is ReadsSyntax;
        if (member.Property.Name != nameof(PropertySyntax.Name)) return false;

        // Trigger data declares occurrence parameters only on a trigger, not on a consuming reaction.
        if (entry.Node is TriggerDataSyntax && Owner(entry)?.Syntax is TriggerSyntax) return true;

        return _namedMemberDeclarations.Contains(entry.Node.GetType());
    }

    void IndexCompleteness()
    {
        foreach (var group in _entries.Where(entry => Owner(entry) is not null).GroupBy(entry => Owner(entry)!))
        {
            var properties = ReaderContext(group.Key).SelectMany(owner => _properties.GetValueOrDefault(owner) ?? []).Distinct().ToArray();
            foreach (var entry in group)
            {
                IndexUnclassified(entry, properties, AuthoredTexts(entry));
                foreach (var comment in entry.Node.SourceComments)
                {
                    var occurrence = entry with { Node = entry.Node with { Location = entry.Location with { Line = comment.Line } } };
                    IndexUnclassified(occurrence, properties, [comment.Text], classifiedOccurrence: false);
                }
            }
        }
    }

    void IndexUnclassified(WorkspaceSyntaxEntry entry, AuthoredDeclaration[] properties, IEnumerable<string> texts, bool classifiedOccurrence = true)
    {
        var identifiers = texts.SelectMany(WorkspaceOpaqueText.Identifiers).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var property in properties.Where(property => identifiers.Contains(property.Name) && !Covered(entry, property, classifiedOccurrence)))
        {
            Add(entry, "unclassified", new(property.Name, [property], Incomplete: true));
        }
    }

    AuthoredDeclaration[] ReaderContext(AuthoredDeclaration owner)
    {
        var context = new HashSet<AuthoredDeclaration> { owner };
        context.UnionWith(_index.Outgoing(owner.Owner).SelectMany(_index.Resolve));
        if (owner.Syntax is PolicySyntax) context.UnionWith(PolicyCallers(owner));
        if (owner.Syntax is CaptureSyntax { Source: { } source })
        {
            context.UnionWith(CaptureEventsSource.Events(source).SelectMany(setting => ResolveAtOwner(setting.Value, ["Event"], owner, source.Location)));
        }
        foreach (var indirect in context.Where(declaration => declaration.Syntax is QuerySyntax or SpecificationExampleSyntax).ToArray())
        {
            context.UnionWith(_index.Outgoing(indirect.Owner).SelectMany(_index.Resolve));
        }

        return Current(context);
    }

    bool Covered(WorkspaceSyntaxEntry entry, AuthoredDeclaration property, bool classifiedOccurrence)
    {
        if (new[] { entry }.Concat(Parents(entry)).Any(item => item.Node is PropertySyntax && item.Location == property.Location)) return true;

        // A typed mapping/reader occurrence with one resolved same-named member is not evidence
        // of every other same-named member in a fan-out context. Use the existing classified link,
        // not a name-based inferred mapping; unknown text and comments still reach the backstop.
        if (classifiedOccurrence && (entry.Node is PropertyMappingSyntax or MappingSyntax or PathExpressionSyntax or ContextExpressionSyntax or SourceItemExpressionSyntax) &&
            _links.Exists(link => link.Reference.Location == entry.Location && link.Resolution == "resolved" &&
                link.Reference.Role != "unclassified" && link.Candidates is [var candidate] && candidate.Name == property.Name && link.Reference.Name.Split('.')[^1] == property.Name))
        {
            return true;
        }
        var locations = new[] { entry }.Concat(Parents(entry).TakeWhile(parent => DeclarationOwner(parent) is null)).Select(item => item.Location).ToHashSet();

        return Incoming(property).Any(link => locations.Contains(link.Reference.Location));
    }
}

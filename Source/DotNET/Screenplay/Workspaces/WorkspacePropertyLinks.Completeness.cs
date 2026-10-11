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
        typeof(AuthenticationProviderSyntax), typeof(BehaviorSyntax), typeof(BehaviorParameterSyntax), typeof(BehaviorArgumentSyntax),
        typeof(CaptureSourceSettingSyntax), typeof(ComponentExposedValueSyntax), typeof(ComponentOutletSyntax), typeof(DomainSyntax), typeof(EventStreamIdPartSyntax),
        typeof(InteractionArgumentSyntax), typeof(ObjectMemberSyntax), typeof(PresentationValueSyntax), typeof(ProjectionVariantSyntax), typeof(PropertySyntax),
        typeof(QueryParameterSyntax), typeof(ResponseFieldSyntax), typeof(ScreenComponentSyntax), typeof(ScreenNavigationParameterSyntax),
        typeof(ScreenSectionSyntax), typeof(ScreenToolbarSyntax), typeof(SlotSyntax), typeof(TemplateExposedValueSyntax),
        typeof(TemplateOutletSyntax), typeof(ToolbarItemSyntax)
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
        if (member.Property.Name == nameof(ReadsSyntax.Alias)) return entry.Node is ReadsSyntax or AuthenticationProviderSyntax;
        if (member.Property.Name != nameof(PropertySyntax.Name)) return false;
        if (entry.Node is ConstraintSyntax) return true;
        if (entry.Node is TriggerDataSyntax && Owner(entry)?.Syntax is TriggerSyntax) return true;

        return DeclarationOwner(entry) is not null || _namedMemberDeclarations.Contains(entry.Node.GetType());
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
                    IndexUnclassified(occurrence, properties, [comment.Text]);
                }
            }
        }
    }

    void IndexUnclassified(WorkspaceSyntaxEntry entry, AuthoredDeclaration[] properties, IEnumerable<string> texts)
    {
        var identifiers = texts.SelectMany(WorkspaceOpaqueText.Identifiers).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var property in properties.Where(property => identifiers.Contains(property.Name) && !Covered(entry, property)))
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

    bool Covered(WorkspaceSyntaxEntry entry, AuthoredDeclaration property)
    {
        if (new[] { entry }.Concat(Parents(entry)).Any(item => item.Node is PropertySyntax && item.Location == property.Location)) return true;
        var locations = new[] { entry }.Concat(Parents(entry).TakeWhile(parent => DeclarationOwner(parent) is null)).Select(item => item.Location).ToHashSet();

        return Incoming(property).Any(link => locations.Contains(link.Reference.Location));
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Indexing;

internal static partial class ReferenceKinds
{
    internal const string Coverage = "Explicit declaration references in source identifier/stream-id types, authored command and specification event source/stream routes, types, identity query sources, policies, processing purposes, commands, queries, screen data, forms, contribution points, outlet changes, hook slices, picker queries, screens a slot shows, specifications, projections, reducers, reactions, captures, constraints, seeds and concurrency event lists. Property queries cover command, current-generation event and read-model mapping paths, AutoMap, conditions, screen/form bindings and specification/example assignments and assertions, and seed event values. Opaque code naming a property is reported as opaque, not resolved. Not code, imports, slot names and outlet item ids (hide, before, after), which are local to the template that declares them, profile settings, external host registrations, arbitrary expression identifiers or external code contents; nested composite paths are incomplete evidence. Read models include projection output aliases and variants, not the projection builder's name when it produces a different view.";

    // Lazy initialization avoids depending on the order of fields across partial files. Keep the
    // original entries (rather than a set) so the inventory spec can also detect duplicate decisions.
    static readonly Lazy<Member[]> _members = new(CollectMembers);
    static readonly Lazy<ILookup<Type, Member>> _byType = new(() => _members.Value.ToLookup(member => member.Node));

    internal static IEnumerable<(Type Node, string Property)> Classified => _members.Value.Select(member => (member.Node, member.Property));

    internal static IEnumerable<(string Name, string[] Kinds, string Role)> For(SyntaxNode node, SyntaxNode? owner = null) =>
        InlineDeclarations(node).Concat(_byType.Value[node.GetType()].Where(member => member.Kinds.Length > 0).SelectMany(member => member.Read(node, owner)));

    internal static bool Classifies(SyntaxNode node, string property) => _byType.Value[node.GetType()].Any(member => member.Property == property);

    internal static bool IsReference(SyntaxNode node, string property) => _byType.Value[node.GetType()].Any(member => member.Property == property && member.Kinds.Length > 0);

    internal static bool Covers(SyntaxNode node, SyntaxNode? owner) => node.GetType().GetProperties()
        .Where(property => IsStringMember(property.PropertyType)).All(property => Classifies(node, property.Name));

    internal static bool IsStringMember(Type type) => type == typeof(string) || typeof(IEnumerable<string>).IsAssignableFrom(type);

    // Inline events have no string-bearing slice member of their own; keep their declaration edges.
    static IEnumerable<(string Name, string[] Kinds, string Role)> InlineDeclarations(SyntaxNode node)
    {
        if (node is not SliceSyntax slice) yield break;
        foreach (var @event in EventDeclarations.In(slice).Except(slice.Events)) yield return (@event.Name, ["Event"], "declares");
    }

    static Member[] CollectMembers() => [.. _nonReferences, .. _references];

    static IEnumerable<Member> NonReferences<T>(params string[] properties)
        where T : SyntaxNode => properties.Select(property => new Member(typeof(T), property, [], string.Empty, (_, _) => []));

    static Member Reference<T>(string property, string[] kinds, string role, Func<T, SyntaxNode?, IEnumerable<(string Name, string[] Kinds, string Role)>>? read = null)
        where T : SyntaxNode =>
        new(typeof(T), property, kinds, role, (node, owner) => read is not null ? read((T)node, owner) : Names(typeof(T).GetProperty(property)!.GetValue(node)).Select(name => (name, kinds, role)));

    static IEnumerable<string> Names(object? value) => value switch
    {
        string name => [name],
        IEnumerable<string> names => names,
        _ => []
    };

    static IEnumerable<(string Name, string[] Kinds, string Role)> One(string? name, string[] kinds, string role) => name is null ? [] : [(name, kinds, role)];

    sealed record Member(Type Node, string Property, string[] Kinds, string Role, Func<SyntaxNode, SyntaxNode?, IEnumerable<(string Name, string[] Kinds, string Role)>> Read);
}

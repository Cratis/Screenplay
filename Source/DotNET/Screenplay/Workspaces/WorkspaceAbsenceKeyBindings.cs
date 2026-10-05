// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Workspaces;

/// <summary>
/// One obligation of a keyed absence assertion: the key itself, bound to the read model identifier property,
/// or one authored object member of a composite key, bound to a composite-type property.
/// </summary>
/// <param name="Assertion">The absence assertion occurrence.</param>
/// <param name="Occurrence">The assertion itself for the key obligation, otherwise the object member occurrence.</param>
/// <param name="Target">The bound property declaration, or null for an explicit unresolved obligation.</param>
/// <param name="Reason">Why the obligation is unresolved; "resolved" when it has a target.</param>
/// <param name="Dependencies">Every occurrence the resolution depended on, from the assertion to the occurrence itself.</param>
sealed record WorkspaceAbsenceKeyObligation(
    WorkspaceSyntaxEntry Assertion,
    WorkspaceSyntaxEntry Occurrence,
    WorkspaceSyntaxEntry? Target,
    string Reason,
    ImmutableArray<WorkspaceSyntaxEntry> Dependencies)
{
    internal bool IsKey => Occurrence.Node is SpecificationAbsentReadModelSyntax;

    internal string Text => Occurrence.Node is ObjectMemberSyntax member ? member.Name : ((SpecificationAbsentReadModelSyntax)Assertion.Node).Name;

    internal (DocumentId Document, string Path) Position => (Occurrence.Handle.Document, Occurrence.Handle.Path);
}

/// <summary>
/// Resolves keyed absence assertions independently of the generic reference correspondence engine. Each assertion
/// resolves its read model, the identifier named by that read model's keyed query and the identifier type; every
/// authored key member is then either bound to one declared property or recorded as an explicit unresolved obligation.
/// </summary>
sealed class WorkspaceAbsenceKeyBindings
{
    readonly WorkspaceSyntaxIndex _index;
    readonly ILookup<WorkspaceNodeHandle, WorkspaceSyntaxEntry> _children;
    readonly Dictionary<string, WorkspaceReferenceBinding> _references;

    internal WorkspaceAbsenceKeyBindings(WorkspaceSyntaxIndex index, WorkspaceReferenceBindings references)
    {
        _index = index;
        _children = index.Entries.Where(entry => entry.Parent is not null).ToLookup(entry => entry.Parent!);
        _references = references.Bindings.ToDictionary(binding => binding.Reference.Key, StringComparer.Ordinal);
        Obligations = [.. index.Entries.Where(entry => entry.Node is SpecificationAbsentReadModelSyntax).SelectMany(Bind)];
    }

    internal ImmutableArray<WorkspaceAbsenceKeyObligation> Obligations { get; }

    internal static bool Present(WorkspaceSyntaxIndex index) => index.Entries.Any(entry => entry.Node is SpecificationAbsentReadModelSyntax);

    static string ShortName(string name) => name.Split('.')[^1];

    static string ReferenceKey(WorkspaceSyntaxEntry entry, string member) => $"{entry.Handle.Document}:{entry.Handle.Path}/{member}";

    IEnumerable<WorkspaceAbsenceKeyObligation> Bind(WorkspaceSyntaxEntry assertion)
    {
        var dependencies = ImmutableArray.CreateBuilder<WorkspaceSyntaxEntry>();
        dependencies.Add(assertion);
        var identifier = Identifier(assertion, dependencies, out var identifierType, out var reason);
        var chain = dependencies.ToImmutable();
        yield return new(assertion, assertion, identifier, identifier is null ? reason : "resolved", chain);

        var keyPath = $"{assertion.Handle.Path}/key";
        foreach (var member in _index.Entries.Where(entry => entry.Node is ObjectMemberSyntax && entry.Handle.Document == assertion.Handle.Document &&
            entry.Handle.Path.StartsWith($"{keyPath}/", StringComparison.Ordinal)))
        {
            yield return Member(assertion, member, identifier is null ? null : identifierType, identifier is null ? $"its key is unresolved: {reason}" : reason, chain);
        }
    }

    WorkspaceSyntaxEntry? Identifier(WorkspaceSyntaxEntry assertion, ImmutableArray<WorkspaceSyntaxEntry>.Builder dependencies, out WorkspaceSyntaxEntry? type, out string reason)
    {
        type = null;
        var name = ((SpecificationAbsentReadModelSyntax)assertion.Node).Name;
        var view = _references.GetValueOrDefault(ReferenceKey(assertion, "name"));
        if (view?.Target?.Entry is not { Node: ReadModelSyntax readModel } readModelEntry)
        {
            reason = view?.Target is null
                ? $"read model '{name}' is {view?.Outcome ?? "unresolved"}"
                : $"'{name}' has no read model declaration";
            return null;
        }

        dependencies.Add(readModelEntry);
        var slice = readModelEntry.Parent is { } parent ? _index.Find(parent) : null;
        var keyed = slice is null
            ? []
            : Children(slice, "queries").Where(query => query.Node is QuerySyntax { By: not null } declared && ShortName(declared.ReturnType.Name) == readModel.Name).ToArray();
        foreach (var query in keyed)
        {
            dependencies.AddRange(Children(query, "returnType"));
            dependencies.AddRange(Children(query, "by"));
        }

        var names = keyed.Select(query => ((QuerySyntax)query.Node).By!.Name).Distinct(StringComparer.Ordinal).ToArray();
        if (names.Length != 1)
        {
            reason = names.Length == 0
                ? $"read model '{readModel.Name}' has no keyed query that names its identifier; give its slice a query that returns '{readModel.Name}' with 'by <property> <Type>' naming the property that identifies an instance"
                : $"read model '{readModel.Name}' has ambiguous keyed-query identifiers; keep the keyed queries that return '{readModel.Name}' on one 'by' property";
            return null;
        }

        // Every competing declaration is a dependency, so removing a duplicate is an explicit repair.
        var properties = Children(readModelEntry, "properties").Where(property => ((PropertySyntax)property.Node).Name == names[0]).ToArray();
        dependencies.AddRange(properties);
        if (properties.Length != 1)
        {
            reason = $"identifier '{names[0]}' is not declared exactly once on read model '{readModel.Name}'";
            return null;
        }

        type = TypeOf(properties[0], dependencies, out reason);
        return properties[0];
    }

    WorkspaceAbsenceKeyObligation Member(
        WorkspaceSyntaxEntry assertion,
        WorkspaceSyntaxEntry member,
        WorkspaceSyntaxEntry? identifierType,
        string reason,
        ImmutableArray<WorkspaceSyntaxEntry> chain)
    {
        var dependencies = chain.ToBuilder();
        var enclosing = new List<WorkspaceSyntaxEntry>();
        for (var current = member.Parent is { } parent ? _index.Find(parent) : null; current is not null && current.Handle != assertion.Handle; current = current.Parent is { } ancestor ? _index.Find(ancestor) : null)
        {
            if (current.Node is ObjectMemberSyntax)
            {
                enclosing.Add(current);
            }
        }

        var type = identifierType;
        foreach (var outer in Enumerable.Reverse(enclosing))
        {
            dependencies.Add(outer);
            if (type is null)
            {
                continue;
            }

            var property = Property(type, ((ObjectMemberSyntax)outer.Node).Name, dependencies, out reason);
            if (property is null)
            {
                reason = $"its enclosing member is unresolved: {reason}";
                type = null;
                continue;
            }

            type = TypeOf(property, dependencies, out reason);
        }

        dependencies.Add(member);
        var target = type is null ? null : Property(type, ((ObjectMemberSyntax)member.Node).Name, dependencies, out reason);
        return new(assertion, member, target, target is null ? reason : "resolved", dependencies.ToImmutable());
    }

    WorkspaceSyntaxEntry? TypeOf(WorkspaceSyntaxEntry property, ImmutableArray<WorkspaceSyntaxEntry>.Builder dependencies, out string reason)
    {
        var reference = Children(property, "type").SingleOrDefault();
        if (reference is null)
        {
            reason = $"property '{((PropertySyntax)property.Node).Name}' has no type";
            return null;
        }

        dependencies.Add(reference);
        var binding = _references.GetValueOrDefault(ReferenceKey(reference, "name"));
        if (binding?.Target?.Entry is { Node: TypeSyntax } type)
        {
            dependencies.Add(type);
            reason = "resolved";
            return type;
        }

        var name = ((TypeRefSyntax)reference.Node).Name;
        reason = binding?.Target is null
            ? $"type '{name}' is {binding?.Outcome ?? "unresolved"}"
            : $"type '{name}' is not a composite type and has no members";
        return null;
    }

    WorkspaceSyntaxEntry? Property(WorkspaceSyntaxEntry type, string name, ImmutableArray<WorkspaceSyntaxEntry>.Builder dependencies, out string reason)
    {
        // Every competing declaration is a dependency, so removing a duplicate is an explicit repair.
        var matches = Children(type, "properties").Where(property => ((PropertySyntax)property.Node).Name == name).ToArray();
        dependencies.AddRange(matches);
        reason = matches.Length switch
        {
            1 => "resolved",
            0 => $"type '{((TypeSyntax)type.Node).Name}' declares no property '{name}'",
            _ => $"type '{((TypeSyntax)type.Node).Name}' declares property '{name}' more than once"
        };
        return matches.Length == 1 ? matches[0] : null;
    }

    IEnumerable<WorkspaceSyntaxEntry> Children(WorkspaceSyntaxEntry entry, string member) =>
        _children[entry.Handle].Where(child => child.Member == member);
}

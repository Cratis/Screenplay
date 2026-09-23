// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Captures;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Workspaces;

/// <summary>
/// Resolves a structured-value key to the declared composite type that owns it.
/// </summary>
static class WorkspaceStructuredReferences
{
    internal static string? Owner(WorkspaceSyntaxEntry entry, WorkspaceSyntaxIndex index)
    {
        var ancestors = new List<WorkspaceSyntaxEntry>();
        for (var current = entry.Parent is { } parent ? index.Find(parent) : null; current is not null; current = current.Parent is { } ancestor ? index.Find(ancestor) : null)
        {
            ancestors.Add(current);
        }

        var mappingIndex = ancestors.FindIndex(ancestor => ancestor.Node is PropertyMappingSyntax);
        if (mappingIndex < 0)
        {
            return null;
        }

        var mapping = (PropertyMappingSyntax)ancestors[mappingIndex].Node;
        var step = ancestors.Skip(mappingIndex + 1).FirstOrDefault(ancestor => ancestor.Node is SpecificationCommandSyntax or SpecificationEventSyntax or SpecificationReadModelSyntax or SpecificationQuerySyntax or SpecificationQueryResultSyntax or ProducesSyntax or CaptureAppendSyntax);
        if (step is null)
        {
            return null;
        }

        var declarations = index.Entries.Select(item => item.Node).ToArray();
        var properties = step.Node switch
        {
            ProducesSyntax produced => Unique(declarations.OfType<EventSyntax>(), produced.Event)?.Properties,
            CaptureAppendSyntax appended => Unique(declarations.OfType<EventSyntax>(), appended.Event)?.Properties,
            SpecificationCommandSyntax command => Unique(declarations.OfType<CommandSyntax>(), command.CommandType)?.Properties,
            SpecificationEventSyntax @event => Unique(declarations.OfType<EventSyntax>(), @event.EventType)?.Properties,
            SpecificationReadModelSyntax view => Unique(declarations.OfType<ReadModelSyntax>(), view.Name)?.Properties,
            SpecificationQueryResultSyntax => QueryView(step, ancestors, declarations)?.Properties,
            SpecificationQuerySyntax query => Unique(declarations.OfType<QuerySyntax>(), query.Query) is { } declared
                ? (declared.By is null ? Enumerable.Empty<PropertySyntax>() : [new PropertySyntax(declared.By.Name, declared.By.Type, declared.By.Location)])
                    .Concat(declared.Filters.Select(filter => new PropertySyntax(filter.Name, filter.Type, filter.Location)))
                : null,
            _ => null
        };
        var type = properties?.SingleOrDefault(property => property.Name == mapping.Property)?.Type.Name;

        // From the mapping outwards, each enclosing object member selects the type of its value.
        foreach (var member in ancestors.Take(mappingIndex).Reverse().Select(ancestor => ancestor.Node).OfType<ObjectMemberSyntax>())
        {
            type = Properties(declarations, type)?.SingleOrDefault(property => property.Name == member.Name)?.Type.Name;
        }

        return Properties(declarations, type) is not null ? type : null;
    }

    static ReadModelSyntax? QueryView(WorkspaceSyntaxEntry result, List<WorkspaceSyntaxEntry> ancestors, SyntaxNode[] declarations)
    {
        var query = ancestors.SkipWhile(entry => entry != result).Skip(1).Select(entry => entry.Node).OfType<SpecificationQuerySyntax>().FirstOrDefault();
        var target = query is null ? null : Unique(declarations.OfType<QuerySyntax>(), query.Query);
        return target is null ? null : Unique(declarations.OfType<ReadModelSyntax>(), target.ReturnType.Name);
    }

    static IEnumerable<PropertySyntax>? Properties(SyntaxNode[] declarations, string? type) => type is null
        ? null
        : Unique(declarations.OfType<TypeSyntax>(), type)?.Properties;

    static T? Unique<T>(IEnumerable<T> nodes, string name)
        where T : SyntaxNode
    {
        var matches = nodes.Where(node => WorkspaceReferenceBindings.Name(node) == name).Take(2).ToArray();
        return matches.Length == 1 ? matches[0] : null;
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Parsing;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Workspaces;

static class WorkspaceFixtureDeclarations
{
    internal static IEnumerable<PropertySyntax>? Properties(WorkspaceSyntaxEntry step, WorkspaceSyntaxIndex index)
    {
        var name = step.Node switch
        {
            SpecificationExampleSyntax example => example.Type,
            SpecificationCommandSyntax command => command.CommandType,
            SpecificationEventSyntax @event => @event.EventType,
            SpecificationReadModelSyntax model => model.Name,
            _ => null
        };
        var target = name is null ? null : Resolve(name, step, index);
        if (target?.Node is SpecificationExampleSyntax exampleTarget)
        {
            target = Resolve(exampleTarget.Type, target, index);
        }

        return target?.Node switch
        {
            CommandSyntax command => command.Properties,
            EventSyntax @event => @event.Properties,
            ReadModelSyntax model => model.Properties,
            _ => null
        };
    }

    static WorkspaceSyntaxEntry? Resolve(string name, WorkspaceSyntaxEntry use, WorkspaceSyntaxIndex index)
    {
        var entries = index.Entries.Where(entry => entry.Node is SpecificationExampleSyntax or CommandSyntax or EventSyntax or ReadModelSyntax)
            .Where(entry => entry.Node is not EventSyntax historical || !index.Entries.Any(other => other.Node is EventSyntax current &&
                current.Name == historical.Name && current.Generation > historical.Generation &&
                WorkspaceReferenceBindings.Scope(other, index).Segments.SequenceEqual(WorkspaceReferenceBindings.Scope(entry, index).Segments)))
            .Select(entry => (Entry: entry, Declaration: new Declaration(WorkspaceReferenceBindings.Name(entry.Node)!, WorkspaceReferenceBindings.Scope(entry, index))))
            .ToArray();
        var resolution = ReferenceResolver.Resolve(name, WorkspaceReferenceBindings.Scope(use, index), [.. entries.Select(entry => entry.Declaration)]);
        if (resolution.IsUnresolved && index.Entries.Select(entry => entry.Node).OfType<ImportSyntax>().Where(import => import.Name == name).ToArray() is [var imported])
        {
            resolution = ReferenceResolver.Resolve(imported.QualifiedName, WorkspaceReferenceBindings.Scope(use, index), [.. entries.Select(entry => entry.Declaration)]);
        }
        var matches = entries.Where(entry => entry.Declaration == resolution.Resolved).Take(2).ToArray();

        return matches is [var match] ? match.Entry : null;
    }
}

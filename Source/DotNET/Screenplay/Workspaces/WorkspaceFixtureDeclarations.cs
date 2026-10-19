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

    internal static WorkspaceSyntaxEntry? Resolve(string name, WorkspaceSyntaxEntry use, WorkspaceSyntaxIndex index)
    {
        var candidates = WorkspaceFixtureCandidates.For(index);
        var scope = candidates.ScopeOf(use);
        var resolution = ReferenceResolver.Resolve(name, scope, candidates.Declarations);
        if (resolution.IsUnresolved && candidates.ImportsNamed(name) is [var imported])
        {
            resolution = ReferenceResolver.Resolve(imported.QualifiedName, scope, candidates.Declarations);
        }
        var matches = candidates.Entries.Where(entry => entry.Declaration == resolution.Resolved).Take(2).ToArray();

        return matches is [var match] ? match.Entry : null;
    }
}

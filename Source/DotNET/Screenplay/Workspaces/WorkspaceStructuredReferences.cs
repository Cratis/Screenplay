// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Captures;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Workspaces;

/// <summary>
/// Resolves structured keys to their declared property owner, including payloads and composite values.
/// </summary>
static class WorkspaceStructuredReferences
{
    internal static string? Owner(WorkspaceSyntaxEntry entry, WorkspaceSyntaxIndex index)
    {
        var ancestors = WorkspaceReferenceMembers.Parents(entry, index).ToList();
        if (entry.Node is PropertyMappingSyntax && ancestors.FirstOrDefault() is { } payload) return PayloadOwner(payload, index);
        var mappingIndex = ancestors.FindIndex(ancestor => ancestor.Node is PropertyMappingSyntax);
        if (mappingIndex < 0) return null;
        var enclosing = ancestors.Skip(mappingIndex + 1).ToArray();
        if (enclosing.Any(ancestor => ancestor.Node is SpecificationStreamSyntax)) return null;
        var step = enclosing.FirstOrDefault(ancestor => IsValueStep(ancestor.Node));
        if (step is null) return null;

        return ValueOwner(ancestors, mappingIndex, step, index);
    }

    static string? PayloadName(SyntaxNode node) => node switch
    {
        ProducesSyntax produced => produced.Event,
        CaptureAppendSyntax appended => appended.Event,
        SpecificationExampleSyntax example => example.Type,
        SpecificationCommandSyntax command => command.CommandType,
        SpecificationEventSyntax @event => @event.EventType,
        SpecificationReadModelSyntax model => model.Name,
        _ => null
    };

    static string? PayloadOwner(WorkspaceSyntaxEntry payload, WorkspaceSyntaxIndex index)
    {
        var name = PayloadName(payload.Node);
        var target = name is null ? null : WorkspaceFixtureDeclarations.Resolve(name, payload, index);
        if (target?.Node is SpecificationExampleSyntax exampleTarget) target = WorkspaceFixtureDeclarations.Resolve(exampleTarget.Type, target, index);

        return target is null ? null : WorkspaceReferenceBindings.Key(target);
    }

    static bool IsValueStep(SyntaxNode node) => IsFixtureStep(node) || node is SpecificationOperationSyntax or SpecificationQuerySyntax or SpecificationQueryResultSyntax or ProducesSyntax or CaptureAppendSyntax;

    static bool IsFixtureStep(SyntaxNode node) => node is SpecificationExampleSyntax or SpecificationCommandSyntax or SpecificationEventSyntax or SpecificationReadModelSyntax;

    static string? ValueOwner(List<WorkspaceSyntaxEntry> ancestors, int mappingIndex, WorkspaceSyntaxEntry step, WorkspaceSyntaxIndex index)
    {
        var mapping = (PropertyMappingSyntax)ancestors[mappingIndex].Node;
        var declarations = WorkspaceUniqueDeclarations.For(index);
        var properties = StepProperties(step, index, ancestors, declarations);
        var type = properties?.SingleOrDefault(property => property.Name == mapping.Property)?.Type.Name;

        // From the mapping outwards, each enclosing object member selects the type of its value.
        foreach (var member in ancestors.Take(mappingIndex).Reverse().Select(ancestor => ancestor.Node).OfType<ObjectMemberSyntax>())
        {
            type = Properties(declarations, type)?.SingleOrDefault(property => property.Name == member.Name)?.Type.Name;
        }

        return Properties(declarations, type) is not null ? type : null;
    }

    static IEnumerable<PropertySyntax>? StepProperties(WorkspaceSyntaxEntry step, WorkspaceSyntaxIndex index, List<WorkspaceSyntaxEntry> ancestors, WorkspaceUniqueDeclarations declarations)
    {
        if (IsFixtureStep(step.Node)) return WorkspaceFixtureDeclarations.Properties(step, index);

        return step.Node switch
        {
            ProducesSyntax produced => ProductionProperties(produced, step, index, declarations),
            SpecificationOperationSyntax operation => OperationInputs(operation.Operation, step, index),
            CaptureAppendSyntax appended => EventProperties(declarations, appended.Event),
            SpecificationQueryResultSyntax => QueryView(step, ancestors, declarations)?.Properties,
            SpecificationQuerySyntax query => QueryProperties(query.Query, declarations),
            _ => null
        };
    }

    static IEnumerable<PropertySyntax>? ProductionProperties(ProducesSyntax produced, WorkspaceSyntaxEntry step, WorkspaceSyntaxIndex index, WorkspaceUniqueDeclarations declarations)
    {
        if (index.OwningSlice(step) is { } slice && !index.Productions.IsEventProduction(produced, slice))
        {
            return (index.Productions.Resolve(produced.Event, slice).Declaration?.Node as OperationSyntax)?.Inputs;
        }

        return EventProperties(declarations, produced.Event);
    }

    static IEnumerable<PropertySyntax>? OperationInputs(string operation, WorkspaceSyntaxEntry step, WorkspaceSyntaxIndex index) => index.OwningSlice(step) is { } slice
        ? (index.Productions.Resolve(operation, slice).Declaration?.Node as OperationSyntax)?.Inputs : null;

    static IEnumerable<PropertySyntax>? QueryProperties(string query, WorkspaceUniqueDeclarations declarations)
    {
        var declared = declarations.Named<QuerySyntax>(query);
        if (declared is null) return null;
        var by = declared.By is null ? Enumerable.Empty<PropertySyntax>() : [new PropertySyntax(declared.By.Name, declared.By.Type, declared.By.Location)];

        return by.Concat(declared.Filters.Select(filter => new PropertySyntax(filter.Name, filter.Type, filter.Location)));
    }

    static ReadModelSyntax? QueryView(WorkspaceSyntaxEntry result, List<WorkspaceSyntaxEntry> ancestors, WorkspaceUniqueDeclarations declarations)
    {
        var query = ancestors.SkipWhile(entry => entry != result).Skip(1).Select(entry => entry.Node).OfType<SpecificationQuerySyntax>().FirstOrDefault();
        var target = query is null ? null : declarations.Named<QuerySyntax>(query.Query);

        return target is null ? null : declarations.Named<ReadModelSyntax>(target.ReturnType.Name);
    }

    static IEnumerable<PropertySyntax>? Properties(WorkspaceUniqueDeclarations declarations, string? type) => declarations.Named<TypeSyntax>(type)?.Properties;

    static IEnumerable<PropertySyntax>? EventProperties(WorkspaceUniqueDeclarations declarations, string @event) => declarations.Named<EventSyntax>(@event)?.Properties;
}

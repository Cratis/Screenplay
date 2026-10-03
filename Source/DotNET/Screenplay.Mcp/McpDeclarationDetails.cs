// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Mcp;

static class McpDeclarationDetails
{
    internal static object Read(McpSnapshot snapshot, JsonElement arguments)
    {
        var declaration = Target(snapshot, arguments);
        var view = McpJson.OptionalString(arguments, "view") ?? "summary";
        var details = view switch
        {
            "summary" => new
            {
                propertyCount = Properties(declaration.Syntax).Count(),
                syntaxOnly = SyntaxOnly(declaration.Syntax),
                executionReadiness = SyntaxOnly(declaration.Syntax) ? "Unavailable until ESM v8 (PLAY0268); use Authoring validation." : null,
                eventCount = declaration.Syntax is SliceSyntax eventOwner ? EventDeclarations.In(eventOwner).Count() : 0,
                eventId = (declaration.Syntax as EventSyntax)?.Id,
                documentation = (declaration.Syntax as EventSyntax)?.Documentation,
                partCount = declaration.Parts.Count,
                commandCount = declaration.Syntax is SliceSyntax slice ? slice.Commands.Count() : 0,
                specificationCount = declaration.Syntax is SliceSyntax described ? described.Specifications.Count() : 0,
                availableViews = Views(declaration.Syntax)
            },
            "properties" when declaration.Syntax is CommandSyntax or EventSyntax or ReadModelSyntax or TypeSyntax => McpPaging.Page(
                Properties(declaration.Syntax),
                property => new
                {
                    property.Name,
                    type = property.Type.Name,
                    property.Type.IsCollection,
                    property.Type.IsOptional,
                    property.IsIdentifier,
                    property.IsGenerated,
                    property.Location
                },
                arguments,
                snapshot.SourceRevision),
            "occurrences" => McpPaging.Page(declaration.Parts, part => new { kind = part.GetType().Name, part.Location }, arguments, snapshot.SourceRevision),
            "commands" when declaration.Syntax is SliceSyntax owner => McpPaging.Page(
                owner.Commands,
                command => new
                {
                    command.Name,
                    command.Description,
                    command.Location,
                    propertyCount = command.Properties.Count(),
                    generatedProperties = command.Properties.Where(property => property.IsGenerated).Select(property => property.Name),
                    response = Response(command),
                    syntaxOnly = SyntaxOnly(command),
                    producedEvents = command.Produces.Select(produces => produces.Event).Distinct(StringComparer.Ordinal).ToArray()
                },
                arguments,
                snapshot.SourceRevision),
            "specifications" when declaration.Syntax is SliceSyntax owner => McpPaging.Page(
                owner.Specifications,
                specification => new
                {
                    specification.Name,
                    specification.Location,
                    command = specification.When?.CommandType,
                    whenAppendedEvent = specification.WhenAppended?.EventType,
                    thenDenied = specification.ThenDenied is not null,
                    generatedValues = specification.When?.GeneratedValues,
                    thenReturns = specification.ThenReturns,
                    syntaxOnly = SyntaxOnly(specification),
                    givenEvents = specification.Given.Count(),
                    thenEvents = specification.ThenEvents.Count(),
                    thenErrors = specification.ThenErrors.Count(),
                    thenReadModels = specification.ThenReadModels?.Count() ?? 0,
                    thenAbsentReadModels = specification.ThenAbsentReadModels.Count(),
                    thenQueries = specification.ThenQueries.Count()
                },
                arguments,
                snapshot.SourceRevision),
            "response" when declaration.Syntax is CommandSyntax responseOwner => Response(responseOwner),
            "produces" when declaration.Syntax is CommandSyntax command => McpPaging.Page(command.Produces, arguments, snapshot.SourceRevision),
            "values" when declaration.Syntax is ConceptSyntax concept => McpPaging.Page(concept.Values, arguments, snapshot.SourceRevision),
            "syntax" => declaration.Syntax,
            _ => throw new McpFailure($"View '{view}' is not available for {declaration.Kind}.", -32602)
        };
        return new
        {
            snapshot.Compilation.Success,
            snapshot.SourceRevision,
            declaration = McpReadResults.Summary(declaration),
            diagnostics = McpModelQueries.DiagnosticSummary(snapshot),
            view,
            details
        };
    }

    internal static McpDeclaration Target(McpSnapshot snapshot, JsonElement arguments)
    {
        var address = McpJson.RequiredString(arguments, "address");
        var kind = McpJson.RequiredString(arguments, "kind");
        var matches = snapshot.Index.Find(address, kind);
        return matches.Length == 1 ? matches[0] : throw new McpFailure($"Declaration target must identify exactly one logical declaration; found {matches.Length}.");
    }

    static IEnumerable<string> Views(SyntaxNode node) => node switch
    {
        SliceSyntax => ["summary", "occurrences", "commands", "specifications", "syntax"],
        CommandSyntax => ["summary", "properties", "occurrences", "produces", "response", "syntax"],
        EventSyntax or ReadModelSyntax or TypeSyntax => ["summary", "properties", "occurrences", "syntax"],
        ConceptSyntax => ["summary", "values", "occurrences", "syntax"],
        _ => ["summary", "occurrences", "syntax"]
    };

    static bool SyntaxOnly(SyntaxNode node) => node switch
    {
        CommandSyntax command => command.Response is not null || command.Properties.Any(property => property.IsGenerated),
        Cratis.Screenplay.Syntax.Specifications.SpecificationSyntax specification => specification.ThenReturns is not null || (specification.When?.GeneratedValues.Any() ?? false),
        SliceSyntax slice => slice.Commands.Any(SyntaxOnly) || slice.Specifications.Any(SyntaxOnly),
        _ => false
    };

    static object Response(CommandSyntax command)
    {
        var properties = command.Properties.GroupBy(property => property.Name, StringComparer.Ordinal)
            .Where(group => group.Count() == 1).ToDictionary(group => group.Key, group => group.Single(), StringComparer.Ordinal);
        return new
        {
            syntaxOnly = SyntaxOnly(command),
            executionReadiness = SyntaxOnly(command) ? "Unavailable until ESM v8 (PLAY0268); no response type is emitted." : null,
            syntax = command.Response,
            fields = command.Response is RecordCommandResponseSyntax record ? record.Fields.Select(field => new
            {
                field.Name,
                declaredType = field.Type,
                inferredType = properties.GetValueOrDefault(field.Source.Property)?.Type,
                source = field.Source.Property,
                field.Location
            }) : null,
            scalarType = command.Response is ScalarCommandResponseSyntax scalar ? properties.GetValueOrDefault(scalar.Source.Property)?.Type : null
        };
    }

    static IEnumerable<PropertySyntax> Properties(SyntaxNode node) => node switch
    {
        CommandSyntax command => command.Properties,
        EventSyntax @event => @event.Properties,
        ReadModelSyntax model => model.Properties,
        TypeSyntax type => type.Properties,
        _ => []
    };
}

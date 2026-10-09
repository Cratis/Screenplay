// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Mcp;

static class McpDeclarationDetails
{
    internal static object Read(McpSnapshot snapshot, JsonElement arguments)
    {
        var declaration = Target(snapshot, arguments);
        var readiness = snapshot.Index.Readiness;
        var view = McpJson.OptionalString(arguments, "view") ?? "summary";
        var details = view switch
        {
            "summary" => new
            {
                propertyCount = Properties(declaration.Syntax).Count(),
                syntaxOnly = readiness.SyntaxOnly(declaration.Syntax),
                executionReadiness = readiness.ExecutionReadiness(declaration.Syntax),
                eventCount = declaration.Syntax is SliceSyntax eventOwner ? EventDeclarations.In(eventOwner).Count() : 0,
                eventId = (declaration.Syntax as EventSyntax)?.Id,
                description = declaration.Syntax.GetType().GetProperty("Description")?.GetValue(declaration.Syntax) as string,
                documentation = declaration.Syntax.GetType().GetProperty("Documentation")?.GetValue(declaration.Syntax) as string,
                uses = (declaration.Syntax as OperationSyntax)?.Uses,
                identifier = (declaration.Syntax as EventSourceSyntax)?.Identifier,
                streamId = (declaration.Syntax as EventStreamSyntax)?.StreamId,
                streamIdParts = (declaration.Syntax as EventStreamSyntax)?.StreamIdParts.Select(part => new { part.Name, part.Type }),
                renameOnlyId = declaration.Syntax switch { EventSourceSyntax source => source.Id, EventStreamSyntax stream => stream.Id, _ => null },
                authoredRoute = (declaration.Syntax as CommandSyntax)?.Stream,
                ambiguousStreamCandidates = (declaration.Syntax as CommandSyntax)?.StreamCandidates,
                operationInputCount = (declaration.Syntax as OperationSyntax)?.Inputs.Count() ?? 0,
                exampleType = (declaration.Syntax as SpecificationExampleSyntax)?.Type,
                exampleFor = (declaration.Syntax as SpecificationExampleSyntax)?.For,
                exampleValueCount = (declaration.Syntax as SpecificationExampleSyntax)?.Values.Count(),
                exampleGeneratedValueCount = (declaration.Syntax as SpecificationExampleSyntax)?.GeneratedValues.Count(),
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
                    response = Response(command, readiness),
                    syntaxOnly = readiness.SyntaxOnly(command),
                    executionReadiness = readiness.ExecutionReadiness(command),
                    producedEvents = readiness.ProducedEvents(command).ToArray()
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
                    givenOperationFailures = specification.GivenOperationFailures,
                    thenOperations = specification.ThenOperations,
                    thenCompensated = specification.ThenCompensated,
                    syntaxOnly = readiness.SyntaxOnly(specification),
                    executionReadiness = readiness.ExecutionReadiness(specification),
                    givenEvents = specification.Given.Count(),
                    thenEvents = specification.ThenEvents.Count(),
                    thenErrors = specification.ThenErrors.Count(),
                    thenReadModels = specification.ThenReadModels?.Count() ?? 0,
                    thenAbsentReadModels = specification.ThenAbsentReadModels.Count(),
                    thenQueries = specification.ThenQueries.Count()
                },
                arguments,
                snapshot.SourceRevision),
            "inputs" when declaration.Syntax is OperationSyntax operation => McpPaging.Page(operation.Inputs, arguments, snapshot.SourceRevision),
            "phases" when declaration.Syntax is OperationSyntax operation => McpPaging.Page(
                new[] { (Name: "execute", Phase: operation.Execute), (Name: "compensate", Phase: operation.Compensate) }.Where(value => value.Phase is not null),
                value => new
                {
                    phase = value.Name,
                    value.Phase!.Description,
                    value.Phase.Location,
                    state = value.Phase switch { { File: not null } => "file", { Code: not null } => "inline", _ => "pending" },
                    file = value.Phase.File?.Path,
                    language = value.Phase.Code?.Language,
                    hintCount = value.Phase.Implementation?.Hints.Count() ?? 0,
                    executionAvailable = false
                },
                arguments,
                snapshot.SourceRevision),
            "dependencies" when declaration.Syntax is ModuleSyntax module => McpPaging.Page(module.DependsOn, arguments, snapshot.SourceRevision),
            "dependencies" when declaration.Syntax is FeatureSyntax feature => McpPaging.Page(feature.DependsOn, arguments, snapshot.SourceRevision),
            "streams" when declaration.Syntax is EventSourceSyntax source => McpPaging.Page(source.Streams, arguments, snapshot.SourceRevision),
            "route" when declaration.Syntax is CommandSyntax routed => new { authoredRoute = routed.Stream, ambiguousStreamCandidates = routed.StreamCandidates, executionAvailable = false, executionReadiness = readiness.ExecutionReadiness(routed) },
            "response" when declaration.Syntax is CommandSyntax responseOwner => Response(responseOwner, readiness),
            "produces" when declaration.Syntax is CommandSyntax command => McpPaging.Page(command.Produces, arguments, snapshot.SourceRevision),
            "values" when declaration.Syntax is ConceptSyntax concept => McpPaging.Page(concept.Values, arguments, snapshot.SourceRevision),
            "values" when declaration.Syntax is SpecificationExampleSyntax example => McpPaging.Page(example.Values, arguments, snapshot.SourceRevision),
            "generatedValues" when declaration.Syntax is SpecificationExampleSyntax example => McpPaging.Page(example.GeneratedValues, arguments, snapshot.SourceRevision),
            "caller" when declaration.Syntax is PersonaSyntax persona => PersonaCaller(persona, snapshot),
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
        if (kind == "EventSource" || kind == "EventStream")
        {
            var parts = address.Split('.');
            var stream = kind == "EventStream" ? parts.ElementAtOrDefault(1) ?? string.Empty : null;
            var confidence = snapshot.Index.SourceConfidence?.Resolve(parts[0], stream);
            if (confidence?.State == "ambiguous" || confidence?.State == "incomplete")
            {
                var candidates = confidence.Sources.Select(source => new { source.Name, source.Location });
                throw new McpFailure($"{(confidence.State == "ambiguous" ? "AmbiguousDeclaration" : "IncompleteSource")}: {string.Join(' ', confidence.Reasons)} Physical candidates: {JsonSerializer.Serialize(candidates)}")
                {
                    FailureKind = confidence.State == "ambiguous" ? "AmbiguousDeclaration" : "IncompleteSource"
                };
            }
        }
        return matches.Length == 1 ? matches[0] : throw new McpFailure($"Declaration target must identify exactly one logical declaration; found {matches.Length}.");
    }

    static IEnumerable<string> Views(SyntaxNode node) => node switch
    {
        ModuleSyntax or FeatureSyntax => ["summary", "dependencies", "occurrences", "syntax"],
        SliceSyntax => ["summary", "occurrences", "commands", "specifications", "syntax"],
        CommandSyntax => ["summary", "properties", "occurrences", "produces", "response", "route", "syntax"],
        EventSourceSyntax => ["summary", "streams", "occurrences", "syntax"],
        OperationSyntax => ["summary", "inputs", "phases", "occurrences", "syntax"],
        EventSyntax or ReadModelSyntax or TypeSyntax => ["summary", "properties", "occurrences", "syntax"],
        ConceptSyntax => ["summary", "values", "occurrences", "syntax"],
        SpecificationExampleSyntax => ["summary", "values", "generatedValues", "occurrences", "syntax"],
        PersonaSyntax => ["summary", "caller", "occurrences", "syntax"],
        _ => ["summary", "occurrences", "syntax"]
    };

    static object PersonaCaller(PersonaSyntax persona, McpSnapshot snapshot)
    {
        var application = snapshot.Compilation.Value ?? throw new McpFailure("Persona caller synthesis requires a parsed application.");
        var result = PersonaCallers.Synthesize(persona, application);
        return new { result.Caller, result.Contributions, result.Refusal };
    }

    static object Response(CommandSyntax command, McpAuthoringReadiness readiness)
    {
        var properties = command.Properties.GroupBy(property => property.Name, StringComparer.Ordinal)
            .Where(group => group.Count() == 1).ToDictionary(group => group.Key, group => group.Single(), StringComparer.Ordinal);
        return new
        {
            syntaxOnly = readiness.SyntaxOnly(command),
            executionReadiness = readiness.ExecutionReadiness(command),
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

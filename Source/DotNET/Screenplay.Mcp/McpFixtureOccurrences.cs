// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Mcp;

static class McpFixtureOccurrences
{
    internal static string Role(SpecificationSyntax specification, SyntaxNode node, string fallback) => Indexing.SpecificationReferenceRoles.For(specification, node, fallback);

    internal static IEnumerable<McpFixtureOccurrence> All(McpSyntaxIndex index, ApplicationSyntax? application)
    {
        var expanded = application is null ? null : SpecificationExamples.Expand(application);
        if (expanded?.Diagnostics.Any(diagnostic => diagnostic.Severity == Diagnostics.DiagnosticSeverity.Error && diagnostic.Code != Diagnostics.DiagnosticCodes.UnsynthesizablePersonaCaller) == true)
        {
            throw new McpFailure($"SpecificationExampleExpansionFailed: fixture values cannot be reported while example resolution has errors. {string.Join("; ", expanded.Diagnostics.Select(diagnostic => diagnostic.Message))}");
        }

        return index.Declarations.Where(declaration => declaration.Syntax is SpecificationSyntax).SelectMany(declaration =>
        {
            var specifications = expanded?.Specifications.Where(specification => specification.Authored.Name == declaration.Name && specification.Authored.Location == declaration.Location).ToArray() ?? [];
            return specifications.Length == 0 ? For(declaration, null) : specifications.SelectMany(specification => For(declaration, specification));
        });
    }

    static IEnumerable<McpFixtureOccurrence> For(McpDeclaration declaration, EffectiveSpecification? expanded)
    {
        var specification = expanded?.Effective ?? (SpecificationSyntax)declaration.Syntax;
        var ordinal = 0;
        var owner = expanded?.Case is null ? declaration.Owner : declaration.Owner with { Name = specification.Name, Address = string.Join('.', declaration.Scope.Append(specification.Name)) };
        if (specification.GivenCaller is { } caller && expanded?.Authored.GivenCallerPersona is { } persona)
        {
            yield return Occurrence(persona.Name, "Persona", "givenCaller", caller, []);
        }
        foreach (var item in specification.Given)
        {
            yield return Occurrence(item.EventType, "Event", "givenEvent", item, item.Values, item.For, item.Stream, item.NoStream);
        }

        if (specification.WhenAppended is { } appended)
        {
            yield return Occurrence(appended.EventType, "Event", "whenAppendedEvent", appended, appended.Values, appended.For, appended.Stream, appended.NoStream);
        }

        if (specification.WhenRedelivered is { } redelivered)
        {
            yield return Occurrence(redelivered.EventType, "Event", "whenRedeliveredEvent", redelivered, redelivered.Values, redelivered.For, redelivered.Stream, redelivered.NoStream);
        }

        foreach (var item in specification.ThenEvents)
        {
            yield return Occurrence(item.EventType, "Event", "thenEvent", item, item.Values, item.For, item.Stream, item.NoStream);
        }

        if (specification.When is { } when)
        {
            yield return Occurrence(when.CommandType, "Command", "whenCommand", when, when.Values, when.For);
            if (when.GeneratedValues.Any())
            {
                yield return Occurrence(when.CommandType, "Command", "generatedValues", when, when.GeneratedValues);
            }

            if (specification.ThenReturns is RecordSpecificationReturnSyntax record)
            {
                yield return Occurrence(when.CommandType, "Command", "thenReturns", record, record.Fields);
            }
            else if (specification.ThenReturns is ScalarSpecificationReturnSyntax scalar)
            {
                yield return Occurrence(when.CommandType, "Command", "thenReturns", scalar, [new PropertyMappingSyntax("returns", scalar.Value, scalar.Location)]);
            }
        }

        foreach (var item in (specification.GivenReadModels ?? []).Concat(specification.ThenReadModels ?? []))
        {
            yield return Occurrence(item.Name, "ReadModel", Role(specification, item, string.Empty), item, item.Properties);
        }

        foreach (var item in specification.ThenAbsentReadModels)
        {
            yield return Occurrence(item.Name, "ReadModel", "thenAbsentReadModel", item, [], item.Key);
        }

        foreach (var query in specification.ThenQueries)
        {
            yield return Occurrence(query.Query, "Query", "queryArguments", query, query.Arguments);
            foreach (var result in query.Results)
            {
                yield return Occurrence(query.Query, "Query", "queryResult", result, result.Properties);
            }
        }

        if (specification.WhenQuery is { } performed)
        {
            yield return Occurrence(performed.Query, "Query", "whenQueryArguments", performed, performed.Arguments);
            foreach (var result in specification.ThenResults) yield return Occurrence(performed.Query, "Query", "thenResult", result, result.Properties);
        }
        foreach (var capture in specification.GivenCaptures) yield return Occurrence(capture.Capture, "Capture", "givenCapture", capture, capture.Record);
        if (specification.WhenCapture is { } captured) yield return Occurrence(captured.Capture, "Capture", "whenCapture", captured, captured.Record);
        if (specification.WhenTrigger is { } trigger) yield return Occurrence(trigger.Trigger, "Trigger", "whenTrigger", trigger, trigger.Values);
        foreach (var error in specification.ThenErrors.Where(error => error.Name is not null)) yield return Occurrence(specification.Name, "Specification", "thenError", error, [new("message", new LiteralExpressionSyntax(error.Name, error.Location), error.Location)]);

        McpFixtureOccurrence Occurrence(string name, string kind, string role, SyntaxNode node, IEnumerable<PropertyMappingSyntax> values, ExpressionSyntax? destination = null, SpecificationStreamSyntax? stream = null, SpecificationNoStreamSyntax? noStream = null) =>
            new(owner, role, new(name, [kind], declaration.Scope, node.Location, role, owner), ordinal++, values, destination, stream, noStream, expanded?.Steps.SingleOrDefault(step => ReferenceEquals(step.Effective, node)))
            {
                Table = expanded?.Case is null ? null : declaration.Address,
                Case = expanded?.Case?.Name,
                CaseValues = expanded?.Case?.Values ?? []
            };
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax.Specifications;

public static partial class SpecificationExamples
{
    internal static IEnumerable<(string Role, SyntaxNode Node, IEnumerable<PropertyMappingSyntax> Values)> CaseSteps(SpecificationSyntax specification)
    {
        foreach (var step in specification.Given) yield return ("given", step, EventValues(step));
        foreach (var step in specification.GivenReadModels ?? []) yield return ("given readmodel", step, step.Properties);
        foreach (var step in specification.GivenCaptures) yield return ("given capture", step, step.Record);
        if (specification.When is { } command) yield return ("when", command, command.Values.Concat(command.GeneratedValues.Select(value => value with { Property = $"generated {value.Property}" })).Concat(ForValue(command.For)));
        if (specification.WhenAppended is { } appended) yield return ("when append", appended, EventValues(appended));
        if (specification.WhenTrigger is { } trigger) yield return ("when trigger", trigger, trigger.Values);
        if (specification.WhenCapture is { } capture) yield return ("when capture", capture, capture.Record);
        if (specification.WhenQuery is { } query) yield return ("when query", query, query.Arguments);
        foreach (var step in specification.ThenEvents) yield return ("then", step, EventValues(step));
        foreach (var step in specification.ThenReadModels ?? []) yield return ("then readmodel", step, step.Properties);
        foreach (var step in specification.ThenAbsentReadModels) yield return ("then no readmodel", step, [new("for", step.Key, step.Location)]);
        foreach (var step in specification.ThenQueries)
        {
            yield return ("then query", step, step.Arguments);
            foreach (var result in step.Results) yield return ("then query result", result, result.Properties);
        }
        foreach (var step in specification.ThenResults) yield return ("then result", step, step.Properties);
        foreach (var step in specification.ThenErrors.Where(step => step.CaseValue is not null)) yield return ("then error", step, [new("message", step.CaseValue!, step.Location)]);
        foreach (var step in specification.ThenOperations) yield return ("then operation", step, step.Values);
        if (specification.ThenReturns is ScalarSpecificationReturnSyntax scalar) yield return ("then returns", scalar, [new("value", scalar.Value, scalar.Location)]);
        if (specification.ThenReturns is RecordSpecificationReturnSyntax record) yield return ("then returns", record, record.Fields);
    }

    static IEnumerable<PropertyMappingSyntax> ForValue(ExpressionSyntax? value) => value is null ? [] : [new("for", value, value.Location)];

    static IEnumerable<PropertyMappingSyntax> EventValues(SpecificationEventSyntax step) => step.Values.Concat(ForValue(step.For))
        .Concat(step.Stream?.StreamId is { } streamId ? [streamId] : []).Concat(step.Stream?.StreamIdParts ?? []);
}

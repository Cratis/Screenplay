// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Validates the specification actions that are not commands or appended events - a query performed, an
/// application trigger firing and a capture's source record - against what the application declares.
/// </summary>
internal static class SpecificationActionValidator
{
    /// <summary>
    /// Validates every specification of the application.
    /// </summary>
    /// <param name="application">The <see cref="ApplicationSyntax"/>.</param>
    /// <param name="declarations">The <see cref="ConsistencyDeclarations"/> of the application.</param>
    /// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
    public static void Validate(ApplicationSyntax application, ConsistencyDeclarations declarations, ParserContext context)
    {
        var triggers = (application.Triggers ?? []).ToDictionary(trigger => trigger.Name, StringComparer.Ordinal);
        var captures = declarations.Slices.SelectMany(entry => entry.Slice.Captures).Select(capture => capture.Name).ToHashSet(StringComparer.Ordinal);
        foreach (var (slice, scope) in declarations.Slices)
        {
            foreach (var specification in slice.Specifications)
            {
                ValidateQueryAction(specification, scope, declarations, context);
                if (specification.WhenTrigger is { } trigger)
                {
                    ValidateTrigger(trigger, triggers, context);
                }

                foreach (var capture in specification.GivenCaptures.Append(specification.WhenCapture).OfType<SpecificationCaptureSyntax>()
                    .Where(capture => !captures.Contains(capture.Capture)))
                {
                    context.Warning(DiagnosticCodes.UnknownSpecificationCapture, $"Unknown capture '{capture.Capture}' - declare it with 'capture {capture.Capture}' in a slice", capture.Location);
                }
            }
        }
    }

    static void ValidateQueryAction(SpecificationSyntax specification, DeclarationScope scope, ConsistencyDeclarations declarations, ParserContext context)
    {
        if (specification.WhenQuery is null)
        {
            foreach (var orphan in specification.ThenResults.Cast<SyntaxNode>().Append(specification.ThenNoResult).OfType<SyntaxNode>())
            {
                context.Error(DiagnosticCodes.MismatchedSpecificationQueryResult, "A query result is asserted without 'when query' - perform the query first, or assert it with 'then query'", orphan.Location);
            }

            return;
        }

        var performed = specification.WhenQuery;
        if (!specification.ThenResults.Any() && specification.ThenNoResult is null && specification.ThenDenied is null)
        {
            context.Error(DiagnosticCodes.MismatchedSpecificationQueryResult, $"'when query {performed.Query}' asserts nothing - add 'then result', 'then no result' or 'then denied'", performed.Location);
        }

        if (specification.ThenResults.Any() && specification.ThenNoResult is not null)
        {
            context.Error(DiagnosticCodes.MismatchedSpecificationQueryResult, "A query cannot both return results and return nothing", specification.ThenNoResult.Location);
        }

        if (declarations.Resolve(performed.Query, scope, item => item.Queries, node => node.Name) is not { } resolved)
        {
            return;
        }

        var parameters = (resolved.Node.By is null ? Enumerable.Empty<QueryParameterSyntax>() : [resolved.Node.By]).Concat(resolved.Node.ByParts).Concat(resolved.Node.Filters).ToList();
        foreach (var argument in performed.Arguments.Where(argument => !parameters.Exists(parameter => parameter.Name == argument.Property)))
        {
            context.Error(
                DiagnosticCodes.UnknownSpecificationQueryArgument,
                $"Query '{performed.Query}' takes no argument '{argument.Property}' - its parameters are {(parameters.Count == 0 ? "none" : string.Join(", ", parameters.Select(parameter => $"'{parameter.Name}'")))}",
                argument.Location);
        }

        SpecificationValueConsistencyValidator.ValidateValues(performed.Arguments, parameters.Select(parameter => new PropertySyntax(parameter.Name, parameter.Type, parameter.Location)), declarations, context);
        foreach (var result in specification.ThenResults)
        {
            SpecificationValueConsistencyValidator.ValidateValues(result.Properties, declarations.ViewProperties(resolved.Node.ReturnType.Name, resolved.Scope), declarations, context);
        }
    }

    static void ValidateTrigger(SpecificationTriggerSyntax trigger, Dictionary<string, TriggerSyntax> triggers, ParserContext context)
    {
        IEnumerable<string>? values;
        if (triggers.TryGetValue(trigger.Trigger, out var declared))
        {
            values = declared.Data.Select(datum => datum.Name);
        }
        else if (context.Languages.Triggers.TryGetValue(trigger.Trigger, out var registered))
        {
            values = registered.Values;
        }
        else
        {
            context.Warning(DiagnosticCodes.UnknownSpecificationTrigger, $"Unknown trigger '{trigger.Trigger}' - declare it with 'trigger {trigger.Trigger}'", trigger.Location);
            return;
        }

        // A registration that does not describe its shape leaves the values alone, as it does for a reaction.
        if (values is null)
        {
            return;
        }

        var known = values.ToHashSet(StringComparer.Ordinal);
        foreach (var value in trigger.Values.Where(value => !known.Contains(value.Property)))
        {
            context.Warning(DiagnosticCodes.UnknownSpecificationTrigger, $"Trigger '{trigger.Trigger}' carries no value '{value.Property}'", value.Location);
        }
    }
}

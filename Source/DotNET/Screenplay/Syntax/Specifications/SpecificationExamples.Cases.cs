// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Parsing;

namespace Cratis.Screenplay.Syntax.Specifications;

public static partial class SpecificationExamples
{
    private sealed partial class Expansion
    {
        SpecificationSyntax[] ExpandSliceSpecifications(SliceSyntax slice)
        {
            var scope = _slices.First(entry => ReferenceEquals(entry.Slice, slice)).Scope;
            var specifications = slice.Specifications.SelectMany(specification => ExpandSpecifications(specification, scope)).ToArray();
            foreach (var origin in _specifications.Where(origin => origin.Case is not null && specifications.Any(node => ReferenceEquals(node, origin.Effective))))
            {
                if (specifications.Count(specification => specification.Name == origin.Effective.Name) > 1 || slice.Specifications.Any(authored => !ReferenceEquals(authored, origin.Authored) && authored.Name == origin.Effective.Name))
                {
                    _context.Error(DiagnosticCodes.SpecificationCaseNameCollision, $"Case '{origin.Case!.Name}' derives specification '{origin.Effective.Name}', which collides in this slice.", origin.Case.Location);
                }
            }

            return specifications;
        }

        IEnumerable<SpecificationSyntax> ExpandSpecifications(SpecificationSyntax specification, DeclarationScope scope)
        {
            var expanded = ExpandSpecification(specification, scope);
            if (!specification.Parameters.Any() && !specification.Cases.Any())
            {
                _specifications.Add(expanded);
                yield return expanded.Effective;
                yield break;
            }
            SpecificationParser.ValidateTable(specification, _context);
            ValidateParameterReferences(expanded.Effective, scope);
            foreach (var row in specification.Cases)
            {
                var substitution = new CaseSubstitution(row);
                var effective = substitution.Specification(expanded.Effective) with
                {
                    Name = $"{specification.Name}_{row.Name}",
                    Parameters = [],
                    Cases = []
                };
                var steps = expanded.Steps.Select(step => step with
                {
                    Effective = substitution.Step(step.Effective),
                    Values = [.. step.Values.Select(value => value.Value is CaseValueExpressionSyntax reference
                        ? value with { Value = substitution.Value(reference), Origin = SpecificationValueOrigin.Case, CaseParameter = reference.Parameter } : value)],
                    Route = step.Route is { } route ? route with { Value = substitution.Step(route.Value) } : null
                }).ToList();

                // Non-example steps still expose case provenance to fixture discovery and failure enrichment.
                foreach (var step in CaseSteps(expanded.Effective).Where(step => !expanded.Steps.Any(existing => ReferenceEquals(existing.Effective, step.Node))))
                {
                    var values = step.Values.Where(value => value.Source is CaseValueExpressionSyntax).Select(value => new EffectiveSpecificationValue(value.Property, substitution.Value(value.Source), SpecificationValueOrigin.Case, null)
                    {
                        CaseParameter = ((CaseValueExpressionSyntax)value.Source).Parameter
                    }).ToArray();
                    if (values.Length > 0) steps.Add(new(step.Role, step.Node, substitution.Step(step.Node), null, values));
                }
                _specifications.Add(new(specification, effective, steps) { Case = row });
                yield return effective;
            }
        }
    }

    sealed class CaseSubstitution(SpecificationCaseSyntax row)
    {
        readonly Dictionary<SyntaxNode, SyntaxNode> _steps = new(ReferenceEqualityComparer.Instance);

        internal ExpressionSyntax Value(ExpressionSyntax value) => value is CaseValueExpressionSyntax reference
            ? row.Values.FirstOrDefault(assignment => assignment.Property == reference.Parameter)?.Source ?? value : value;

        internal IEnumerable<PropertyMappingSyntax> Values(IEnumerable<PropertyMappingSyntax> values) => [.. values.Select(value => value with { Source = Value(value.Source) })];

        internal SyntaxNode Step(SyntaxNode step)
        {
            if (_steps.TryGetValue(step, out var rewritten)) return rewritten;
            rewritten = RewriteStep(step);
            _steps.Add(step, rewritten);

            return rewritten;
        }

        internal SyntaxNode RewriteStep(SyntaxNode step) => step switch
        {
            SpecificationEventSyntax item => item with { Values = Values(item.Values), For = item.For is null ? null : Value(item.For), Stream = item.Stream is null ? null : (SpecificationStreamSyntax)Step(item.Stream) },
            SpecificationCommandSyntax item => item with { Values = Values(item.Values), GeneratedValues = Values(item.GeneratedValues), For = item.For is null ? null : Value(item.For) },
            SpecificationReadModelSyntax item => item with { Properties = Values(item.Properties) },
            SpecificationAbsentReadModelSyntax item => item with { Key = Value(item.Key) },
            SpecificationCaptureSyntax item => item with { Record = Values(item.Record) },
            SpecificationTriggerSyntax item => item with { Values = Values(item.Values) },
            SpecificationWhenQuerySyntax item => item with { Arguments = Values(item.Arguments) },
            SpecificationQuerySyntax item => item with { Arguments = Values(item.Arguments), Results = [.. item.Results.Select(result => (SpecificationQueryResultSyntax)Step(result))] },
            SpecificationQueryResultSyntax item => item with { Properties = Values(item.Properties) },
            SpecificationErrorSyntax { CaseValue: { } reference } item => item with { Name = (Value(reference) as LiteralExpressionSyntax)?.Value as string, CaseValue = null },
            SpecificationStreamSyntax item => item with { StreamId = item.StreamId is null ? null : item.StreamId with { Source = Value(item.StreamId.Source) }, StreamIdParts = Values(item.StreamIdParts) },
            SpecificationOperationSyntax item => item with { Values = Values(item.Values) },
            ScalarSpecificationReturnSyntax item => item with { Value = Value(item.Value) },
            RecordSpecificationReturnSyntax item => item with { Fields = Values(item.Fields) },
            _ => step
        };

        internal SpecificationSyntax Specification(SpecificationSyntax specification) => specification with
        {
            Given = [.. specification.Given.Select(step => (SpecificationEventSyntax)Step(step))],
            GivenReadModels = [.. (specification.GivenReadModels ?? []).Select(step => (SpecificationReadModelSyntax)Step(step))],
            GivenCaptures = [.. specification.GivenCaptures.Select(step => (SpecificationCaptureSyntax)Step(step))],
            When = specification.When is null ? null : (SpecificationCommandSyntax)Step(specification.When),
            WhenAppended = specification.WhenAppended is null ? null : (SpecificationEventSyntax)Step(specification.WhenAppended),
            WhenTrigger = specification.WhenTrigger is null ? null : (SpecificationTriggerSyntax)Step(specification.WhenTrigger),
            WhenCapture = specification.WhenCapture is null ? null : (SpecificationCaptureSyntax)Step(specification.WhenCapture),
            WhenQuery = specification.WhenQuery is null ? null : (SpecificationWhenQuerySyntax)Step(specification.WhenQuery),
            ThenEvents = [.. specification.ThenEvents.Select(step => (SpecificationEventSyntax)Step(step))],
            ThenReadModels = [.. (specification.ThenReadModels ?? []).Select(step => (SpecificationReadModelSyntax)Step(step))],
            ThenAbsentReadModels = [.. specification.ThenAbsentReadModels.Select(step => (SpecificationAbsentReadModelSyntax)Step(step))],
            ThenQueries = [.. specification.ThenQueries.Select(step => (SpecificationQuerySyntax)Step(step))],
            ThenResults = [.. specification.ThenResults.Select(step => (SpecificationQueryResultSyntax)Step(step))],
            ThenErrors = [.. specification.ThenErrors.Select(step => (SpecificationErrorSyntax)Step(step))],
            ThenOperations = [.. specification.ThenOperations.Select(step => (SpecificationOperationSyntax)Step(step))],
            ThenReturns = specification.ThenReturns is null ? null : (SpecificationReturnSyntax)Step(specification.ThenReturns)
        };
    }
}

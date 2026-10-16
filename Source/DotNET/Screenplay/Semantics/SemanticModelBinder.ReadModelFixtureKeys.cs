// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics;

public sealed partial class SemanticModelBinder
{
    private sealed partial class BindingContext
    {
        SemanticValue? BindReadModelStateKey(BoundReadModel readModel, ImmutableArray<SemanticPropertyValue> values, SourceLocation location, SemanticValue? inferredKey)
        {
            var parts = ImmutableArray.CreateBuilder<SemanticPropertyValue>();
            var inferred = (inferredKey as SemanticCompositeValue)?.Properties ?? [];
            foreach (var property in readModel.Model.Properties.Where(property => property.IsIdentifier))
            {
                var value = values.FirstOrDefault(value => value.TargetProperty == property.Id)?.Value ??
                    inferred.FirstOrDefault(value => value.TargetProperty == property.Id)?.Value;
                if (value is null)
                {
                    Error(DiagnosticCodes.MissingSpecificationReadModelIdentifier,
                        $"Specification read model '{readModel.Model.Name}' must state key part '{property.Name}' in this block.", location,
                        DiagnosticArguments.Empty.With("readmodel", DiagnosticArgument.Text(readModel.Model.Name)).With("property", DiagnosticArgument.Text(property.Name)));
                    return null;
                }
                parts.Add(new(property.Id, value));
            }

            return SemanticValue.Composite(parts.ToImmutable());
        }

        SemanticValue? BindAbsentReadModelKey(BoundReadModel readModel, ExpressionSyntax expression, SourceLocation location)
        {
            var keys = readModel.Model.Properties.Where(property => property.IsIdentifier).ToArray();
            if (expression is not ObjectExpressionSyntax obj)
            {
                Error(DiagnosticCodes.IncompleteReadModelKey, "A composite read-model absence key must supply every named part.", location);
                return null;
            }
            var values = ImmutableArray.CreateBuilder<SemanticPropertyValue>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var member in obj.Members)
            {
                var property = keys.SingleOrDefault(property => property.Name == member.Name);
                if (property is null || !seen.Add(member.Name))
                {
                    Error(DiagnosticCodes.InvalidReadModelKeyLookup, $"Unknown or repeated key part '{member.Name}'.", member.Location);
                    return null;
                }
                if (BindConcreteValue(member.Value, property.Type, "specification read model key part", false) is not { } value) return null;
                values.Add(new(property.Id, value));
            }
            var missing = keys.Where(property => !seen.Contains(property.Name)).Select(property => property.Name).ToArray();
            if (missing.Length > 0)
            {
                Error(DiagnosticCodes.IncompleteReadModelKey, $"Read-model lookup is missing key parts: {string.Join(", ", missing)}.", location);
                return null;
            }

            return SemanticValue.Composite(values.ToImmutable());
        }
    }
}

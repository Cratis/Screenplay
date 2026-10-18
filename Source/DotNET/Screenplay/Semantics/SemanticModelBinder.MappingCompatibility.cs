// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics;

public sealed partial class SemanticModelBinder
{
    private sealed partial class BindingContext
    {
        /// <summary>
        /// Reports a mapping whose property source cannot be assigned to its target at the mapping, with the same rule the
        /// validator enforces on the finished model, so the defect is not left to surface as a root-located contract error.
        /// </summary>
        /// <param name="mapping">The authored mapping.</param>
        /// <param name="sources">The properties a path source can name.</param>
        /// <param name="target">The target property.</param>
        /// <param name="declaration">The kind and name of the declaration the mapping assigns into, such as <c>event 'AlertRaised'</c>.</param>
        /// <returns><see langword="true"/> when the mapping is compatible or its source is not a property.</returns>
        bool IsCompatibleMapping(PropertyMappingSyntax mapping, Dictionary<string, SemanticProperty> sources, SemanticProperty target, string declaration)
        {
            if (mapping.Source is not PathExpressionSyntax path || !sources.TryGetValue(path.Path, out var source) ||
                SemanticTypeCompatibility.IsCompatibleSource(source.Type, target.Type) ||
                SemanticTypeCompatibility.ConvertsConcept(source.Type, target.Type, id => _boundConcepts.GetValueOrDefault(id)))
            {
                return true;
            }

            var targetShape = target.Type.IsOptional ? "optional" : "required";
            var sourceType = DescribeType(source.Type);
            var message = SemanticTypeCompatibility.SameValueType(source.Type, target.Type) && source.Type.IsCollection == target.Type.IsCollection
                ? $"Mapping '{mapping.Property}' into {declaration} assigns {Article(source.Type)} '{sourceType}' to the {targetShape} property '{target.Name}'."
                : $"Mapping '{mapping.Property}' into {declaration} assigns {Article(source.Type)} '{sourceType}' to the {targetShape} property '{target.Name}' of type '{DescribeType(target.Type)}'.";
            Error(DiagnosticCodes.InvalidSemanticBinding, message, mapping.Location);
            return false;
        }

        string Article(SemanticTypeReference type) => type.IsOptional ? "an optional" : "a required";

        string DescribeType(SemanticTypeReference type)
        {
            var name = type.Kind switch
            {
                SemanticTypeReferenceKind.Concept => _concepts.FirstOrDefault(entry => entry.Value.Id == type.Target).Key,
                SemanticTypeReferenceKind.CompositeType => _types.FirstOrDefault(entry => entry.Value.Id == type.Target).Key,
                _ => type.Primitive switch
                {
                    SemanticPrimitiveType.Uuid => "Uuid",
                    SemanticPrimitiveType.Text => "String",
                    SemanticPrimitiveType.WholeNumber => "Int",
                    SemanticPrimitiveType.DecimalNumber => "Decimal",
                    SemanticPrimitiveType.Boolean => "Bool",
                    SemanticPrimitiveType.Date => "Date",
                    SemanticPrimitiveType.DateTime => "DateTime",
                    _ => type.Primitive.ToString()
                }
            };

            return type.IsCollection ? $"{name}[]" : name ?? "unknown";
        }
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Globalization;
using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics;

public sealed partial class SemanticModelBinder
{
    private sealed partial class BindingContext
    {
        [GeneratedRegex(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}(?::\d{2}(?:\.\d+)?)?(?:Z|[+-]\d{2}:\d{2})$", RegexOptions.NonBacktracking)]
        private static partial Regex IsoInstant();

        static bool TryParseSpecificationInstant(string text, out DateTimeOffset instant)
        {
            instant = default;
            return IsoInstant().IsMatch(text) && DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out instant);
        }

        static bool IsFactValue(string description) =>
            string.Equals(description, "specification command", StringComparison.Ordinal) ||
            string.Equals(description, "specification event", StringComparison.Ordinal);

        ImmutableArray<SemanticPropertyValue> BindPropertyValues(
            IEnumerable<PropertyMappingSyntax> values,
            Dictionary<string, SemanticProperty> properties,
            string description,
            IReadOnlySet<SemanticId>? routedInputs = null)
        {
            var bound = ImmutableArray.CreateBuilder<SemanticPropertyValue>();
            var allowNull = string.Equals(description, "specification read model", StringComparison.Ordinal);
            foreach (var value in values)
            {
                if (!properties.TryGetValue(value.Property, out var property))
                {
                    Error(DiagnosticCodes.InvalidSemanticBinding, $"The {description} property '{value.Property}' is unresolved.", value.Location);
                    continue;
                }

                if (BindConcreteValue(value.Source, property.Type, description, allowNull, routedInputs?.Contains(property.Id) == true) is { } concrete)
                {
                    bound.Add(new(property.Id, concrete));
                }
            }

            return bound.ToImmutable();
        }

        SemanticValue? BindConcreteValue(ExpressionSyntax expression, SemanticTypeReference target, string description, bool allowNull, bool routeInput = false) =>
            expression switch
            {
                LiteralExpressionSyntax { Value: null } => BindSpecificationNull(expression, target, description, allowNull),
                ListExpressionSyntax list => BindSpecificationList(list, target, description, allowNull),
                ObjectExpressionSyntax obj => BindSpecificationObject(obj, target, description, allowNull),
                _ => BindSpecificationScalar(expression, target, description, routeInput)
            };

        SemanticValue? BindSpecificationNull(ExpressionSyntax expression, SemanticTypeReference target, string description, bool allowNull)
        {
            if (!allowNull && IsFactValue(description))
            {
                Error(DiagnosticCodes.NullSpecificationFact, $"A {description} cannot contain null: in Chronicle, an optional fact is a separate event.", expression.Location);
                return null;
            }

            if (!allowNull || !target.IsOptional)
            {
                Error(DiagnosticCodes.InvalidSpecificationNull, $"A null {description} requires an optional read-model property.", expression.Location);
                return null;
            }

            return SemanticValue.Null;
        }

        SemanticValue? BindSpecificationList(ListExpressionSyntax list, SemanticTypeReference target, string description, bool allowNull)
        {
            if (!target.IsCollection)
            {
                return InvalidShape(list, description, "a scalar or object");
            }

            var values = ImmutableArray.CreateBuilder<SemanticValue>();
            var valid = true;
            foreach (var item in list.Items)
            {
                var bound = BindConcreteValue(item, target with { IsCollection = false, IsOptional = false }, description, allowNull);
                if (bound is null)
                {
                    valid = false;
                    continue;
                }

                values.Add(bound);
            }

            return valid ? SemanticValue.Array(values.ToImmutable()) : null;
        }

        SemanticValue? BindSpecificationObject(ObjectExpressionSyntax expression, SemanticTypeReference target, string description, bool allowNull)
        {
            if (target.IsCollection)
            {
                return InvalidShape(expression, description, "a list");
            }

            return target.Kind == SemanticTypeReferenceKind.CompositeType
                ? BindStructuredValue(expression, target, description, allowNull)
                : InvalidShape(expression, description, "a scalar");
        }

        SemanticValue? BindSpecificationScalar(ExpressionSyntax expression, SemanticTypeReference target, string description, bool routeInput)
        {
            if (target.IsCollection)
            {
                return InvalidShape(expression, description, "a list");
            }

            if (target.Kind == SemanticTypeReferenceKind.CompositeType)
            {
                return InvalidShape(expression, description, "an object");
            }

            if (expression is LiteralExpressionSyntax literal)
            {
                return BindSpecificationLiteral(literal, target, routeInput);
            }

            // An enumeration member may be written bare or qualified by its concept, exactly as the compiler accepts
            // it - both name the same member as its quoted spelling and bind to the same value.
            if (expression is PathExpressionSyntax path && EnumerationMember(path.Path, target) is { } member)
            {
                return SemanticValue.Text(member);
            }

            Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"The {description} requires a concrete portable value in Program v1.", expression.Location);
            return null;
        }

        SemanticValue BindSpecificationLiteral(LiteralExpressionSyntax literal, SemanticTypeReference target, bool routeInput)
        {
            if (literal.Value is string text && IsDateTime(target) && TryParseSpecificationInstant(text, out var instant))
            {
                return SemanticValue.Text(instant.Offset == TimeSpan.Zero
                    ? instant.UtcDateTime.ToString("O", CultureInfo.InvariantCulture)
                    : instant.ToString("O", CultureInfo.InvariantCulture));
            }

            return routeInput ? BindRouteLiteral(literal) : BindLiteral(literal);
        }

        SemanticValue? BindStructuredValue(ObjectExpressionSyntax expression, SemanticTypeReference target, string description, bool allowNull)
        {
            var declaration = (syntax.Types ?? []).SingleOrDefault(type =>
                _types.TryGetValue(type.Name, out var registered) && registered.Id == target.Target);
            if (declaration is null)
            {
                Error(DiagnosticCodes.UnresolvedStructuredValueType, $"The {description} composite type cannot be resolved.", expression.Location);
                return null;
            }

            var assigned = new HashSet<string>(StringComparer.Ordinal);
            var (properties, valid) = BindStructuredMembers(expression, declaration, assigned, description, allowNull);
            var complete = ValidateStructuredMembers(expression, declaration, assigned, description);
            return valid && complete ? SemanticValue.Composite(properties.ToImmutable()) : null;
        }

        (ImmutableArray<SemanticPropertyValue>.Builder Properties, bool Valid) BindStructuredMembers(
            ObjectExpressionSyntax expression,
            TypeSyntax declaration,
            HashSet<string> assigned,
            string description,
            bool allowNull)
        {
            // A property declared more than once cannot be bound; it is reported where a value names it.
            var declared = declaration.Properties.GroupBy(property => property.Name, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.Count() == 1 ? group.Single() : null, StringComparer.Ordinal);
            var properties = ImmutableArray.CreateBuilder<SemanticPropertyValue>();
            var valid = true;
            foreach (var member in expression.Members)
            {
                if (!assigned.Add(member.Name))
                {
                    Error(DiagnosticCodes.DuplicateSemanticValueMember, $"Duplicate property '{member.Name}' in {description}.", member.Location);
                    valid = false;
                    continue;
                }

                var property = ResolveStructuredMember(member, declared, declaration.Name);
                if (property is null || BindConcreteValue(member.Value, BindTypeReference(property.Type), description, allowNull) is not { } value)
                {
                    valid = false;
                    continue;
                }

                var address = SemanticAddress.ForProperty(_types[declaration.Name].Address, property.Name);
                var id = documents.IdentityCatalog.ResolveSemanticAssignment(address).Id;
                properties.Add(new(id, value));
            }

            return (properties, valid);
        }

        PropertySyntax? ResolveStructuredMember(ObjectMemberSyntax member, Dictionary<string, PropertySyntax?> declared, string typeName)
        {
            if (!declared.TryGetValue(member.Name, out var property))
            {
                Error(DiagnosticCodes.UnknownStructuredValueMember, $"Unknown property '{member.Name}' in structured value for '{typeName}'.", member.Location);
                return null;
            }

            if (property is null)
            {
                Error(DiagnosticCodes.UnknownStructuredValueMember, $"Property '{member.Name}' is declared more than once on '{typeName}', so the structured value cannot bind it.", member.Location);
            }

            return property;
        }

        bool ValidateStructuredMembers(ObjectExpressionSyntax expression, TypeSyntax declaration, HashSet<string> assigned, string description)
        {
            var complete = true;
            foreach (var property in declaration.Properties.Where(property => !property.Type.IsOptional && !assigned.Contains(property.Name)))
            {
                Error(DiagnosticCodes.MissingStructuredValueMember, $"The {description} composite '{declaration.Name}' requires property '{property.Name}'.", expression.Location);
                complete = false;
            }

            return complete;
        }

        // An instant may be written the way people write one - "2026-10-05T08:00:00Z" - and binds to the same
        // round-trip value as its fully written form, so the canonical bytes do not depend on the spelling.
        bool IsDateTime(SemanticTypeReference target) => target.Kind switch
        {
            SemanticTypeReferenceKind.Primitive => target.Primitive == SemanticPrimitiveType.DateTime,
            SemanticTypeReferenceKind.Concept => ConceptShape(target.Target).Primitive == SemanticPrimitiveType.DateTime,
            _ => false
        };

        string? EnumerationMember(string path, SemanticTypeReference target)
        {
            if (target.Kind != SemanticTypeReferenceKind.Concept)
            {
                return null;
            }

            var concept = syntax.Concepts.First(_ => _concepts[_.Name].Id == target.Target);
            if (!concept.IsEnum)
            {
                return null;
            }

            var separator = path.LastIndexOf('.');
            var member = separator < 0 ? path : path[(separator + 1)..];
            var qualifier = separator < 0 ? null : path[..separator];
            return (qualifier is null || string.Equals(qualifier, concept.Name, StringComparison.Ordinal)) && concept.Values.Contains(member, StringComparer.Ordinal)
                ? member
                : null;
        }

        SemanticValue? InvalidShape(ExpressionSyntax expression, string description, string expected)
        {
            Error(DiagnosticCodes.IncompatibleStructuredValue, $"The {description} value must be {expected} for its declared type.", expression.Location);
            return null;
        }
    }
}

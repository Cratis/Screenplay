// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics;

internal static partial class SemanticModelValidator
{
    private sealed partial class ValidationContext
    {
        void ValidateMappings(
            ImmutableArray<SemanticPropertyMapping> mappings,
            Dictionary<SemanticId, SemanticProperty> targets,
            SemanticExpressionRootKind root,
            Dictionary<SemanticId, SemanticProperty> sources,
            HashSet<SemanticId>? routedInputs = null)
        {
            RequireObjects(mappings, nameof(mappings), "property mapping");
            var mapped = new HashSet<SemanticId>();
            foreach (var mapping in mappings)
            {
                if (!mapped.Add(mapping.TargetProperty))
                {
                    throw new InvalidSemanticContract($"Mapping target property '{mapping.TargetProperty}' is duplicated.");
                }

                if (!targets.TryGetValue(mapping.TargetProperty, out var target))
                {
                    throw new InvalidSemanticContract($"Mapping target property '{mapping.TargetProperty}' is unresolved.");
                }

                ValidateMappingSource(mapping, target, root, sources, routedInputs?.Contains(target.Id) == true);
            }
        }

        void ValidateMappingSource(
            SemanticPropertyMapping mapping,
            SemanticProperty target,
            SemanticExpressionRootKind root,
            Dictionary<SemanticId, SemanticProperty> sources,
            bool routeInput)
        {
            if (mapping.Source is SemanticValueExpression { Kind: SemanticExpressionKind.Value } value)
            {
                ValidateValue(value.Value, target.Type, "property mapping", routeInput);
                return;
            }

            var sourceType = ResolveExpression(mapping.Source, root, sources);
            if (mapping.Source is SemanticRefusalExpression && !target.Type.IsCollection && UnderlyingPrimitive(target.Type) == SemanticPrimitiveType.Text)
            {
                return;
            }
            RequireCompatible(sourceType, target.Type, "property mapping");
        }

        SemanticTypeReference? ResolveExpression(
            SemanticExpression expression,
            SemanticExpressionRootKind expectedRoot,
            Dictionary<SemanticId, SemanticProperty> properties)
        {
            RejectNull(expression, "expression");
            ValidateEnum(expression.Kind, SemanticExpressionKind.Unknown, "expression kind");
            return expression switch
            {
                SemanticValueExpression value when expression.Kind == SemanticExpressionKind.Value => ResolveValueExpression(value),
                SemanticEventContextExpression context when expression.Kind == SemanticExpressionKind.EventContext => ResolveEventContext(context, expectedRoot),
                SemanticRefusalExpression refusal when expression.Kind == SemanticExpressionKind.Refusal => ResolveRefusalExpression(refusal),
                SemanticReadPathExpression read when expression.Kind == SemanticExpressionKind.ReadPath && expectedRoot == SemanticExpressionRootKind.Command => ResolveDecisionReadProperty(read.Read, read.PropertyId),
                SemanticResolvedExpression resolved when expression.Kind == SemanticExpressionKind.Resolved => ResolveResolvedExpression(resolved, expectedRoot, properties),
                _ => throw new InvalidSemanticContract("A semantic expression variant is malformed or unknown.")
            };
        }

        SemanticTypeReference? ResolveValueExpression(SemanticValueExpression value)
        {
            RejectNull(value.Value, "semantic value");
            ValidateValueVariant(value.Value);
            return TypeOf(value.Value);
        }

        SemanticTypeReference ResolveEventContext(SemanticEventContextExpression context, SemanticExpressionRootKind expectedRoot)
        {
            ValidateTypeReference(context.Type);
            if (!IsAdmittedOccurrenceContext(context, expectedRoot))
            {
                throw new InvalidSemanticContract("Only scalar command occurrence fields are admitted in ESM v2 produces mappings.");
            }

            var primitive = context.Type.Kind == SemanticTypeReferenceKind.Concept
                ? _concepts[context.Type.Target].Primitive : context.Type.Primitive;
            if (!IsCompatibleOccurrencePrimitive(context.Value, primitive))
            {
                throw new InvalidSemanticContract("The occurrence context field and mapping target have incompatible types.");
            }

            return context.Type;
        }

        bool IsAdmittedOccurrenceContext(SemanticEventContextExpression context, SemanticExpressionRootKind expectedRoot) =>
            _semanticVersion != SemanticVersion.V1 &&
            (expectedRoot == SemanticExpressionRootKind.Command || _occurrenceRoots) &&
            (IsOccurrenceField(context.Value) ||
                (context.Value == SemanticEventContextValueKind.EventSourceIdentity && _occurrenceRoots && expectedRoot == SemanticExpressionRootKind.Event && context.Type.Kind != SemanticTypeReferenceKind.CompositeType)) &&
            IsRequiredScalar(context.Type);

        bool IsOccurrenceField(SemanticEventContextValueKind value) =>
            value is SemanticEventContextValueKind.Occurred or SemanticEventContextValueKind.CausedBySubject or
                SemanticEventContextValueKind.CausedByName or SemanticEventContextValueKind.CausedByUserName;

        bool IsCompatibleOccurrencePrimitive(SemanticEventContextValueKind value, SemanticPrimitiveType primitive) =>
            value == SemanticEventContextValueKind.EventSourceIdentity ||
            (value == SemanticEventContextValueKind.Occurred
                ? primitive == SemanticPrimitiveType.DateTime
                : primitive is SemanticPrimitiveType.Text or SemanticPrimitiveType.Uuid);

        SemanticTypeReference ResolveResolvedExpression(
            SemanticResolvedExpression resolved,
            SemanticExpressionRootKind expectedRoot,
            Dictionary<SemanticId, SemanticProperty> properties)
        {
            ValidateEnum(resolved.Root, SemanticExpressionRootKind.Unknown, "expression root");
            ValidateEnum(resolved.Source, SemanticExpressionSourceKind.Unknown, "expression source");
            if (resolved.Root != expectedRoot || !resolved.Target.IsSet)
            {
                throw new InvalidSemanticContract("A resolved expression uses the wrong root or a default target identity.");
            }

            return resolved.Source switch
            {
                SemanticExpressionSourceKind.Property when properties.TryGetValue(resolved.Target, out var property) => property.Type,
                _ => throw new InvalidSemanticContract($"Resolved expression target '{resolved.Target}' is unresolved in its root scope.")
            };
        }

        void ValidateValue(SemanticValue value, SemanticTypeReference target, string description, bool routeInput = false)
        {
            _valueValidator.Validate(value, target, description);
            if (!routeInput) UsesExactNumberLiterals = UsesExactNumberLiterals || ContainsExactNumberLiteral(value);
        }

        void ValidateValueVariant(SemanticValue value)
        {
            _valueValidator.ValidateVariant(value);
            UsesExactNumberLiterals = UsesExactNumberLiterals || ContainsExactNumberLiteral(value);
        }

        SemanticTypeReference? TypeOf(SemanticValue value) => _valueValidator.TypeOf(value);
    }
}

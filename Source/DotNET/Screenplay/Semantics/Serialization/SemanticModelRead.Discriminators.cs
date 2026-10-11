// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Serialization;

internal static partial class SemanticModelRead
{
    static readonly DiscriminatorTable<SemanticSliceKind> _sliceKinds = new("slice kind", new()
    {
        ["stateChange"] = SemanticSliceKind.StateChange,
        ["stateView"] = SemanticSliceKind.StateView,
        ["automation"] = SemanticSliceKind.Automation,
        ["translate"] = SemanticSliceKind.Translate
    });

    static readonly DiscriminatorTable<SemanticPrimitiveType> _primitives = new("primitive type", new()
    {
        ["uuid"] = SemanticPrimitiveType.Uuid,
        ["string"] = SemanticPrimitiveType.Text,
        ["integer"] = SemanticPrimitiveType.WholeNumber,
        ["decimal"] = SemanticPrimitiveType.DecimalNumber,
        ["boolean"] = SemanticPrimitiveType.Boolean,
        ["date"] = SemanticPrimitiveType.Date,
        ["dateTime"] = SemanticPrimitiveType.DateTime
    });

    static readonly DiscriminatorTable<SemanticTypeReferenceKind> _typeReferenceKinds = new("type reference kind", new()
    {
        ["primitive"] = SemanticTypeReferenceKind.Primitive,
        ["concept"] = SemanticTypeReferenceKind.Concept,
        ["compositeType"] = SemanticTypeReferenceKind.CompositeType
    });

    static readonly DiscriminatorTable<SemanticValidationRuleKind> _validationKinds = new("validation kind", new()
    {
        ["notEmpty"] = SemanticValidationRuleKind.NotEmpty,
        ["maximum"] = SemanticValidationRuleKind.Maximum,
        ["minimum"] = SemanticValidationRuleKind.Minimum,
        ["equal"] = SemanticValidationRuleKind.Equal,
        ["notEqual"] = SemanticValidationRuleKind.NotEqual,
        ["greaterThan"] = SemanticValidationRuleKind.GreaterThan,
        ["greaterThanOrEqual"] = SemanticValidationRuleKind.GreaterThanOrEqual,
        ["lessThan"] = SemanticValidationRuleKind.LessThan,
        ["lessThanOrEqual"] = SemanticValidationRuleKind.LessThanOrEqual,
        ["length"] = SemanticValidationRuleKind.Length,
        ["allGreaterThan"] = SemanticValidationRuleKind.AllGreaterThan,
        ["allGreaterThanOrEqual"] = SemanticValidationRuleKind.AllGreaterThanOrEqual,
        ["matches"] = SemanticValidationRuleKind.Matches,
        ["rulePredicate"] = SemanticValidationRuleKind.RulePredicate,
        ["codeValidation"] = SemanticValidationRuleKind.CodeValidation
    });

    static readonly DiscriminatorTable<SemanticExpressionKind> _expressionKinds = new("expression kind", new()
    {
        ["value"] = SemanticExpressionKind.Value,
        ["resolved"] = SemanticExpressionKind.Resolved,
        ["eventContext"] = SemanticExpressionKind.EventContext
    });

    static readonly DiscriminatorTable<SemanticExpressionRootKind> _expressionRoots = new("expression root", new()
    {
        ["command"] = SemanticExpressionRootKind.Command,
        ["event"] = SemanticExpressionRootKind.Event,
        ["trigger"] = SemanticExpressionRootKind.Trigger
    });

    static readonly DiscriminatorTable<SemanticExpressionSourceKind> _expressionSources = new("expression source", new()
    {
        ["property"] = SemanticExpressionSourceKind.Property
    });

    static readonly DiscriminatorTable<SemanticValueKind> _valueKinds = new("value kind", new()
    {
        ["null"] = SemanticValueKind.Null,
        ["string"] = SemanticValueKind.Text,
        ["number"] = SemanticValueKind.Number,
        ["boolean"] = SemanticValueKind.Boolean,
        ["array"] = SemanticValueKind.Array,
        ["object"] = SemanticValueKind.Composite
    });

    static readonly DiscriminatorTable<AffectedInstanceCardinality> _affectedCardinalities = new("affected instance cardinality", new()
    {
        ["one"] = AffectedInstanceCardinality.One,
        ["zeroOrOne"] = AffectedInstanceCardinality.ZeroOrOne,
        ["many"] = AffectedInstanceCardinality.Many
    });

    static readonly DiscriminatorTable<SemanticQueryCardinality> _queryCardinalities = new("query cardinality", new()
    {
        ["one"] = SemanticQueryCardinality.One,
        ["zeroOrOne"] = SemanticQueryCardinality.ZeroOrOne,
        ["many"] = SemanticQueryCardinality.Many
    });

    static readonly DiscriminatorTable<SemanticQueryDelivery> _queryDeliveries = new("query delivery", new()
    {
        ["snapshot"] = SemanticQueryDelivery.Snapshot,
        ["live"] = SemanticQueryDelivery.Live
    });

    internal static SemanticTranslationDirection ParseDirection(string value) => value switch
    {
        "inbound" => SemanticTranslationDirection.Inbound,
        "outbound" => SemanticTranslationDirection.Outbound,
        _ => throw new InvalidSemanticContract($"Unknown translation direction '{value}'.")
    };

    internal static SemanticSliceKind ParseSliceKind(string value) => _sliceKinds.Parse(value);

    internal static SemanticPrimitiveType ParsePrimitive(string value) => _primitives.Parse(value);

    internal static SemanticTypeReferenceKind ParseTypeReferenceKind(string value) => _typeReferenceKinds.Parse(value);

    internal static SemanticValidationRuleKind ParseValidationKind(string value) => _validationKinds.Parse(value);

    internal static SemanticExpressionKind ParseExpressionKind(string value) => _expressionKinds.Parse(value);

    internal static SemanticExpressionRootKind ParseExpressionRoot(string value) => _expressionRoots.Parse(value);

    internal static SemanticExpressionSourceKind ParseExpressionSource(string value) => _expressionSources.Parse(value);

    internal static SemanticValueKind ParseValueKind(string value) => _valueKinds.Parse(value);

    internal static AffectedInstanceCardinality ParseAffectedCardinality(string value) => _affectedCardinalities.Parse(value);

    internal static SemanticQueryCardinality ParseQueryCardinality(string value) => _queryCardinalities.Parse(value);

    internal static SemanticQueryDelivery ParseQueryDelivery(string value) => _queryDeliveries.Parse(value);

    sealed class DiscriminatorTable<T>(string description, Dictionary<string, T> values)
    {
        public T Parse(string value) => values.TryGetValue(value, out var parsed) ? parsed : throw DiscriminatorError(value, description);
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;

namespace Cratis.Screenplay.Semantics.Serialization;

#pragma warning disable IDE0350 // Explicit ref reader parameters keep version-aware parsing delegates clear.
internal static partial class SemanticModelRead
{
    internal static SemanticExpression Expression(ref Utf8JsonReader reader)
    {
        var state = new ExpressionState();
        ExpressionState.Fields.Read(ref reader, state);
        return state.Build();
    }

    internal static SemanticValue Value(ref Utf8JsonReader reader)
    {
        var state = new ValueState();
        ValueState.Fields.Read(ref reader, state);
        return state.Build();
    }

    internal static SemanticValue ValueElement(ref Utf8JsonReader reader)
    {
        Object(ref reader, "semantic array element");
        return Value(ref reader);
    }

    internal static SemanticExpression? NullableExpression(ref Utf8JsonReader reader, string name) =>
        NullableObject(ref reader, name, Expression);

    internal static SemanticValue? NullableValue(ref Utf8JsonReader reader, string name) =>
        NullableObject(ref reader, name, Value);

    sealed class ExpressionState() : ReadState(0)
    {
        public static readonly FieldTable<ExpressionState> Fields = new FieldTable<ExpressionState>("expression")
            .Add("kind", (ref Utf8JsonReader reader, ExpressionState state, string property) => state.Kind = ParseExpressionKind(String(ref reader, property)))
            .Add("value", (ref Utf8JsonReader reader, ExpressionState state, string property) => state.ReadValue(ref reader, property))
            .Add("root", (ref Utf8JsonReader reader, ExpressionState state, string property) => state.Root = ParseExpressionRoot(String(ref reader, property)))
            .Add("source", (ref Utf8JsonReader reader, ExpressionState state, string property) => state.Source = ParseExpressionSource(String(ref reader, property)))
            .Add("target", (ref Utf8JsonReader reader, ExpressionState state, string property) => state.ReadTarget(ref reader, property))
            .Add("contextValue", (ref Utf8JsonReader reader, ExpressionState state, string property) => state.ContextValue = ParseContextValue(String(ref reader, property)))
            .Add("type", (ref Utf8JsonReader reader, ExpressionState state, string property) => state.Type = Nested(ref reader, TypeReference, property))
            .Add("member", (ref Utf8JsonReader reader, ExpressionState state, string property) => state.Member = String(ref reader, property));

        public SemanticExpressionKind? Kind { get; set; }

        public SemanticValue? Content { get; set; }

        public bool ContentRead { get; set; }

        public SemanticExpressionRootKind? Root { get; set; }

        public SemanticExpressionSourceKind? Source { get; set; }

        public SemanticId Target { get; set; }

        public bool TargetRead { get; set; }

        public SemanticEventContextValueKind? ContextValue { get; set; }

        public SemanticTypeReference? Type { get; set; }

        public string? Member { get; set; }

        bool IsValueShape => ContentRead && Content is not null && Root is null && Source is null && !TargetRead && Member is null;

        bool IsResolvedShape => !ContentRead && Root is not null && Source is not null && TargetRead && Target.IsSet && Type is null && ContextValue is null && Member is null;

        bool IsEventContextShape => !ContentRead && Root is null && Source is null && !TargetRead && ContextValue is not null && Type is not null && Member is null;

        public void ReadValue(ref Utf8JsonReader reader, string property)
        {
            ContentRead = true;
            Content = Nested(ref reader, Value, property);
        }

        public void ReadTarget(ref Utf8JsonReader reader, string property)
        {
            TargetRead = true;
            Target = Identifier(ref reader, property);
        }

        public SemanticExpression Build() => Kind switch
        {
            SemanticExpressionKind.Value when IsValueShape => new SemanticValueExpression(Content!),
            SemanticExpressionKind.Resolved when IsResolvedShape => new SemanticResolvedExpression(Root!.Value, Source!.Value, Target),
            SemanticExpressionKind.EventContext when IsEventContextShape => new SemanticEventContextExpression(ContextValue!.Value, Type!),
            SemanticExpressionKind.Refusal when !ContentRead && Root is null && Source is null && !TargetRead && ContextValue is null && Type is not null && Member is not null => new SemanticRefusalExpression(Member, Type),
            _ => throw Malformed("expression", "one exact expression variant")
        };
    }

    sealed class ValueState() : ReadState(0)
    {
        public static readonly FieldTable<ValueState> Fields = new FieldTable<ValueState>("value")
            .Add("kind", (ref Utf8JsonReader reader, ValueState state, string property) => state.Kind = ParseValueKind(String(ref reader, property)))
            .Add("value", (ref Utf8JsonReader reader, ValueState state, string property) => state.ReadScalar(ref reader, property))
            .Add("values", (ref Utf8JsonReader reader, ValueState state, string property) => state.ReadItems(ref reader, property))
            .Add("properties", (ref Utf8JsonReader reader, ValueState state, string property) => state.ReadMembers(ref reader, property));

        public SemanticValueKind? Kind { get; set; }

        public SemanticValue? Scalar { get; set; }

        public bool ScalarRead { get; set; }

        public ImmutableArray<SemanticValue> Items { get; set; }

        public bool ItemsRead { get; set; }

        public ImmutableArray<SemanticPropertyValue> Members { get; set; }

        public bool MembersRead { get; set; }

        bool IsNullShape => !ScalarRead && !ItemsRead && !MembersRead;

        bool IsScalarShape => ScalarRead && Scalar is not null && !ItemsRead && !MembersRead;

        bool IsArrayShape => !ScalarRead && ItemsRead && !MembersRead;

        bool IsCompositeShape => !ScalarRead && !ItemsRead && MembersRead;

        public void ReadScalar(ref Utf8JsonReader reader, string property)
        {
            ScalarRead = true;
            Scalar = Kind switch
            {
                SemanticValueKind.Text => new SemanticTextValue(String(ref reader, property)),
                SemanticValueKind.Number => new SemanticNumberValue(Decimal(ref reader, property)),
                SemanticValueKind.Boolean => new SemanticBooleanValue(Boolean(ref reader, property)),
                _ => throw Malformed("value", "a kind discriminator before its typed value")
            };
        }

        public void ReadItems(ref Utf8JsonReader reader, string property)
        {
            if (Kind != SemanticValueKind.Array)
            {
                throw Malformed("value", "an array kind discriminator before its values");
            }

            ItemsRead = true;
            Items = Array(ref reader, ValueElement, property);
        }

        public void ReadMembers(ref Utf8JsonReader reader, string property)
        {
            if (Kind != SemanticValueKind.Composite)
            {
                throw Malformed("value", "an object kind discriminator before its properties");
            }

            MembersRead = true;
            Members = Array(ref reader, PropertyValue, property);
        }

        public SemanticValue Build() => Kind switch
        {
            SemanticValueKind.Null when IsNullShape => SemanticValue.Null,
            SemanticValueKind.Text or SemanticValueKind.Number or SemanticValueKind.Boolean when IsScalarShape => Scalar!,
            SemanticValueKind.Array when IsArrayShape => new SemanticArrayValue(Items),
            SemanticValueKind.Composite when IsCompositeShape => new SemanticCompositeValue(Members),
            _ => throw Malformed("value", "one exact value variant")
        };
    }
}
#pragma warning restore IDE0350

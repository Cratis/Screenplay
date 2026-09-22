// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json;

namespace Cratis.Screenplay.Syntax.Serialization;

internal sealed class SyntaxMember(PropertyInfo property, ParameterInfo? parameter)
{
    internal PropertyInfo Property { get; } = property;

    internal ParameterInfo? Parameter { get; } = parameter;

    // The discriminator reserves "kind"; some AST records also have a structural Kind member.
    internal string Name { get; } = property.Name == "Kind" ? "syntaxKind" : JsonNamingPolicy.CamelCase.ConvertName(property.Name);

    internal Type Type { get; } = property.PropertyType;

    internal bool Nullable { get; } = new NullabilityInfoContext().Create(property).ReadState == NullabilityState.Nullable;

    internal Type? ElementType { get; } = CollectionElement(property.PropertyType);

    internal bool Required => ElementType is null && !Nullable && Parameter?.HasDefaultValue != true;

    internal object? MissingValue
    {
        get
        {
            if (ElementType is not null)
            {
                return Array.CreateInstance(ElementType, 0);
            }

            return Parameter?.HasDefaultValue == true ? Parameter.DefaultValue : null;
        }
    }

    internal static Type? CollectionElement(Type type) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>) ? type.GetGenericArguments()[0] : null;
}

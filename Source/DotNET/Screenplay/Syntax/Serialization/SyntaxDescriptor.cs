// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.CompilerServices;
using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax.Serialization;

internal sealed class SyntaxDescriptor
{
    internal SyntaxDescriptor(Type type)
    {
        Type = type;
        Constructor = type.GetConstructors().OrderByDescending(constructor => constructor.GetParameters().Length).First();
        Parameters = Constructor.GetParameters();
        Members = [.. type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(property => !IsMetadata(property.Name) && (Parameters.Any(parameter => parameter.Name == property.Name) || IsInit(property)))
            .Select(property => new SyntaxMember(property, Parameters.FirstOrDefault(parameter => parameter.Name == property.Name)))
            .OrderBy(member => member.Name, StringComparer.Ordinal)];
    }

    internal Type Type { get; }

    internal ConstructorInfo Constructor { get; }

    internal ParameterInfo[] Parameters { get; }

    internal SyntaxMember[] Members { get; }

    internal SyntaxNode Create(IReadOnlyDictionary<string, object?> values)
    {
        var arguments = Parameters.Select(parameter => parameter.ParameterType == typeof(SourceLocation)
            ? SourceLocation.Start
            : values[Members.First(member => member.Parameter == parameter).Name]).ToArray();
        var node = (SyntaxNode)Constructor.Invoke(arguments);
        foreach (var member in Members.Where(member => member.Parameter is null))
        {
            member.Property.SetValue(node, values[member.Name]);
        }

        return node;
    }

    static bool IsMetadata(string name) => name == nameof(SyntaxNode.Location) || name == nameof(SliceSyntax.DescriptionLocation) || name == nameof(SliceSyntax.DescriptionRawLength);

    static bool IsInit(PropertyInfo property) => property.SetMethod is { IsPublic: true } setter &&
        setter.ReturnParameter.GetRequiredCustomModifiers().Contains(typeof(IsExternalInit));
}

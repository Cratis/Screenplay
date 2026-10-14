// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Resolves nominal identity domains using the same scoped declarations as authoring validation.
/// </summary>
internal sealed class AuthoringIdentityDomains(ApplicationSyntax application, ConsistencyDeclarations declarations)
{
    readonly (Declaration Declaration, AuthoringIdentityDomainKind Kind)[] _types = [.. application.Concepts.Select(concept => (Named(concept.Name), AuthoringIdentityDomainKind.Concept))
        .Concat((application.Types ?? []).Select(type => (Named(type.Name), AuthoringIdentityDomainKind.Composite)))];

    internal AuthoringIdentityDomain? Resolve(TypeRefSyntax? type, DeclarationScope scope)
    {
        if (type?.IsCollection != false || type.IsOptional) return null;
        if (ConceptSyntax.PrimitiveTypes.Contains(type.Name))
        {
            return _types.Any(entry => entry.Declaration.Scope.Depth == 0 && entry.Declaration.Name == type.Name) ? null : new(AuthoringIdentityDomainKind.Primitive, type.Name, string.Empty);
        }
        var resolution = declarations.ResolveAuthoringDeclaration(type.Name, scope, [.. _types.Select(entry => entry.Declaration)]);
        if (resolution.Resolved is not { } resolved) return null;
        var entry = _types.Single(entry => ReferenceEquals(entry.Declaration, resolved));

        return new(entry.Kind, string.Empty, string.Join('.', resolved.Scope.Segments.Append(resolved.Name)));
    }

    static Declaration Named(string name)
    {
        var segments = name.Split('.');

        return new(segments[^1], new(segments[..^1]));
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Reflection;
using Cratis.Screenplay.Indexing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Comparison;

static class AgentReferenceCoverage
{
    // References and literal/local names both use strings in the syntax model. Unknown string-bearing
    // nodes must not silently become leaves in the dependency graph. These exclusions are deliberately
    // property-specific: a new string property on an excluded node still needs review.
    static readonly HashSet<(Type Node, string Property)> _nonReferences =
    [

        // Declaration identity and authoring-only text, not outgoing declaration references.
        (typeof(CommandSyntax), nameof(CommandSyntax.Name)),
        (typeof(CommandSyntax), nameof(CommandSyntax.Description)),
        (typeof(CommandSyntax), nameof(CommandSyntax.Documentation)),
        (typeof(QuerySyntax), nameof(QuerySyntax.Name)),
        (typeof(QuerySyntax), nameof(QuerySyntax.Description)),
        (typeof(QuerySyntax), nameof(QuerySyntax.Scope)), // A built-in scope modifier.
        (typeof(ReadModelSyntax), nameof(ReadModelSyntax.Name)),
        (typeof(ReadModelSyntax), nameof(ReadModelSyntax.Description)),
        (typeof(ReadModelSyntax), nameof(ReadModelSyntax.Documentation)),
        (typeof(EventSyntax), nameof(EventSyntax.Name)),
        (typeof(EventSyntax), nameof(EventSyntax.Description)),
        (typeof(EventSyntax), nameof(EventSyntax.Documentation)),
        (typeof(EventSyntax), nameof(EventSyntax.Origin)), // Opaque cross-application origin.
        (typeof(EventSyntax), nameof(EventSyntax.Id)), // Previous persisted name, not a local reference.
        (typeof(TypeSyntax), nameof(TypeSyntax.Name)),
        (typeof(TypeSyntax), nameof(TypeSyntax.Description)),
        (typeof(ConceptSyntax), nameof(ConceptSyntax.Name)),
        (typeof(ConceptSyntax), nameof(ConceptSyntax.Type)), // Concepts are based only on primitives or Enum.
        (typeof(ConceptSyntax), nameof(ConceptSyntax.Values)), // Enumeration literals.
        (typeof(ConceptSyntax), nameof(ConceptSyntax.AttributeNames)), // Derived marker names.
        (typeof(ConceptAttributeSyntax), nameof(ConceptAttributeSyntax.Name)),
        (typeof(ConceptAttributeSyntax), nameof(ConceptAttributeSyntax.Reason)),
        (typeof(ConceptAttributeSyntax), nameof(ConceptAttributeSyntax.Scope)),
        (typeof(ConceptAttributeSyntax), nameof(ConceptAttributeSyntax.SpecialCategory)),
        (typeof(PolicySyntax), nameof(PolicySyntax.Name)),
        (typeof(PolicySyntax), nameof(PolicySyntax.Description)),

        // Local member names and paths do not resolve to declarations in ReferenceKinds.
        (typeof(PropertySyntax), nameof(PropertySyntax.Name)),
        (typeof(QueryParameterSyntax), nameof(QueryParameterSyntax.Name)),
        (typeof(PropertyMappingSyntax), nameof(PropertyMappingSyntax.Property)),
        (typeof(PathExpressionSyntax), nameof(PathExpressionSyntax.Path)),
        (typeof(ContextExpressionSyntax), nameof(ContextExpressionSyntax.Path)),
        (typeof(IdentityExpressionSyntax), nameof(IdentityExpressionSyntax.Path)),

        // Caller role/claim values and opaque realization contents are literals, not declarations.
        (typeof(RoleConditionSyntax), nameof(RoleConditionSyntax.Role)),
        (typeof(ClaimConditionSyntax), nameof(ClaimConditionSyntax.Claim)),
        (typeof(CodeBlockSyntax), nameof(CodeBlockSyntax.Language)),
        (typeof(CodeBlockSyntax), nameof(CodeBlockSyntax.Code)),
        (typeof(FileReferenceSyntax), nameof(FileReferenceSyntax.Path))
    ];

    internal static bool Complete(SyntaxNode node, SyntaxNode owner)
    {
        var properties = node.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
        if (!ReferenceKinds.Covers(node, owner) && properties.Any(property => IsUncovered(node, property))) return false;

        // Reflect the children too: a newly introduced node must be checked even if an older walker's
        // dispatch table has not yet learned to descend into it.
        return properties.Select(property => property.GetValue(node)).All(value => Children(value).All(child => Complete(child, owner)));
    }

    static bool IsUncovered(SyntaxNode node, PropertyInfo property)
    {
        if (property.PropertyType != typeof(string) && !typeof(IEnumerable<string>).IsAssignableFrom(property.PropertyType)) return false;
        if (property.GetValue(node) is null) return false;
        if (_nonReferences.Contains((node.GetType(), property.Name))) return false;

        return node is not TypeRefSyntax type || !ConceptSyntax.PrimitiveTypes.Contains(type.Name, StringComparer.Ordinal);
    }

    static IEnumerable<SyntaxNode> Children(object? value) => value switch
    {
        SyntaxNode child => [child],
        IEnumerable children => children.OfType<SyntaxNode>(),
        _ => []
    };
}

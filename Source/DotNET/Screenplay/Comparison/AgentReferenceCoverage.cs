// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Reflection;
using Cratis.Screenplay.Indexing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Comparison;

static class AgentReferenceCoverage
{
    internal static bool Complete(SyntaxNode node, SyntaxNode owner)
    {
        var properties = node.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);

        // Check each member, even when its current value is null. Knowing one reference on a node
        // does not establish what another (possibly newly added) string member means.
        if (properties.Any(property => ReferenceKinds.IsStringMember(property.PropertyType) && !ReferenceKinds.Classifies(node, property.Name)))
        {
            return false;
        }

        // Reflect children unknown to an older syntax walker's dispatch table as well.
        return properties.Select(property => property.GetValue(node)).All(value => Children(value).All(child => Complete(child, owner)));
    }

    static IEnumerable<SyntaxNode> Children(object? value) => value switch
    {
        SyntaxNode child => [child],
        IEnumerable children => children.OfType<SyntaxNode>(),
        _ => []
    };
}

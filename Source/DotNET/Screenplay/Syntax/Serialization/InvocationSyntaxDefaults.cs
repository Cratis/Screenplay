// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;

namespace Cratis.Screenplay.Syntax.Serialization;

/// <summary>
/// Omits absent invocation, compute and entering-body syntax so models without them keep their syntax JSON bytes.
/// </summary>
internal static class InvocationSyntaxDefaults
{
    static readonly Dictionary<Type, HashSet<string>> _members = new()
    {
        [typeof(InvokesSyntax)] = ["branches"],
        [typeof(OperationPhaseSyntax)] = ["invocations"],
        [typeof(WorkflowNodeSyntax)] = ["mappings", "returns", "file", "code", "via"],
        [typeof(StateEntrySyntax)] = ["mappings", "file", "code"]
    };

    internal static bool Omit(SyntaxNode node, string member, object? value) =>
        _members.TryGetValue(node.GetType(), out var omittable) && omittable.Contains(member) &&
        (value is null || (value is IEnumerable items && !items.Cast<object>().Any()));
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using Cratis.Screenplay.Indexing;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Comparison;

static class AgentExposureSignature
{
    internal static string? Read(AuthoringIndex index, AuthoredDeclaration operation)
    {
        var pending = new Stack<AuthoredDeclaration>();
        var visited = new Dictionary<(string Kind, string Address), AuthoredDeclaration>();
        pending.Push(operation);
        while (pending.TryPop(out var declaration))
        {
            if (!visited.TryAdd((declaration.Kind, declaration.Address), declaration)) continue;
            var dependencies = Dependencies(index, declaration);
            if (dependencies is null) return null;
            foreach (var dependency in dependencies) pending.Push(dependency);
        }

        var contract = string.Join('\n', visited.OrderBy(pair => pair.Key.Kind, StringComparer.Ordinal).ThenBy(pair => pair.Key.Address, StringComparer.Ordinal)
            .Select(pair => $"{pair.Key.Kind}:{pair.Key.Address}:{string.Join('\n', pair.Value.Parts.Select(part => SyntaxJson.Serialize(part).GetRawText()))}"));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(contract)));
    }

    static List<AuthoredDeclaration>? Dependencies(AuthoringIndex index, AuthoredDeclaration declaration)
    {
        var dependencies = new List<AuthoredDeclaration>();

        // References belong to the declaration's owner address, including nested properties and gates.
        var references = index.Outgoing(declaration.Owner);
        if (declaration.Kind == "Command" || declaration.Kind == "Query")
        {
            references = references.Concat(declaration.Hierarchy.SelectMany(index.Outgoing).Where(reference => reference.Role == "authorizes"));
        }
        foreach (var reference in references)
        {
            var targets = index.Resolve(reference);
            if (targets.Length != 1) return null;
            dependencies.Add(targets[0]);
        }
        return dependencies;
    }
}

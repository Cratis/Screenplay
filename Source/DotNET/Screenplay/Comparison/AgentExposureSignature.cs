// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using Cratis.Screenplay.Indexing;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Comparison;

static class AgentExposureSignature
{
    internal static string? Read(AuthoringIndex index, AuthoredDeclaration operation) => Read(index, operation, out _);

    internal static string? Read(AuthoringIndex index, AuthoredDeclaration operation, out bool incompleteReferences)
    {
        incompleteReferences = false;
        var pending = new Stack<AuthoredDeclaration>();
        var visited = new Dictionary<(string Kind, string Address), string>();
        pending.Push(operation);
        while (pending.TryPop(out var declaration))
        {
            var key = (declaration.Kind, declaration.Address);
            if (visited.ContainsKey(key)) continue;
            if (declaration.Parts.Exists(part => !AgentReferenceCoverage.Complete(part, declaration.Syntax)))
            {
                incompleteReferences = true;
                return null;
            }
            var authorization = InheritedAuthorization(index, declaration);
            if (authorization is null) return null;
            visited.Add(key, $"{string.Join('\n', declaration.Parts.Select(part => SyntaxJson.Serialize(part).GetRawText()))}\n{authorization}");
            var dependencies = Dependencies(index, declaration);
            if (dependencies is null) return null;
            foreach (var dependency in dependencies) pending.Push(dependency);
        }

        var contract = string.Join('\n', visited.OrderBy(pair => pair.Key.Kind, StringComparer.Ordinal).ThenBy(pair => pair.Key.Address, StringComparer.Ordinal)
            .Select(pair => $"{pair.Key.Kind}:{pair.Key.Address}:{pair.Value}"));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(contract)));
    }

    static string? InheritedAuthorization(AuthoringIndex index, AuthoredDeclaration declaration)
    {
        var scopes = new List<string>();
        foreach (var owner in declaration.Hierarchy)
        {
            var matches = index.Declarations.Where(candidate => candidate.Kind == owner.Kind && candidate.Address == owner.Address).ToArray();
            if (matches.Length != 1) return null;
            foreach (var part in matches[0].Parts)
            {
                // Read the structural member on every owner, without enumerating authorization-bearing scope kinds.
                // Do not hash the whole owner: unrelated sibling declarations are not exposed contracts.
                if (SyntaxJson.Serialize(part).TryGetProperty("authorize", out var authorize))
                {
                    scopes.Add($"{owner.Kind}:{owner.Address}:{authorize.GetRawText()}");
                }
            }
        }
        return string.Join('\n', scopes);
    }

    static List<AuthoredDeclaration>? Dependencies(AuthoringIndex index, AuthoredDeclaration declaration)
    {
        var dependencies = new List<AuthoredDeclaration>();

        // References belong to the declaration's owner address, including nested properties and gates.
        var references = index.Outgoing(declaration.Owner)
            .Concat(declaration.Hierarchy.SelectMany(index.Outgoing).Where(reference => reference.Role == "authorizes"));
        foreach (var reference in references)
        {
            var targets = index.Resolve(reference);
            if (targets.Length != 1) return null;
            dependencies.Add(targets[0]);
        }
        return dependencies;
    }
}

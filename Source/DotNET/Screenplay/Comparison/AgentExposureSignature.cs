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
            foreach (var reference in index.Outgoing(declaration.Owner))
            {
                var targets = index.Resolve(reference);
                if (targets.Length != 1) return null;
                pending.Push(targets[0]);
            }
        }

        var contract = string.Join('\n', visited.OrderBy(pair => pair.Key.Kind, StringComparer.Ordinal).ThenBy(pair => pair.Key.Address, StringComparer.Ordinal)
            .Select(pair => $"{pair.Key.Kind}:{pair.Key.Address}:{string.Join('\n', pair.Value.Parts.Select(part => SyntaxJson.Serialize(part).GetRawText()))}"));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(contract)));
    }
}

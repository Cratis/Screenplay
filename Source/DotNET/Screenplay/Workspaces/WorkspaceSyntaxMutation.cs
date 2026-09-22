// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Workspaces;

static class WorkspaceSyntaxMutation
{
    internal static JsonNode Json(SyntaxNode syntax) => JsonNode.Parse(SyntaxJson.Serialize(syntax).GetRawText())!;

    internal static JsonNode At(JsonNode root, string path)
    {
        var current = root;
        foreach (var segment in path.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            current = current is JsonArray array ? array[int.Parse(segment, System.Globalization.CultureInfo.InvariantCulture)]! : current[segment]!;
        }

        return current;
    }

    internal static void Set(JsonNode root, string path, string value)
    {
        var separator = path.LastIndexOf('/');
        var parent = At(root, path[..separator]);
        var member = path[(separator + 1)..];
        if (parent is JsonArray array)
        {
            array[int.Parse(member, System.Globalization.CultureInfo.InvariantCulture)] = value;
        }
        else
        {
            parent[member] = value;
        }
    }

    internal static ApplicationSyntax Syntax(JsonNode root) => (ApplicationSyntax)SyntaxJson.Deserialize(JsonSerializer.SerializeToElement(root));
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Text.Json.Nodes;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Workspaces;

internal sealed partial class WorkspaceAstEdits
{
    /// <summary>
    /// Carries locations across replacement JSON, without adding source metadata to the typed JSON contract.
    /// Unchanged JSON objects retain their own locations in the map. For a replaced subtree, match equal
    /// siblings first, then same-kind named siblings, and only use indexes when collection lengths agree.
    /// </summary>
    void CarrySourceLocations(JsonNode original, JsonNode replacement)
    {
        if (_sourceLocations.TryGetValue(original, out var location))
        {
            _sourceLocations[replacement] = location;
        }

        if (original is JsonObject oldObject && replacement is JsonObject newObject)
        {
            foreach (var (name, child) in newObject)
            {
                if (child is not null && oldObject[name] is { } previous)
                {
                    CarrySourceLocations(previous, child);
                }
            }
        }
        else if (original is JsonArray oldArray && replacement is JsonArray newArray)
        {
            var matched = new HashSet<int>();
            for (var position = 0; position < newArray.Count; position++)
            {
                if (newArray[position] is not { } child)
                {
                    continue;
                }

                var match = Enumerable.Range(0, oldArray.Count).FirstOrDefault(
                    candidate => !matched.Contains(candidate) && JsonNode.DeepEquals(oldArray[candidate], child),
                    -1);
                if (match < 0 && child is JsonObject named && named["name"] is not null)
                {
                    match = Enumerable.Range(0, oldArray.Count).FirstOrDefault(
                        candidate => !matched.Contains(candidate) && oldArray[candidate] is JsonObject previous &&
                            JsonNode.DeepEquals(previous["kind"], named["kind"]) &&
                            JsonNode.DeepEquals(previous["name"], named["name"]),
                        -1);
                }

                if (match < 0 && oldArray.Count == newArray.Count && position < oldArray.Count &&
                    !matched.Contains(position) && oldArray[position] is JsonObject previousAtIndex &&
                    child is JsonObject currentAtIndex && JsonNode.DeepEquals(previousAtIndex["kind"], currentAtIndex["kind"]))
                {
                    match = position;
                }

                if (match >= 0 && oldArray[match] is { } previousChild)
                {
                    matched.Add(match);
                    CarrySourceLocations(previousChild, child);
                }
            }
        }
    }

    ApplicationSyntax RestoreSourceLocations(JsonNode json, ApplicationSyntax syntax)
    {
        Restore(json, syntax);
        return syntax;
    }

    void Restore(JsonNode json, SyntaxNode node)
    {
        if (_sourceLocations.TryGetValue(json, out var location))
        {
            // The codec has already admitted the node. Set only server-owned metadata on that decoded tree.
            typeof(SyntaxNode).GetProperty(nameof(SyntaxNode.Location))!.SetValue(node, location);
        }

        var descriptor = SyntaxKinds.All.Single(kind => kind.Type == node.GetType());
        foreach (var member in descriptor.Members)
        {
            if (json[member.Name] is not { } childJson)
            {
                continue;
            }

            var value = member.Property.GetValue(node);
            if (value is SyntaxNode child)
            {
                Restore(childJson, child);
            }
            else if (value is IEnumerable children and not string && childJson is JsonArray array)
            {
                var position = 0;
                foreach (var item in children)
                {
                    if (item is SyntaxNode childNode && array[position] is { } element)
                    {
                        Restore(element, childNode);
                    }

                    position++;
                }
            }
        }
    }
}

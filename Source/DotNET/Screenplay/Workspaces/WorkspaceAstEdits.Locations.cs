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
    static bool IsRule(JsonNode? node) => node is JsonObject rule && rule["kind"]?.GetValue<string>() == "ValidationRuleSyntax";

    static bool IsPending(SyntaxNode node) => node is ValidationRuleSyntax { Implementation: not null, File: null, Code: null };
    static bool PendingRule(JsonNode? node) => IsRule(node) && node!["implementation"] is not null && node["file"] is null && node["code"] is null;
    static bool BareRule(JsonNode? node) => IsRule(node) && node!["implementation"] is null && node["file"] is null && node["code"] is null;
    static bool SameRuleHeader(JsonNode previous, JsonNode current) => JsonNode.DeepEquals(previous["property"], current["property"]) &&
        JsonNode.DeepEquals(previous["rule"], current["rule"]) && JsonNode.DeepEquals(previous["value"], current["value"]);

    static bool AmbiguousBareRule(JsonArray originals, JsonNode? candidate) => BareRule(candidate) &&
        originals.Any(previous => PendingRule(previous) && SameRuleHeader(previous!, candidate!));

    static bool CompetingBareRule(JsonArray candidates, JsonNode? previous) => PendingRule(previous) &&
        candidates.Any(current => BareRule(current) && SameRuleHeader(previous!, current!));

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

        if (_sourceComments.TryGetValue(original, out var comments))
        {
            _sourceComments[replacement] = comments;
        }

        if (_directiveLocations.TryGetValue(original, out var directives))
        {
            _directiveLocations[replacement] = directives;
        }

        if (_parsedAutoMapModes.TryGetValue(original, out var mode))
        {
            _parsedAutoMapModes[replacement] = mode;
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

    /// <summary>
    /// Carries rule-occurrence lineage (pending proof) only. Never touches printer metadata.
    /// </summary>
    void CarryRuleLineage(JsonNode original, JsonNode replacement, bool ruleLineage)
    {
        CarryNodeMetadata(original, replacement, ruleLineage);
        if (original is JsonObject oldObject && replacement is JsonObject newObject)
        {
            foreach (var (name, child) in newObject)
            {
                if (child is not null && oldObject[name] is { } previous)
                {
                    CarryRuleLineage(previous, child, ruleLineage);
                }
            }
        }
        else if (original is JsonArray oldArray && replacement is JsonArray newArray)
        {
            var matched = new HashSet<int>();
            var carried = new HashSet<int>();

            // Equal JSON is not occurrence evidence when copied pending guidance competes with a
            // surviving bare original. Actual reused node/member lineage is recorded separately.
            Match((previous, current) => JsonNode.DeepEquals(previous, current) && !AmbiguousBareRule(oldArray, current));
            Match((previous, current) => previous is JsonObject prior && current is JsonObject named && named["name"] is not null &&
                JsonNode.DeepEquals(prior["kind"], named["kind"]) && JsonNode.DeepEquals(prior["name"], named["name"]));

            // Coordinates and equal collection lengths are printer hints, not rule-occurrence identity.
            for (var position = 0; position < newArray.Count && oldArray.Count == newArray.Count; position++)
            {
                if (ruleLineage && !carried.Contains(position) && !matched.Contains(position) && oldArray[position] is JsonObject previous &&
                    newArray[position] is JsonObject current && JsonNode.DeepEquals(previous["kind"], current["kind"]) &&
                    (!IsRule(current) || (!CompetingBareRule(newArray, previous) && !AmbiguousBareRule(oldArray, current))))
                {
                    CarryRuleLineage(previous, current, !IsRule(current));
                }
            }

            void Match(Func<JsonNode?, JsonNode?, bool> equal)
            {
                var matches = Enumerable.Range(0, newArray.Count).Where(position => !carried.Contains(position) && newArray[position] is not null)
                    .ToDictionary(position => position, position => Enumerable.Range(0, oldArray.Count).Where(candidate => !matched.Contains(candidate) && !CompetingBareRule(newArray, oldArray[candidate]) && equal(oldArray[candidate], newArray[position])).ToArray());
                foreach (var (position, candidates) in matches)
                {
                    if (candidates.Length == 1 && matches.Count(pair => pair.Value.Contains(candidates[0])) == 1)
                    {
                        matched.Add(candidates[0]);
                        carried.Add(position);
                        CarryRuleLineage(oldArray[candidates[0]]!, newArray[position]!, ruleLineage);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Carries metadata across a validated unchanged subtree by exact position. A move keeps its whole subtree
    /// unchanged, so each descendant corresponds to the descendant at the same path; this is not ambiguous
    /// replacement matching, which cannot tell structurally identical siblings apart.
    /// </summary>
    void CarryExactCorrespondence(JsonNode original, JsonNode moved)
    {
        CarryNodeMetadata(original, moved, ruleLineage: true);
        if (original is JsonObject oldObject && moved is JsonObject newObject)
        {
            foreach (var (name, child) in newObject.Where(pair => pair.Value is not null && oldObject[pair.Key] is not null).ToArray())
            {
                CarryExactCorrespondence(oldObject[name]!, child!);
            }
        }
        else if (original is JsonArray oldArray && moved is JsonArray newArray && oldArray.Count == newArray.Count)
        {
            for (var position = 0; position < newArray.Count; position++)
            {
                if (oldArray[position] is { } previous && newArray[position] is { } current)
                {
                    CarryExactCorrespondence(previous, current);
                }
            }
        }
    }

    void CarryNodeMetadata(JsonNode original, JsonNode replacement, bool ruleLineage)
    {
        // Lineage only: printer metadata follows the unchanged merge-base matching in CarrySourceLocations.
        if (ruleLineage && _ruleOrigins.TryGetValue(original, out var origin))
        {
            _ruleOrigins[replacement] = origin;
        }
    }

    // A reused typed rule or its actual value child establishes occurrence lineage. A coincident source
    // coordinate cannot override a structural match (or establish lineage for a newly parsed document).
    void CarryReplacementMetadata(SyntaxNode node, JsonNode json)
    {
        if (node is ValidationRuleSyntax rule)
        {
            var origin = _expectedOrigins.GetValueOrDefault(rule);
            if (origin is null && rule.Value is { } value && _expectedOrigins.GetValueOrDefault(value)?.Parent is { } parent)
            {
                origin = index.Find(parent);
            }

            if (origin?.Node is ValidationRuleSyntax)
            {
                // Direct replacement-target lineage is authoritative; a reused node with different
                // provenance must not overwrite it, and a conflict is refused rather than discharged.
                if (_ruleOrigins.TryGetValue(json, out var existing) && existing.Handle != origin.Handle && (IsPending(existing.Node) || IsPending(origin.Node)))
                {
                    throw new InvalidWorkspaceAuthoring("A replacement reuses a validation rule from a different original occurrence than the rule it replaces, so pending rule intent cannot be conserved.");
                }

                _ruleOrigins[json] = origin;
            }
        }

        if (node.SourceComments.Length > 0)
        {
            _sourceComments[json] = node.SourceComments;
        }

        if (node.DirectiveLocations.Count > 0)
        {
            _directiveLocations[json] = node.DirectiveLocations;
        }

        if (node.ParsedAutoMapMode is { } mode)
        {
            _parsedAutoMapModes[json] = mode;
        }

        if (node.Location.Line > 1)
        {
            _sourceLocations[json] = node.Location;
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
                CarryReplacementMetadata(child, childJson);
            }
            else if (value is IEnumerable children and not string && childJson is JsonArray array)
            {
                var position = 0;
                foreach (var item in children)
                {
                    if (item is SyntaxNode nested && array[position] is { } element)
                    {
                        CarryReplacementMetadata(nested, element);
                    }

                    position++;
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

        if (_sourceComments.TryGetValue(json, out var comments))
        {
            typeof(SyntaxNode).GetProperty(nameof(SyntaxNode.SourceComments))!.SetValue(node, comments);
        }

        if (_directiveLocations.TryGetValue(json, out var directives))
        {
            typeof(SyntaxNode).GetProperty(nameof(SyntaxNode.DirectiveLocations))!.SetValue(node, directives);
        }

        if (_parsedAutoMapModes.TryGetValue(json, out var mode))
        {
            typeof(SyntaxNode).GetProperty(nameof(SyntaxNode.ParsedAutoMapMode))!.SetValue(node, mode);
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

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Text.Json.Nodes;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Workspaces;

/// <summary>
/// Proves the content of a PLAY0516 repair without equating missing executable models.
/// </summary>
internal static class WorkspaceTimelineContentProof
{
    static readonly Dictionary<Type, SyntaxMember[]> _children = SyntaxKinds.All.ToDictionary(
        kind => kind.Type,
        kind => kind.Members.Where(member => typeof(SyntaxNode).IsAssignableFrom(member.Type) ||
            (member.ElementType is not null && typeof(SyntaxNode).IsAssignableFrom(member.ElementType))).ToArray());

    internal static bool Preserves(ScreenplayWorkspace before, ScreenplayWorkspace after, IReadOnlyList<TimelineImportPin> pins)
    {
        if (before.Compilation.Value is not null && after.Compilation.Value is not null)
        {
            return WorkspaceRepairVerification.SameModel(before, after);
        }
        if (before.Compilation.Value is not null || after.Compilation.Value is not null)
        {
            return false;
        }

        return SameSourceModuloTimelinePermutation(before, after, pins) && SameAdmissionDiagnostics(before.Compilation.Diagnostics, after.Compilation.Diagnostics);
    }

    internal static bool SameSourceModuloTimelinePermutation(ScreenplayWorkspace before, ScreenplayWorkspace after, IReadOnlyList<TimelineImportPin> pins)
    {
        var original = WorkspaceTimelineRepairs.Timeline(before);
        var candidate = WorkspaceTimelineRepairs.Timeline(after);
        if (!original.SourceValid || !candidate.SourceValid || original.Application is null || candidate.Application is null ||
            !PinsAlreadyPlaced(original, candidate, pins))
        {
            return false;
        }

        var left = JsonNode.Parse(SyntaxJson.Serialize(original.Application).GetRawText())!.AsObject();
        var right = JsonNode.Parse(SyntaxJson.Serialize(candidate.Application).GetRawText())!.AsObject();
        var remaining = pins.ToList();
        Normalize(original.Application, left, [], []);
        Normalize(candidate.Application, right, [], remaining);

        return remaining.Count == 0 && JsonNode.DeepEquals(left, right);
    }

    internal static bool SameAdmissionDiagnostics(IEnumerable<Diagnostic> before, IEnumerable<Diagnostic> after) =>
        AdmissionDiagnostics(before).SequenceEqual(AdmissionDiagnostics(after));

    static IEnumerable<(string Code, DiagnosticSeverity Severity, int Count)> AdmissionDiagnostics(IEnumerable<Diagnostic> diagnostics) => diagnostics
        .Where(diagnostic => diagnostic.Code != DiagnosticCodes.EventFromLaterSlice && diagnostic.Code != DiagnosticCodes.TimelineCycleGroup)
        .GroupBy(diagnostic => (diagnostic.Code, diagnostic.Severity))
        .Select(group => (group.Key.Code, group.Key.Severity, group.Count()))
        .OrderBy(group => group.Code, StringComparer.Ordinal).ThenBy(group => group.Severity);

    static bool PinsAlreadyPlaced(AuthoredTimeline before, AuthoredTimeline after, IReadOnlyList<TimelineImportPin> pins) => pins.All(pin =>
    {
        if (pin.Import.Pattern.IndexOfAny(['*', '?', '[', ']', '{', '}']) >= 0) return false;
        var path = PlayGlob.Resolve(pin.DocumentPath, pin.Import.Pattern);
        var original = before.Documents.SingleOrDefault(document => document.Path == path);
        var candidate = after.Documents.SingleOrDefault(document => document.Path == path);

        return original is { IsPlacementResolved: true } && candidate is { IsPlacementResolved: true } &&
            original.Placement.Equals(pin.Placement) && candidate.Placement.Equals(pin.Placement);
    });

    static void Normalize(SyntaxNode syntax, JsonObject node, string[] outer, List<TimelineImportPin> pins)
    {
        var container = syntax is ApplicationSyntax or ModuleSyntax or FeatureSyntax;
        var scope = syntax switch
        {
            ModuleSyntax module => [.. outer, module.Name],
            FeatureSyntax feature => [.. outer, feature.Name],
            _ => outer
        };

        // Traverse codec-declared syntax children only. Literal data can contain a 'kind' property,
        // but it is data, never a declaration or permission to reorder one of its arrays.
        foreach (var member in _children[syntax.GetType()])
        {
            var value = member.Property.GetValue(syntax);
            if (value is SyntaxNode child && node[member.Name] is JsonObject childJson)
            {
                Normalize(child, childJson, scope, pins);
            }
            else if (value is IEnumerable children && node[member.Name] is JsonArray array)
            {
                var position = 0;
                foreach (var element in children.OfType<SyntaxNode>())
                {
                    Normalize(element, array[position++]!.AsObject(), scope, pins);
                }
            }
        }

        if (container && node["fileImports"] is JsonArray imports)
        {
            foreach (var pin in pins.Where(pin => pin.Placement.Scope.SequenceEqual(scope)).ToArray())
            {
                var expected = JsonNode.Parse(SyntaxJson.Serialize(pin.Import).GetRawText());
                var position = Enumerable.Range(0, imports.Count).FirstOrDefault(position => JsonNode.DeepEquals(imports[position], expected), -1);
                if (position < 0) continue;
                imports.RemoveAt(position);
                pins.Remove(pin);
            }
        }

        foreach (var (member, value) in node)
        {
            // Only the timeline's named sibling collections and imports may permute. In particular,
            // events, commands, projection blocks, specifications and every scalar array retain order.
            if (!container || member is not ("modules" or "features" or "slices" or "fileImports") || value is not JsonArray children) continue;
            var ordered = children.OrderBy(element => element?.ToJsonString(), StringComparer.Ordinal).Select(element => element?.DeepClone()).ToArray();
            children.Clear();
            foreach (var element in ordered) children.Add(element);
        }
    }
}

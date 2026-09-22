// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces;

internal sealed partial class WorkspaceAstEdits
{
    internal void ValidateFragmentRenames(IEnumerable<ReplaceWorkspaceSyntaxDocument> replacements)
    {
        var edits = _edits.Concat(replacements.Select(replacement => new Edit(
            index.Entries.SingleOrDefault(entry => entry.Handle.Document == replacement.Document && entry.Parent is null),
            null,
            null,
            replacement.Syntax))).ToArray();
        var sharedHeaders = index.Entries.Where(entry => entry.Address?.Kind is SemanticKind.Module or SemanticKind.Feature)
            .GroupBy(entry => entry.Address!).Where(group => group.Count() > 1);
        foreach (var group in sharedHeaders)
        {
            var names = group.Select(entry => RenamedHeader(entry, edits)).ToArray();
            if (names.Any(name => name is not null) && (names.Any(name => name is null) || names.Distinct(StringComparer.Ordinal).Count() != 1))
            {
                throw new InvalidWorkspaceAuthoring($"Logical {group.Key.Kind} '{group.Key.Name}' has multiple source fragments. Rename every header occurrence consistently in this batch; one occurrence cannot silently rename the logical declaration.");
            }
        }
    }

    static string? RenamedHeader(WorkspaceSyntaxEntry entry, IEnumerable<Edit> edits)
    {
        var replacement = edits.SingleOrDefault(edit => edit.Target is not null && Contains(edit.Target.Handle, entry.Handle));
        if (replacement?.Value is null || replacement.Destination is not null)
        {
            return null;
        }

        var current = ToJson(replacement.Value);
        var relativePath = entry.Handle.Path[replacement.Target!.Handle.Path.Length..];
        foreach (var segment in relativePath.Split('/').Skip(1))
        {
            if (current is JsonArray array && int.TryParse(segment, out var position) && position >= 0 && position < array.Count)
            {
                current = array[position];
            }
            else if (current is JsonObject owner && owner.TryGetPropertyValue(segment, out var child))
            {
                current = child;
            }
            else
            {
                return null;
            }
        }

        var previousName = entry.Node switch
        {
            ModuleSyntax module => module.Name,
            FeatureSyntax feature => feature.Name,
            _ => null
        };
        var newName = current is JsonObject node && node["kind"]?.GetValue<string>() == entry.Kind
            ? node["name"]?.GetValue<string>()
            : null;
        return newName is not null && newName != previousName ? newName : null;
    }
}

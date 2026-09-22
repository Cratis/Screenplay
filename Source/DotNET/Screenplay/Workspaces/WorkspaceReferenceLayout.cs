// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Files;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Workspaces;

static class WorkspaceReferenceLayout
{
    internal static bool Equivalent(ScreenplayWorkspace before, ScreenplayWorkspace after)
    {
        var previous = Merge(before);
        var current = Merge(after);
        return previous is not null && current is not null && SyntaxJson.StructurallyEqual(Normalize(previous), Normalize(current));
    }

    static ApplicationSyntax? Merge(ScreenplayWorkspace workspace)
    {
        var parsed = workspace.Documents.OrderBy(document => document.Path.Value, StringComparer.Ordinal)
            .Select(document => new ScreenplayCompiler().Parse(document.Text, document.Path.Value)).ToArray();
        if (parsed.Any(document => !document.Success))
        {
            return null;
        }

        var merged = PlayFolderMerge.Merge(parsed);
        return merged.Success ? merged.Value : null;
    }

    // Folder layouts order these named containers by path. Their sibling ordering is not a binding
    // dimension; all other collection order and every structural member remain part of this proof.
    static ApplicationSyntax Normalize(ApplicationSyntax application) => application with
    {
        Modules = [.. application.Modules.OrderBy(module => module.Name, StringComparer.Ordinal).Select(module => module with
        {
            Features = [.. module.Features.OrderBy(feature => feature.Name, StringComparer.Ordinal).Select(Normalize)]
        })]
    };

    static FeatureSyntax Normalize(FeatureSyntax feature) => feature with
    {
        Features = [.. feature.Features.OrderBy(child => child.Name, StringComparer.Ordinal).Select(Normalize)],
        Slices = [.. feature.Slices.OrderBy(slice => slice.Name, StringComparer.Ordinal)]
    };
}

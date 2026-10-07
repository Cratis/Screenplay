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
        var texts = workspace.Documents.OrderBy(document => document.Path.Value, StringComparer.Ordinal).ToDictionary(document => document.Path.Value, document => document.Text, StringComparer.Ordinal);
        var (_, merged) = PlayApplicationAssembly.Compile(new ScreenplayCompiler(), texts.Keys, new InMemoryPlayDocumentSource(texts));
        return merged.Success ? merged.Value : null;
    }

    // This is a binding-safety relaxation for layout changes, not an authored-order guard.
    // Sibling order is presentation-only; expand-layout guards it separately against the ordering root.
    // File imports are resolved composition edges, not semantic references. All other collection order
    // and every structural member remain part of this proof.
    static ApplicationSyntax Normalize(ApplicationSyntax application) => application with
    {
        FileImports = [],
        Modules = [.. application.Modules.OrderBy(module => module.Name, StringComparer.Ordinal).Select(module => module with
        {
            FileImports = [],
            Features = [.. module.Features.OrderBy(feature => feature.Name, StringComparer.Ordinal).Select(Normalize)]
        })]
    };

    static FeatureSyntax Normalize(FeatureSyntax feature) => feature with
    {
        FileImports = [],
        Features = [.. feature.Features.OrderBy(child => child.Name, StringComparer.Ordinal).Select(Normalize)],
        Slices = [.. feature.Slices.OrderBy(slice => slice.Name, StringComparer.Ordinal)]
    };
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp;

static class McpRuleBodies
{
    internal static IEnumerable<SemanticImplementationBody> Resolve(ScreenplayWorkspace workspace)
    {
        var inline = WorkspaceSyntaxIndex.Create(workspace).Entries.Select(entry => entry.Node).OfType<CodeBlockSyntax>()
            .Select(block => block.Code).Distinct(StringComparer.Ordinal)
            .ToDictionary(content => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content))).ToLowerInvariant(), StringComparer.Ordinal);
        foreach (var requirement in workspace.Compilation.ImplementationRequirements.Where(requirement => requirement.Role is SemanticImplementationRole.RulePredicate or SemanticImplementationRole.CommandValidation or SemanticImplementationRole.ConceptValidation or SemanticImplementationRole.ReducerTransition))
        {
            var content = inline.GetValueOrDefault(requirement.ContentHash);
            if (requirement.File is { } file)
            {
                content = AttachmentFiles.TryNormalize(file, out var path, out _) ? workspace.AttachmentContents.GetValueOrDefault(path) : null;
            }
            if (content is not null) yield return new(requirement, content);
        }
    }
}

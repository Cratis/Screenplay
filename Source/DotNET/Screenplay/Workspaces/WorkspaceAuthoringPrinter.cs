// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Workspaces;

static class WorkspaceAuthoringPrinter
{
    internal static WorkspaceDocument Print(
        DocumentId id,
        string key,
        PortablePlayPath path,
        WorkspaceTextEncoding encoding,
        ApplicationSyntax intended,
        WorkspaceAuthoringFormatting formatting,
        ImmutableArray<Diagnostic>.Builder diagnostics,
        WorkspaceDocument? original = null)
    {
        if (formatting == WorkspaceAuthoringFormatting.PreserveTrivia && original is not null)
        {
            return WorkspaceTriviaPrinter.Print(original, intended);
        }

        if (formatting != WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments)
        {
            throw new InvalidWorkspaceAuthoring("Typed syntax edits require explicit CanonicalizeTouchedDocuments permission; untouched documents remain exact.");
        }

        if (!Enum.IsDefined(encoding))
        {
            throw new InvalidWorkspaceAuthoring("The requested document encoding is unknown.");
        }

        // Round-trip through the codec first: reject malformed runtime trees and server-managed source metadata.
        var checkedSyntax = SyntaxJson.Deserialize(SyntaxJson.Serialize(intended)) as ApplicationSyntax
            ?? throw new InvalidWorkspaceAuthoring("A typed document requires an ApplicationSyntax root.");
        var text = new ScreenplayPrinter().Print(checkedSyntax);
        var parsed = new ScreenplayCompiler().Parse(text, path.Value);
        diagnostics.AddRange(parsed.Diagnostics);
        if (!parsed.Success || parsed.Value is null || !SyntaxJson.StructurallyEqual(checkedSyntax, parsed.Value))
        {
            throw new InvalidWorkspaceAuthoring($"Printing '{path}' did not reparse to the intended typed AST. A printer omission, unrepresentable value, or malformed syntax cannot be committed.");
        }

        diagnostics.Add(Diagnostic.Warning(
            DiagnosticCodes.AuthoringSourceNormalization,
            $"'{path}' was canonically printed. Existing comments and whitespace trivia are not retained; its UTF-8 BOM policy is preserved. Untouched documents remain byte-exact.",
            SourceLocation.Start.In(path.Value)));
        var utf8 = new UTF8Encoding(false, true);
        var bytes = encoding == WorkspaceTextEncoding.Utf8WithBom
            ? [0xef, 0xbb, 0xbf, .. utf8.GetBytes(text)]
            : utf8.GetBytes(text);
        return WorkspaceDocument.Create(id, key, path, bytes);
    }
}

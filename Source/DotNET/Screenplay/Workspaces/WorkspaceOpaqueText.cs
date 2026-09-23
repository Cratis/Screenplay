// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Captures;

namespace Cratis.Screenplay.Workspaces;

/// <summary>
/// Extracts the verbatim text of syntax whose references the workspace cannot bind, and finds declaration names in it.
/// </summary>
/// <remarks>
/// Opaque text is not Screenplay source: code, JSON, template literals and qualified names can name a declaration from
/// inside a string literal or an object key. Every maximal identifier run is compared, including runs inside quotes,
/// because refusing a safe rename costs one message while accepting an unsafe one silently breaks a model.
/// </remarks>
static class WorkspaceOpaqueText
{
    /// <summary>
    /// Gets the opaque text a syntax node carries.
    /// </summary>
    /// <param name="node">The syntax node.</param>
    /// <returns>The opaque text runs, or <c>null</c> when every reference in the node is structured.</returns>
    internal static IEnumerable<string>? Texts(SyntaxNode node) => node switch
    {
        RawExpressionSyntax raw => [raw.Text],
        CodeBlockSyntax code => [code.Language, code.Code],
        ImportSyntax import => [import.QualifiedName],
        FileReferenceSyntax file => [file.Path],
        CaptureSourceSyntax source => [source.Kind, .. source.Settings.SelectMany(setting => new[] { setting.Name, setting.Value })],
        CaptureWhenSyntax { Expression: not null } trigger => [trigger.Expression, trigger.FromValue ?? string.Empty, trigger.ToValue ?? string.Empty],
        FormFieldSyntax { ComposeUsing: not null } field => [field.ComposeUsing, field.From ?? string.Empty, field.Label ?? string.Empty],
        _ => null
    };

    /// <summary>
    /// Finds the first of the given names that occurs in a node's opaque text as a whole identifier.
    /// </summary>
    /// <param name="node">The syntax node.</param>
    /// <param name="names">The candidate declaration names, in priority order.</param>
    /// <returns>The matched name, or <c>null</c> when the node is not opaque or names none of them.</returns>
    internal static string? Named(SyntaxNode node, params string[] names)
    {
        if (Texts(node) is not { } texts)
        {
            return null;
        }

        var identifiers = texts.SelectMany(Identifiers).ToHashSet(StringComparer.Ordinal);
        return names.FirstOrDefault(identifiers.Contains);
    }

    /// <summary>
    /// Splits text into maximal runs of letters, digits and underscores, ignoring quoting.
    /// </summary>
    /// <param name="text">The opaque text.</param>
    /// <returns>Every identifier-like run.</returns>
    internal static IEnumerable<string> Identifiers(string text)
    {
        var start = -1;
        for (var position = 0; position <= text.Length; position++)
        {
            var identifier = position < text.Length && (char.IsLetterOrDigit(text[position]) || text[position] == '_');
            if (identifier && start < 0)
            {
                start = position;
            }
            else if (!identifier && start >= 0)
            {
                yield return text[start..position];
                start = -1;
            }
        }
    }
}

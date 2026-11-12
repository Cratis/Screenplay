// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace Cratis.Screenplay.Extensions;

/// <summary>
/// Checks a component placeholder: static HTML that design tools draw in a sandboxed frame with scripts off.
/// </summary>
public static partial class ExtensionPlaceholder
{
    /// <summary>
    /// Lists why a placeholder is not allowed: script, an inline event handler or an external reference.
    /// </summary>
    /// <param name="html">The placeholder file text.</param>
    /// <returns>One message per violation; empty when it is static and self-contained.</returns>
    public static IReadOnlyList<string> Violations(string html)
    {
        var found = new List<string>();
        if (ScriptRegex().IsMatch(html)) found.Add("contains a script element");
        if (HandlerRegex().IsMatch(html)) found.Add("contains an inline event handler");
        if (JavascriptUrlRegex().IsMatch(html)) found.Add("contains a javascript: URL");
        if (ExternalRegex().IsMatch(html)) found.Add("references an external resource; inline styles and use data: URIs for images");
        return found;
    }

    [GeneratedRegex(@"<\s*(script|iframe|object|embed)\b", RegexOptions.IgnoreCase | RegexOptions.NonBacktracking)]
    private static partial Regex ScriptRegex();

    [GeneratedRegex(@"\son[a-z]+\s*=", RegexOptions.IgnoreCase | RegexOptions.NonBacktracking)]
    private static partial Regex HandlerRegex();

    [GeneratedRegex(@"javascript\s*:", RegexOptions.IgnoreCase | RegexOptions.NonBacktracking)]
    private static partial Regex JavascriptUrlRegex();

    [GeneratedRegex(@"(?:src|href|action|poster)\s*=\s*[""']?\s*(?:https?:)?//|url\(\s*[""']?\s*(?:https?:)?//|@import|<\s*link\b", RegexOptions.IgnoreCase | RegexOptions.NonBacktracking)]
    private static partial Regex ExternalRegex();
}

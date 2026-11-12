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

    /// <summary>
    /// Reads a placeholder the restored package holds, when it lies inside the package folder and is safe to draw.
    /// </summary>
    /// <param name="folder">The folder that holds the restored package.</param>
    /// <param name="path">The package-relative path the contract declares.</param>
    /// <returns>The static HTML, or <see langword="null"/> when the package does not hold it or it is not static.</returns>
    public static string? TryRead(string folder, string path)
    {
        const int maximumBytes = 256 * 1024;
        var root = Path.GetFullPath(folder);
        var file = Path.GetFullPath(Path.Combine(root, path));
        if (Path.IsPathRooted(path) || !file.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal) || !File.Exists(file)) return null;
        if (new FileInfo(file).Length > maximumBytes) return null;
        var html = File.ReadAllText(file);
        return Violations(html).Count == 0 ? html : null;
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

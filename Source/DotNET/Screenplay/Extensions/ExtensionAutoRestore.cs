// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Extensions;

/// <summary>
/// Prepares the editors (the language server and MCP) to read extension contracts for a model root: it points the binder at the
/// root's lock and restores references that are not restored yet, so the user sees "extension not restored" only until restore
/// has run.
/// </summary>
public static partial class ExtensionAutoRestore
{
    /// <summary>The environment variable naming the directory of packages restore fetches from.</summary>
    public const string SourceVariable = "SCREENPLAY_EXTENSION_SOURCE";

    /// <summary>
    /// Points the binder at the model root's lock and restores what it can.
    /// </summary>
    /// <param name="root">The model root folder.</param>
    /// <param name="source">The package source, or <see langword="null"/> to use the directory in <see cref="SourceVariable"/>.</param>
    public static void UseRoot(string root, IExtensionPackageSource? source = null)
    {
        if (!Directory.Exists(root)) return;
        var cache = ExtensionCache.Default;
        ExtensionContractResolver.Current = new(cache, ExtensionResolutionMode.Editor, root: root);
        source ??= Environment.GetEnvironmentVariable(SourceVariable) is { Length: > 0 } directory ? new DirectoryExtensionPackageSource(directory) : null;
        if (source is null) return;
        var references = References(root);
        if (references.Count == 0) return;

        var lockPath = Path.Combine(root, ExtensionLock.FileName);
        var current = File.Exists(lockPath) ? ExtensionLock.Parse(File.ReadAllText(lockPath), lockPath).Value ?? ExtensionLock.Empty : ExtensionLock.Empty;
        var restorer = new ExtensionRestorer(cache, source);
        var (next, _) = restorer.Resolve(references, current);
        _ = restorer.RestoreFromLock(next);
        if (next.Format() != current.Format()) File.WriteAllText(lockPath, next.Format());
    }

    static List<SystemExtensionSyntax> References(string root)
    {
        var references = new List<SystemExtensionSyntax>();
        foreach (var file in Directory.EnumerateFiles(root, "*.play", SearchOption.AllDirectories))
        {
            foreach (var line in File.ReadLines(file))
            {
                if (ReferenceRegex().Match(line.Trim()) is { Success: true } match)
                {
                    references.Add(new(StringLiteral.Unescape(match.Groups[1].Value), match.Groups[2].Success ? match.Groups[2].Value : null, SourceLocation.Start));
                }
            }
        }

        return references;
    }

    [GeneratedRegex(@"^extension\s+""((?:[^""\\]|\\.)*)""(?:\s+version\s+(\S+))?$", RegexOptions.NonBacktracking)]
    private static partial Regex ReferenceRegex();
}

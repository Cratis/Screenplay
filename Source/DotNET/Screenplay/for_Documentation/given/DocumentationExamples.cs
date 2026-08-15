// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace Cratis.Screenplay.for_Documentation.given;

/// <summary>
/// Represents one fenced code block found in the documentation.
/// </summary>
/// <param name="Path">The documentation file it came from, relative to the documentation root.</param>
/// <param name="Line">The line the fence opens on.</param>
/// <param name="Language">The language tag on the fence - <c>screenplay</c> or <c>pdl</c>.</param>
/// <param name="Source">The source to compile, wrapped in whatever context the fragment needs.</param>
/// <param name="Kind">What the example turned out to be, used to say what was compiled.</param>
public sealed record DocumentationExample(string Path, int Line, string Language, string Source, string Kind)
{
    /// <summary>
    /// Gets the reference a failure message points at.
    /// </summary>
    public string Reference => $"{Path}:{Line} [{Language}/{Kind}]";
}

/// <summary>
/// Finds the fenced <c>screenplay</c> and <c>pdl</c> examples in the documentation so they can be compiled.
/// </summary>
/// <remarks>
/// An example that does not compile is worse than no example - a reader takes it as the shape the language
/// accepts, and the one that broke here did so silently, by a rename nobody thought to grep the prose for.
/// <para>
/// Not every fence is an example. A page states a syntax template (<c>&lt;Name&gt;</c>, <c>{Property}</c>),
/// elides the middle of a document with <c>...</c>, or lists the forms an expression may take. Those are
/// skipped by shape rather than by a list of exceptions, so a new one does not need this file changed.
/// </para>
/// </remarks>
public static partial class DocumentationExamples
{
    static readonly HashSet<string> _topLevel = new(StringComparer.Ordinal)
    {
        "domain", "import", "concept", "type", "policy", "persona", "authentication", "seed", "module", "trigger"
    };

    static readonly HashSet<string> _sliceMembers = new(StringComparer.Ordinal)
    {
        "command", "query", "event", "projection", "reaction", "constraint", "specification", "screen", "layout", "capture", "slice", "feature"
    };

    static readonly HashSet<string> _projectionLevel = new(StringComparer.Ordinal)
    {
        "from", "every", "children", "nested", "join", "remove", "automap", "no"
    };

    static readonly string[] _mappingDirectives = ["key ", "add ", "subtract ", "parent "];

    /// <summary>
    /// Gets the documentation root, found by walking up from this file.
    /// </summary>
    /// <param name="path">The path of this source file, supplied by the compiler.</param>
    /// <returns>The full path of the documentation directory.</returns>
    public static string Root([CallerFilePath] string path = "")
    {
        var directory = Directory.GetParent(path);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "Documentation")))
        {
            directory = directory.Parent;
        }

        return Path.Combine(directory!.FullName, "Documentation");
    }

    /// <summary>
    /// Finds every example in the documentation that is meant to compile.
    /// </summary>
    /// <returns>The <see cref="DocumentationExample">examples</see>, in file order.</returns>
    public static IEnumerable<DocumentationExample> All()
    {
        var root = Root();
        foreach (var file in Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories)
            .Where(_ => _.EndsWith(".md", StringComparison.Ordinal) || _.EndsWith(".mdx", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal))
        {
            foreach (var example in InFile(file, Path.GetRelativePath(root, file)))
            {
                yield return example;
            }
        }
    }

    static IEnumerable<DocumentationExample> InFile(string file, string relative)
    {
        var lines = File.ReadAllLines(file);
        for (var index = 0; index < lines.Length; index++)
        {
            var fence = FenceRegex().Match(lines[index].Trim());
            if (!fence.Success || (fence.Groups[2].Value != "screenplay" && fence.Groups[2].Value != "pdl"))
            {
                continue;
            }

            var ticks = fence.Groups[1].Value;
            var language = fence.Groups[2].Value;
            var line = index + 1;
            var body = new List<string>();
            index++;
            while (index < lines.Length && lines[index].Trim() != ticks)
            {
                body.Add(lines[index]);
                index++;
            }

            if (Classify(body, language) is { } classified)
            {
                yield return new(relative, line, language, classified.Source, classified.Kind);
            }
        }
    }

    static (string Source, string Kind)? Classify(List<string> body, string language)
    {
        var text = string.Join('\n', body);
        if (text.Trim().Length == 0 || PlaceholderRegex().IsMatch(text) || body.Exists(_ => string.Equals(_.Trim(), "...", StringComparison.Ordinal) || string.Equals(_.Trim(), "// ...", StringComparison.Ordinal)))
        {
            return null;
        }

        var first = body.Find(_ => _.Trim().Length > 0 && !_.TrimStart().StartsWith("//", StringComparison.Ordinal) && !_.TrimStart().StartsWith('#'))?.Trim() ?? string.Empty;
        var keyword = first.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;

        if (language == "pdl")
        {
            var hasMapping = body.Exists(_ => _.Contains('=', StringComparison.Ordinal));
            var hasDirective = body.Exists(_ => Array.Exists(_mappingDirectives, directive => _.TrimStart().StartsWith(directive, StringComparison.Ordinal)));
            if (!hasMapping && !hasDirective && !_projectionLevel.Contains(keyword))
            {
                return null;
            }

            return keyword switch
            {
                "projection" => (Dedent(body), "projection"),
                _ when _projectionLevel.Contains(keyword) => (Wrap("projection Doc => DocReadModel", body), "projection body"),
                "parent" => (Wrap("projection Doc => DocReadModel\n  children docs identified by docId\n    from DocEvent", body), "child mapping"),
                _ => (Wrap("projection Doc => DocReadModel\n  from DocEvent", body), "mapping block")
            };
        }

        if (_topLevel.Contains(keyword))
        {
            return (Dedent(body), "document");
        }

        if (_sliceMembers.Contains(keyword))
        {
            var header = keyword switch
            {
                "feature" => "module Doc",
                "slice" => "module Doc\n  feature Doc",
                _ => "module Doc\n  feature Doc\n    slice StateChange Doc"
            };
            return (Wrap(header, body), $"{keyword} in a slice");
        }

        return null;
    }

    static string Dedent(List<string> body)
    {
        var indent = body.Where(_ => _.Trim().Length > 0).Select(_ => _.Length - _.TrimStart().Length).DefaultIfEmpty(0).Min();
        return string.Join('\n', body.Select(_ => _.Trim().Length == 0 ? string.Empty : _[indent..]));
    }

    static string Wrap(string header, List<string> body)
    {
        var padding = new string(' ', header.Split('\n').Length * 2);
        var builder = new StringBuilder(header).Append('\n');
        foreach (var line in Dedent(body).Split('\n'))
        {
            builder.Append(line.Length == 0 ? string.Empty : padding + line).Append('\n');
        }

        return builder.ToString();
    }

    [GeneratedRegex(@"^(`{3,4})(\w+)\s*$", RegexOptions.None, 1000)]
    private static partial Regex FenceRegex();

    [GeneratedRegex(@"<[A-Za-z][\w .]*>|\{[A-Za-z][\w .]*\}", RegexOptions.None, 1000)]
    private static partial Regex PlaceholderRegex();
}

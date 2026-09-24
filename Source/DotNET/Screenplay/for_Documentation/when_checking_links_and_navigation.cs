// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.for_Documentation.given;

namespace Cratis.Screenplay.for_Documentation;

public partial class when_checking_links_and_navigation : Specification
{
    readonly List<string> _broken = [];
    readonly HashSet<string> _reachable = new(StringComparer.OrdinalIgnoreCase);
    int _links;
    int _pages;

    void Because()
    {
        var root = DocumentationExamples.Root();
        foreach (var page in Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories)
            .Where(path => path.EndsWith(".md", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".mdx", StringComparison.OrdinalIgnoreCase)))
        {
            _pages++;
            var fence = false;
            var lines = File.ReadAllLines(page);
            for (var line = 0; line < lines.Length; line++)
            {
                var text = lines[line];
                if (FenceRegex().IsMatch(text))
                {
                    fence = !fence;
                    continue;
                }

                if (fence)
                {
                    continue;
                }

                foreach (Match match in MarkdownLinkRegex().Matches(text))
                {
                    var destination = match.Groups[1].Value;
                    if (destination.StartsWith('#') || IsLocal(destination))
                    {
                        _links++;
                        CheckLink(page, line + 1, destination);
                    }
                }
            }
        }

        VisitToc(Path.Combine(root, "screenplay", "toc.yml"));
        foreach (var page in Directory.EnumerateFiles(Path.Combine(root, "screenplay"), "*.*", SearchOption.AllDirectories)
            .Where(path => path.EndsWith(".md", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".mdx", StringComparison.OrdinalIgnoreCase)))
        {
            if (!_reachable.Contains(Path.GetFullPath(page)))
            {
                _broken.Add($"{Path.GetRelativePath(root, page)}: not reachable from screenplay/toc.yml");
            }
        }
    }

    [Fact] void should_find_pages() => _pages.ShouldBeGreaterThan(50);
    [Fact] void should_find_relative_links() => _links.ShouldBeGreaterThan(250);
    [Fact] void should_find_navigation_targets() => _reachable.Count.ShouldBeGreaterThan(50);
    [Fact] void should_resolve_every_link_and_include_every_page_in_navigation() => string.Join('\n', _broken).ShouldEqual(string.Empty);

    static bool IsLocal(string destination) =>
        !destination.StartsWith('/') &&
        !destination.StartsWith("//", StringComparison.Ordinal) &&
        !SchemeRegex().IsMatch(destination);

    void CheckLink(string source, int line, string destination)
    {
        var parts = destination.Split('#', 2);
        var path = Uri.UnescapeDataString(parts[0]);
        var target = path.Length == 0 ? source : Path.GetFullPath(Path.Combine(Path.GetDirectoryName(source)!, path));

        // DocFX/Starlight source links may omit .md/.mdx; both resolve to the same published URL.
        if (!File.Exists(target) && Path.GetExtension(target).Length == 0)
        {
            target = new[] { target + ".md", target + ".mdx" }.FirstOrDefault(File.Exists) ?? target;
        }

        if (!File.Exists(target))
        {
            _broken.Add($"{Path.GetRelativePath(DocumentationExamples.Root(), source)}:{line}: {destination} does not exist");
            return;
        }

        if (parts.Length == 2 && !Headings(target).Contains(Uri.UnescapeDataString(parts[1])))
        {
            _broken.Add($"{Path.GetRelativePath(DocumentationExamples.Root(), source)}:{line}: {destination} has no heading");
        }
    }

    static HashSet<string> Headings(string page)
    {
        var anchors = new HashSet<string>(StringComparer.Ordinal);
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        var fence = false;
        foreach (var text in File.ReadLines(page))
        {
            if (FenceRegex().IsMatch(text))
            {
                fence = !fence;
                continue;
            }

            var heading = fence ? Match.Empty : HeadingRegex().Match(text);
            if (!heading.Success)
            {
                continue;
            }

            // GitHub/DocFX-style heading IDs: lowercase, strip punctuation (not hyphens), replace
            // spaces with hyphens, and suffix duplicate headings with -1, -2, ... .
            var title = PunctuationRegex().Replace(heading.Groups[1].Value.ToLowerInvariant(), string.Empty);
            var slug = title.Replace(' ', '-');
            counts.TryGetValue(slug, out var duplicates);
            anchors.Add(duplicates == 0 ? slug : $"{slug}-{duplicates}");
            counts[slug] = duplicates + 1;
        }

        return anchors;
    }

    void VisitToc(string toc)
    {
        if (!_reachable.Add(Path.GetFullPath(toc)))
        {
            return;
        }

        if (!File.Exists(toc))
        {
            _broken.Add($"Missing navigation file {toc}");
            return;
        }

        foreach (var text in File.ReadLines(toc))
        {
            var match = TocHrefRegex().Match(text);
            if (!match.Success || !IsLocal(match.Groups[1].Value))
            {
                continue;
            }

            var target = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(toc)!, match.Groups[1].Value));
            if (target.EndsWith(".yml", StringComparison.OrdinalIgnoreCase))
            {
                VisitToc(target);
            }
            else if (!File.Exists(target))
            {
                _broken.Add($"{toc}: missing navigation target {match.Groups[1].Value}");
            }
            else
            {
                _reachable.Add(target);
            }
        }
    }

    [GeneratedRegex(@"^\s*(`{3,}|~{3,})", RegexOptions.None, 1000)]
    private static partial Regex FenceRegex();

    [GeneratedRegex(@"!?\[[^\]\n]*\]\(<?([^\s)>]+)>?(?:\s+[^)]*)?\)", RegexOptions.None, 1000)]
    private static partial Regex MarkdownLinkRegex();

    [GeneratedRegex("^[a-z][a-z0-9+.-]*:", RegexOptions.IgnoreCase, 1000)]
    private static partial Regex SchemeRegex();

    [GeneratedRegex(@"^#{1,6}\s+(.+?)\s*#*\s*$", RegexOptions.None, 1000)]
    private static partial Regex HeadingRegex();

    [GeneratedRegex(@"[^\p{L}\p{N}_\- ]", RegexOptions.None, 1000)]
    private static partial Regex PunctuationRegex();

    [GeneratedRegex(@"^\s*href:\s*['"" ]?([^'""\s#]+)", RegexOptions.None, 1000)]
    private static partial Regex TocHrefRegex();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Syntax.for_EventContextCatalog.given;

namespace Cratis.Screenplay.Syntax.for_EventContextCatalog;

/// <summary>
/// Holds the editor surfaces that describe $eventContext to the generated catalog: each must read it through
/// <c>event-context.ts</c> and none may carry a list of its own.
/// </summary>
/// <remarks>
/// Before the catalog, the projection completion offered one list, its expression completion and hover another,
/// and the host editor a third - and one of them named a member that does not exist.
/// </remarks>
public partial class when_holding_the_editor_surfaces_to_it : Specification
{
    static readonly string[] _surfaces =
    [
        "sub-languages/projection/CompletionProvider.ts",
        "sub-languages/projection/HoverProvider.ts",
        "keyword-docs.ts",
        "completion-items.ts"
    ];

    List<(string Surface, string Source)> _sources;

    void Because() => _sources =
    [
        .. _surfaces.Select(surface => (surface, File.ReadAllText(Path.Combine(EventContextSurfaces.Root(), "Source", "Screenplay", "Monaco", "screenplay-language", surface))))
    ];

    [Fact] void should_read_the_catalog_in_every_surface() => _sources.Where(_ => !CatalogImportRegex().IsMatch(_.Source)).Select(_ => _.Surface).ShouldBeEmpty();
    [Fact] void should_not_spell_out_an_event_context_path_in_any_surface() => _sources.Where(_ => PathLiteralRegex().IsMatch(_.Source)).Select(_ => _.Surface).ShouldBeEmpty();
    [Fact] void should_not_list_an_event_context_member_in_any_surface() => _sources.SelectMany(_ => ListedMembers(_.Source).Select(member => $"{_.Surface}: {member}")).ShouldBeEmpty();

    static IEnumerable<string> ListedMembers(string source) =>
        EventContextCatalog.Members
            .Select(member => member.Name)
            .Where(name => Regex.IsMatch(source, $@"name:\s*'{name}'", RegexOptions.None, TimeSpan.FromSeconds(1)));

    [GeneratedRegex(@"from\s+'(\.\./)*(\./)?event-context'", RegexOptions.None, 1000)]
    private static partial Regex CatalogImportRegex();

    [GeneratedRegex(@"['`]\$eventContext\.[A-Za-z]", RegexOptions.None, 1000)]
    private static partial Regex PathLiteralRegex();
}

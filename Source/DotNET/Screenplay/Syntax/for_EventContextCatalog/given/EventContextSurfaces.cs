// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Text;

namespace Cratis.Screenplay.Syntax.for_EventContextCatalog.given;

/// <summary>
/// Renders the surfaces generated from the <see cref="EventContextCatalog"/> - the editor's TypeScript catalog and the
/// documentation's member table - and rewrites them on explicit request.
/// </summary>
/// <remarks>
/// The editor and the documentation each used to carry their own event-context list, and they had drifted to four
/// different answers. Both are now rendered from the catalog; the specs fail when the checked-in text differs from the
/// rendering. Regeneration is opt-in through <see cref="Variable"/> and always ends in
/// <see cref="EventContextSurfacesRegenerated"/>, so a regenerating run can never pass.
/// </remarks>
public static class EventContextSurfaces
{
    /// <summary>
    /// The environment variable that requests regeneration when set to <c>1</c>.
    /// </summary>
    public const string Variable = "SCREENPLAY_REGENERATE_EVENT_CONTEXT";

    /// <summary>
    /// The marker opening the generated table in the documentation page.
    /// </summary>
    public const string TableStart = "<!-- event-context-catalog:start - generated from EventContextCatalog, do not edit by hand -->";

    /// <summary>
    /// The marker closing the generated table in the documentation page.
    /// </summary>
    public const string TableEnd = "<!-- event-context-catalog:end -->";

    const string ConceptValue = "value";

    /// <summary>
    /// Gets the path of the editor's generated TypeScript catalog.
    /// </summary>
    public static string EditorCatalogPath => Path.Combine(Root(), "Source", "Screenplay", "Monaco", "screenplay-language", "event-context-catalog.ts");

    /// <summary>
    /// Gets the path of the documentation page carrying the generated table.
    /// </summary>
    public static string DocumentationPath => Path.Combine(Root(), "Documentation", "screenplay", "projections", "event-context.md");

    /// <summary>
    /// Gets the repository root, found by walking up from this file.
    /// </summary>
    /// <param name="path">The path of this source file, supplied by the compiler.</param>
    /// <returns>The full path of the repository root.</returns>
    public static string Root([CallerFilePath] string path = "")
    {
        var directory = Directory.GetParent(path);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "Documentation")))
        {
            directory = directory.Parent;
        }

        return directory!.FullName;
    }

    /// <summary>
    /// Rewrites both surfaces from the catalog when regeneration was requested.
    /// </summary>
    /// <exception cref="EventContextSurfacesRegenerated">Always thrown after the surfaces are rewritten.</exception>
    public static void RegenerateWhenRequested()
    {
        if (Environment.GetEnvironmentVariable(Variable) != "1")
        {
            return;
        }

        File.WriteAllText(EditorCatalogPath, RenderEditorCatalog());
        File.WriteAllText(DocumentationPath, WithTable(File.ReadAllText(DocumentationPath)));
        throw new EventContextSurfacesRegenerated($"Rewrote '{EditorCatalogPath}' and the table in '{DocumentationPath}'. Review the diff, then rerun without {Variable}.");
    }

    /// <summary>
    /// Renders the editor's TypeScript catalog.
    /// </summary>
    /// <returns>The TypeScript source.</returns>
    public static string RenderEditorCatalog()
    {
        var builder = new StringBuilder()
            .Append("// Copyright (c) Cratis. All rights reserved.\n")
            .Append("// Licensed under the MIT license. See LICENSE file in the project root for full license information.\n\n")
            .Append("// Generated from Cratis.Screenplay.Syntax.EventContextCatalog - do not edit by hand. The spec\n")
            .Append("// Syntax/for_EventContextCatalog/when_holding_the_editor_catalog_to_it fails when this file drifts; rerun\n")
            .Append($"// it with {Variable}=1 to rewrite it.\n\n")
            .Append("export type EventContextMemberKind = 'value' | 'composite' | 'collection' | 'function';\n\n")
            .Append("export interface EventContextMember {\n")
            .Append("    readonly name: string;\n")
            .Append("    readonly type: string;\n")
            .Append("    readonly kind: EventContextMemberKind;\n")
            .Append("    readonly description: string;\n")
            .Append("}\n\n")
            .Append("export interface EventContextPath extends EventContextMember {\n")
            .Append("    readonly path: string;\n")
            .Append("}\n\n")
            .Append($"export const eventContextRootType = '{EventContextCatalog.EventContextType}';\n\n")
            .Append("// The members of each type a path passes through, keyed by type name.\n")
            .Append("export const eventContextTypes: Readonly<Record<string, readonly EventContextMember[]>> = {\n");

        foreach (var type in Types())
        {
            builder.Append($"    '{type.Name}': [\n");
            foreach (var member in type.Members)
            {
                builder.Append($"        {{ {Fields(member)} }},\n");
            }

            builder.Append("    ],\n");
        }

        builder.Append("};\n\n")
            .Append("// Every path the catalog lists, depth first in declaration order.\n")
            .Append("export const eventContextPaths: readonly EventContextPath[] = [\n");
        foreach (var path in EventContextCatalog.Paths)
        {
            builder.Append($"    {{ path: '{Escape(path.Path)}', {Fields(path.Member)} }},\n");
        }

        return builder.Append("];\n").ToString();
    }

    /// <summary>
    /// Renders the documentation's member table, markers included.
    /// </summary>
    /// <returns>The markdown table.</returns>
    /// <remarks>
    /// The underlying <c>value</c> of a concept-typed member is left out - the page states it once rather than in a row
    /// under every such member.
    /// </remarks>
    public static string RenderTable()
    {
        var builder = new StringBuilder()
            .Append(TableStart).Append('\n')
            .Append("| Path | Type | Description |\n")
            .Append("|---|---|---|\n");
        foreach (var path in EventContextCatalog.Paths.Where(path => path.Member.Name != ConceptValue))
        {
            builder.Append($"| `{path.Path}` | `{path.Member.Type}` | {path.Member.Description.Replace("$eventSourceId", "`$eventSourceId`", StringComparison.Ordinal)} |\n");
        }

        return builder.Append(TableEnd).ToString();
    }

    /// <summary>
    /// Gets the generated table as it currently stands in the documentation page, markers included.
    /// </summary>
    /// <param name="page">The text of the page.</param>
    /// <returns>The table, or an empty string when the page carries no markers.</returns>
    public static string TableIn(string page)
    {
        var start = page.IndexOf(TableStart, StringComparison.Ordinal);
        var end = page.IndexOf(TableEnd, StringComparison.Ordinal);
        return start < 0 || end < start ? string.Empty : page[start..(end + TableEnd.Length)];
    }

    static string WithTable(string page)
    {
        var current = TableIn(page);
        return current.Length == 0 ? page : page.Replace(current, RenderTable(), StringComparison.Ordinal);
    }

    static IEnumerable<(string Name, IReadOnlyList<EventContextMember> Members)> Types()
    {
        var seen = new List<string>();
        var pending = new Queue<string>([EventContextCatalog.EventContextType]);
        while (pending.Count > 0)
        {
            var type = pending.Dequeue();
            var members = EventContextCatalog.MembersOf(type);
            if (seen.Contains(type) || members.Count == 0)
            {
                continue;
            }

            seen.Add(type);
            yield return (type, members);
            foreach (var member in members.Where(member => member.Kind is EventContextMemberKind.Value or EventContextMemberKind.Composite))
            {
                pending.Enqueue(member.Type);
            }
        }
    }

    static string Fields(EventContextMember member) =>
        $"name: '{Escape(member.Name)}', type: '{Escape(member.Type)}', kind: '{Kind(member.Kind)}', description: '{Escape(member.Description)}'";

    static string Kind(EventContextMemberKind kind) => kind switch
    {
        EventContextMemberKind.Composite => "composite",
        EventContextMemberKind.Collection => "collection",
        EventContextMemberKind.Function => "function",
        _ => "value"
    };

    static string Escape(string text) => text.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("'", "\\'", StringComparison.Ordinal);
}

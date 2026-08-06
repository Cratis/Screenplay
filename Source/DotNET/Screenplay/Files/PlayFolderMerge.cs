// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Parsing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Files;

/// <summary>
/// Merges the parsed documents of a folder into the one application they describe, then resolves the cross
/// references of the whole.
/// </summary>
/// <remarks>
/// The rule the merge is built on: <b>the documents of a folder are one document</b>. Modules and features
/// therefore combine by name rather than collide - that is what lets a slice live in its own file while still
/// belonging to its feature - and everything else accumulates exactly as it would within a single document.
/// <para>
/// Where a name can only mean one thing, the merge reports a duplicate <b>only when it is declared in more
/// than one file</b>. A duplicate inside one file keeps whatever the single document compiler already does
/// with it, so compiling one document is unaffected by any of this.
/// </para>
/// </remarks>
internal static partial class PlayFolderMerge
{
    /// <summary>
    /// Merges parsed documents into one application and resolves its cross references.
    /// </summary>
    /// <param name="documents">The <see cref="CompilationResult{TResult}">parsed documents</see>, in path order.</param>
    /// <returns>The <see cref="CompilationResult{TResult}"/> of the folder as a whole.</returns>
    public static CompilationResult<ApplicationSyntax> Merge(IReadOnlyList<CompilationResult<ApplicationSyntax>> documents)
    {
        var context = ParserContext.ForDiagnostics();
        var application = MergeApplications([.. documents.Select(document => document.Value).OfType<ApplicationSyntax>()], context);
        ScreenplayValidator.Validate(application, context);

        return new(application, [.. documents.SelectMany(document => document.Diagnostics), .. context.Diagnostics]);
    }

    static ApplicationSyntax MergeApplications(IReadOnlyList<ApplicationSyntax> applications, ParserContext context)
    {
        // A concept and a type name the same kind of thing to everything that references one, so they share a
        // namespace and are checked against each other rather than each on their own.
        var named = new Dictionary<string, SourceLocation>(StringComparer.Ordinal);
        var concepts = DeclaredInOneFile(applications.SelectMany(application => application.Concepts), concept => concept.Name, concept => concept.Location, "declaration of", context, claimed: named);
        var types = DeclaredInOneFile(applications.SelectMany(application => application.Types ?? []), type => type.Name, type => type.Location, "declaration of", context, claimed: named);

        return new(
            [.. applications.SelectMany(application => application.Imports)
                .GroupBy(import => import.QualifiedName, StringComparer.Ordinal)
                .Select(group => group.First())],
            concepts,
            DeclaredInOneFile(applications.SelectMany(application => application.Policies), policy => policy.Name, policy => policy.Location, "policy", context),
            MergeModules(applications.SelectMany(application => application.Modules), context),
            applications.Count > 0 ? applications[0].Location : SourceLocation.Start,
            OnlyOne(applications.Select(application => application.Domain), domain => domain.Location, "domain", context),
            DeclaredInOneFile(applications.SelectMany(application => application.Personas ?? []), persona => persona.Name, persona => persona.Location, "persona", context),
            [.. applications.SelectMany(application => application.Seeds ?? [])],
            OnlyOne(applications.Select(application => application.Authentication), authentication => authentication.Location, "authentication block", context),
            types);
    }

    /// <summary>
    /// Keeps the first of a declaration the application can only have one of, reporting every later one.
    /// </summary>
    /// <typeparam name="TSyntax">The type of the declaration.</typeparam>
    /// <param name="declarations">The declaration of each document, absent as <c>null</c>.</param>
    /// <param name="location">Reads the <see cref="SourceLocation"/> of a declaration.</param>
    /// <param name="keyword">The name of the construct, used in the diagnostic.</param>
    /// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
    /// <returns>The first declaration, or <c>null</c> when no document declares one.</returns>
    /// <remarks>
    /// Each document holds at most one already - the parser rejects a second within a document - so anything
    /// found here came from another file.
    /// </remarks>
    static TSyntax? OnlyOne<TSyntax>(
        IEnumerable<TSyntax?> declarations,
        Func<TSyntax, SourceLocation> location,
        string keyword,
        ParserContext context)
        where TSyntax : class
    {
        var declared = declarations.OfType<TSyntax>().ToList();
        foreach (var extra in declared.Skip(1))
        {
            context.Error(
                $"The folder already declares a {keyword} in '{Describe(location(declared[0]).Path)}' - a folder compiles to one application, which can have at most one",
                location(extra));
        }

        return declared.Count > 0 ? declared[0] : null;
    }

    /// <summary>
    /// Keeps every declaration whose name is not already claimed by another file, reporting the ones that are.
    /// </summary>
    /// <typeparam name="TSyntax">The type of the declaration.</typeparam>
    /// <param name="declarations">The declarations of every document, in path order.</param>
    /// <param name="name">Reads the name of a declaration.</param>
    /// <param name="location">Reads the <see cref="SourceLocation"/> of a declaration.</param>
    /// <param name="keyword">The name of the construct, used in the diagnostic.</param>
    /// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
    /// <param name="within">The declaration the names have to be unique within, used in the diagnostic.</param>
    /// <param name="claimed">The names already claimed, when several kinds of declaration share one namespace.</param>
    /// <returns>The declarations that survive.</returns>
    static List<TSyntax> DeclaredInOneFile<TSyntax>(
        IEnumerable<TSyntax> declarations,
        Func<TSyntax, string> name,
        Func<TSyntax, SourceLocation> location,
        string keyword,
        ParserContext context,
        string? within = null,
        Dictionary<string, SourceLocation>? claimed = null)
    {
        var kept = new List<TSyntax>();
        claimed ??= new(StringComparer.Ordinal);

        foreach (var declaration in declarations)
        {
            var current = location(declaration);
            if (claimed.TryGetValue(name(declaration), out var first) && !string.Equals(first.Path, current.Path, StringComparison.Ordinal))
            {
                context.Error(
                    $"Duplicate {keyword} '{name(declaration)}'{(within is null ? string.Empty : $" in {within}")} - already declared in '{Describe(first.Path)}'",
                    current);
                continue;
            }

            claimed.TryAdd(name(declaration), current);
            kept.Add(declaration);
        }

        return kept;
    }

    static string Describe(string? path) => path ?? "another file";
}

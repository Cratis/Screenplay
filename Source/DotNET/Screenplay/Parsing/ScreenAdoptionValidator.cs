// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Checks the declarations of free screens and the contract between a screen and the slices that adopt it: shapes,
/// needs, <c>uses screen</c> and the provisions that supply each need.
/// </summary>
internal static class ScreenAdoptionValidator
{
    /// <summary>
    /// Validates shapes, free screens, adoptions and provisions.
    /// </summary>
    /// <param name="application">The <see cref="ApplicationSyntax"/> to validate.</param>
    /// <param name="declarations">The <see cref="ConsistencyDeclarations"/> that resolve queries and read models.</param>
    /// <param name="screens">Every screen of the model with the scope it is declared in.</param>
    /// <param name="context">The <see cref="ParserContext"/> to report to.</param>
    public static void Validate(
        ApplicationSyntax application,
        ConsistencyDeclarations declarations,
        IReadOnlyList<ScopedScreen> screens,
        ParserContext context)
    {
        ReportDuplicateShapes(application, context);
        ReportDuplicateScreens(screens, context);
        foreach (var entry in screens)
        {
            ReportDuplicateNeeds(entry.Screen, context);
        }

        var adopted = new HashSet<ScreenSyntax>(ReferenceEqualityComparer.Instance);
        var pool = screens.Select(entry => new Declaration(entry.Screen.Name, entry.Scope)).ToList();
        foreach (var (slice, sliceScope) in declarations.Slices)
        {
            var provided = new Dictionary<ScreenSyntax, HashSet<string>>(ReferenceEqualityComparer.Instance);
            var adoptions = new List<(ScreenSyntax Screen, SourceLocation Location)>();
            foreach (var own in slice.Screens)
            {
                adopted.Add(own);
                adoptions.Add((own, own.Location));
            }

            var used = new HashSet<ScreenSyntax>(ReferenceEqualityComparer.Instance);
            foreach (var adoption in slice.UsedScreens.Where(adoption => adoption.Screen.Length > 0))
            {
                var resolved = ResolveScreen(adoption, sliceScope, screens, pool, context);
                if (resolved is null)
                {
                    continue;
                }

                adopted.Add(resolved.Screen);
                if (!used.Add(resolved.Screen))
                {
                    context.Error(DiagnosticCodes.DuplicateScreenAdoption, $"Slice '{slice.Name}' adopts screen '{adoption.Screen}' more than once - adopt it once and put every provision under that line", adoption.Location);
                    continue;
                }

                if (!adoptions.Exists(entry => ReferenceEquals(entry.Screen, resolved.Screen)))
                {
                    adoptions.Add((resolved.Screen, adoption.Location));
                }

                if (!provided.TryGetValue(resolved.Screen, out var names))
                {
                    provided[resolved.Screen] = names = new(StringComparer.Ordinal);
                }

                CheckProvisions(application, declarations, sliceScope, resolved, adoption, names, context);
            }

            foreach (var (screen, location) in adoptions)
            {
                var names = provided.TryGetValue(screen, out var found) ? found : [];
                foreach (var need in Needs(screen).DistinctBy(need => need.Name).Where(need => !names.Contains(need.Name)))
                {
                    context.Warning(
                        DiagnosticCodes.UnprovidedScreenNeed,
                        $"Screen '{screen.Name}' needs '{need.Name}' and slice '{slice.Name}' does not provide it - add 'data {need.Name} via query <Query>' under 'uses screen {screen.Name}'",
                        location);
                }
            }
        }

        foreach (var entry in screens.Where(entry => !adopted.Contains(entry.Screen) && !entry.InSlice))
        {
            context.Add(new Diagnostic(
                DiagnosticSeverity.Information,
                DiagnosticCodes.DesignOnlyScreen,
                $"Screen '{entry.Screen.Name}' is not adopted by any slice - it is design-only until a slice adds 'uses screen {entry.Screen.Name}'",
                entry.Screen.Location));
        }
    }

    /// <summary>
    /// Gets the needs of a screen, in authored order, wherever in the screen they are written.
    /// </summary>
    /// <param name="screen">The <see cref="ScreenSyntax"/> to read.</param>
    /// <returns>The <see cref="ScreenNeedSyntax"/> directives of the screen.</returns>
    internal static IEnumerable<ScreenNeedSyntax> Needs(ScreenSyntax screen) =>
        ScreenplayValidator.AllDirectives(screen.Directives).OfType<ScreenNeedSyntax>();

    /// <summary>
    /// Gets the fields a screen reads of one need: table columns, summary fields, row-click keys and bound paths.
    /// </summary>
    /// <param name="screen">The <see cref="ScreenSyntax"/> to read.</param>
    /// <param name="need">The need whose fields to collect.</param>
    /// <returns>The distinct field paths, in the order the screen first reads them.</returns>
    internal static IReadOnlyList<string> Reads(ScreenSyntax screen, ScreenNeedSyntax need)
    {
        var fields = new List<string>();
        foreach (var directive in ScreenplayValidator.AllDirectives(screen.Directives))
        {
            switch (directive)
            {
                case ScreenTableSyntax table when table.Target == need.Name || (need.Alias is null && table.Target == need.Type.Name):
                    fields.AddRange(table.Columns.Select(column => column.Property));
                    if (table.RowClick?.By is { } key) fields.Add(key);
                    break;
                case ScreenSummarySyntax summary when summary.Target == need.Name || (need.Alias is null && summary.Target == need.Type.Name):
                    fields.AddRange(summary.Fields.Select(field => field.Property));
                    break;
                case ScreenComponentSyntax component:
                    foreach (var binding in component.Properties.Select(property => property.Binding).Append(component.Context).OfType<UiBindingSyntax>())
                    {
                        if (binding.BindingKind == UiBindingKind.DataContext && binding.Path.StartsWith(need.Name + ".", StringComparison.Ordinal))
                        {
                            fields.Add(binding.Path[(need.Name.Length + 1)..]);
                        }
                    }

                    break;
            }
        }

        return [.. fields.Distinct(StringComparer.Ordinal)];
    }

    static void ReportDuplicateShapes(ApplicationSyntax application, ParserContext context)
    {
        foreach (var duplicate in application.Shapes.GroupBy(shape => shape.Name, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .SelectMany(group => group.Skip(1)))
        {
            context.Error(DiagnosticCodes.DuplicateShape, $"Duplicate shape '{duplicate.Name}' - a shape is declared once", duplicate.Location);
        }
    }

    static void ReportDuplicateScreens(IReadOnlyList<ScopedScreen> screens, ParserContext context)
    {
        foreach (var duplicate in screens.Where(entry => !entry.InSlice)
            .GroupBy(entry => (entry.Screen.Name, Scope: string.Join('.', entry.Scope.Segments)))
            .Where(group => group.Count() > 1)
            .SelectMany(group => group.Skip(1)))
        {
            context.Error(DiagnosticCodes.DuplicateScreen, $"Duplicate screen '{duplicate.Screen.Name}' - a screen is declared once in a scope", duplicate.Screen.Location);
        }
    }

    static void ReportDuplicateNeeds(ScreenSyntax screen, ParserContext context)
    {
        foreach (var duplicate in Needs(screen).GroupBy(need => need.Name, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .SelectMany(group => group.Skip(1)))
        {
            context.Error(
                DiagnosticCodes.DuplicateScreenNeed,
                $"Screen '{screen.Name}' needs '{duplicate.Name}' more than once - tell the needs apart with 'data {duplicate.Type.Name}[] as <alias>'",
                duplicate.Location);
        }
    }

    static ScopedScreen? ResolveScreen(UsesScreenSyntax used, DeclarationScope scope, IReadOnlyList<ScopedScreen> screens, IReadOnlyList<Declaration> pool, ParserContext context)
    {
        var resolution = ReferenceResolver.Resolve(used.Screen, scope, pool);
        if (resolution.Resolved is { } resolved)
        {
            return screens[IndexOf(pool, resolved)];
        }

        ScreenplayValidator.Report(used.Screen, scope, pool, DiagnosticCodes.UnknownScreen, "screen", used.Location, context);
        return null;
    }

    static int IndexOf(IReadOnlyList<Declaration> pool, Declaration declaration)
    {
        for (var index = 0; index < pool.Count; index++)
        {
            if (ReferenceEquals(pool[index], declaration)) return index;
        }

        return -1;
    }

    static void CheckProvisions(
        ApplicationSyntax application,
        ConsistencyDeclarations declarations,
        DeclarationScope sliceScope,
        ScopedScreen resolved,
        UsesScreenSyntax used,
        HashSet<string> provided,
        ParserContext context)
    {
        var screen = resolved.Screen;
        var needs = Needs(screen).ToList();
        var sources = ScreenplayValidator.AllDirectives(screen.Directives).OfType<ScreenDataSyntax>().ToList();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var provision in used.Provisions)
        {
            var need = needs.Find(candidate => candidate.Name == provision.Need);
            if (need is null)
            {
                if (sources.Exists(source => source.Type.Name == provision.Need))
                {
                    context.Error(DiagnosticCodes.ProvisionOverridesScreenSource, $"Screen '{screen.Name}' already sources '{provision.Need}' with its own 'data {provision.Need} via query' - a provision never overrides a source the screen resolves", provision.Location);
                }
                else
                {
                    context.Error(DiagnosticCodes.UnknownScreenNeed, $"Screen '{screen.Name}' does not need '{provision.Need}'{NeedList(needs)}", provision.Location);
                }

                continue;
            }

            if (!seen.Add(provision.Need))
            {
                context.Error(DiagnosticCodes.DuplicateScreenProvision, $"Need '{provision.Need}' of screen '{screen.Name}' is provided more than once", provision.Location);
                continue;
            }

            provided.Add(need.Name);
            CheckProvision(application, declarations, sliceScope, screen, need, provision, context);
        }
    }

    static string NeedList(List<ScreenNeedSyntax> needs) =>
        needs.Count == 0 ? " - it declares no data needs" : $" - it needs {string.Join(", ", needs.Select(need => $"'{need.Name}'"))}";

    static void CheckProvision(
        ApplicationSyntax application,
        ConsistencyDeclarations declarations,
        DeclarationScope sliceScope,
        ScreenSyntax screen,
        ScreenNeedSyntax need,
        ScreenProvisionSyntax provision,
        ParserContext context)
    {
        var queries = declarations.Slices.SelectMany(entry => entry.Slice.Queries.Select(query => new Declaration(query.Name, entry.Scope))).ToList();
        ScreenplayValidator.Report(provision.Query, sliceScope, queries, DiagnosticCodes.UnknownQuery, "query", provision.Location, context);
        if (declarations.Resolve(provision.Query, sliceScope, slice => slice.Queries, query => query.Name) is not { } found)
        {
            return;
        }

        var (query, queryScope) = found;
        if (query.ReturnType.IsCollection != need.Type.IsCollection)
        {
            context.Error(
                DiagnosticCodes.ScreenNeedCardinalityMismatch,
                $"Screen '{screen.Name}' needs {(need.Type.IsCollection ? "a list" : "a single value")} for '{need.Name}', and query '{query.Name}' returns {(query.ReturnType.IsCollection ? "a list" : "a single value")}",
                provision.Location);
        }

        if (provision.By is { } by && !QueryParameters(query).Contains(by, StringComparer.Ordinal))
        {
            context.Error(DiagnosticCodes.UnknownProvisionParameter, $"Query '{query.Name}' has no parameter '{by}' to supply with 'by {by}'", provision.Location);
        }

        var shape = application.Shapes.FirstOrDefault(candidate => candidate.Name == need.Type.Name);
        var explicitFields = shape?.Properties.ToDictionary(property => property.Name, StringComparer.Ordinal) ?? [];
        var fields = explicitFields.Keys.Concat(Reads(screen, need)).Distinct(StringComparer.Ordinal).ToList();
        foreach (var mapping in provision.Mappings.Where(mapping => !fields.Contains(mapping.Field, StringComparer.Ordinal)))
        {
            context.Error(DiagnosticCodes.UnknownProvisionMapping, $"Screen '{screen.Name}' does not read '{mapping.Field}' of '{need.Name}', so there is nothing to map it to{FieldList(fields)}", mapping.Location);
        }

        if (declarations.ViewProperties(query.ReturnType.Name, queryScope) is not { } properties)
        {
            return;
        }

        var providingProperties = properties.ToList();
        foreach (var field in fields)
        {
            var mapping = provision.Mappings.FirstOrDefault(candidate => candidate.Field == field);
            var path = mapping?.Path ?? field;
            var property = declarations.Property(providingProperties, path, out var missing);
            if (property is null)
            {
                if (missing)
                {
                    context.Error(
                        DiagnosticCodes.ProvisionFieldMissing,
                        $"Read model '{query.ReturnType.Name}' of query '{query.Name}' has no '{path}' to provide '{field}' of '{need.Name}'{(mapping is null ? $" - add '{field} from <path>' under the provision if it is named differently" : string.Empty)}",
                        mapping?.Location ?? provision.Location);
                }

                continue;
            }

            if (explicitFields.TryGetValue(field, out var declared) && !SameType(application, property.Type, declared.Type))
            {
                context.Error(
                    DiagnosticCodes.ProvisionFieldTypeMismatch,
                    $"'{path}' of read model '{query.ReturnType.Name}' is {Describe(property.Type)}, and shape '{need.Type.Name}' declares '{field}' as {Describe(declared.Type)}",
                    mapping?.Location ?? provision.Location);
            }
        }
    }

    static string FieldList(List<string> fields) =>
        fields.Count == 0 ? string.Empty : $" - it reads {string.Join(", ", fields.Select(field => $"'{field}'"))}";

    static IEnumerable<string> QueryParameters(QuerySyntax query) =>
        (query.By is null ? [] : new[] { query.By.Name })
        .Concat(query.ByParts.Select(part => part.Name))
        .Concat(query.Filters.Select(filter => filter.Name));

    static string Describe(TypeRefSyntax type) => $"{type.Name}{(type.IsCollection ? "[]" : string.Empty)}{(type.IsOptional ? " optional" : string.Empty)}";

    // A concept stands for its primitive: a shape that says String accepts a property typed with a concept over String.
    static bool SameType(ApplicationSyntax application, TypeRefSyntax provided, TypeRefSyntax declared)
    {
        string Underlying(string name) => application.Concepts.FirstOrDefault(concept => concept.Name == name && !concept.IsEnum)?.Type ?? name;
        var known = ConceptSyntax.PrimitiveTypes.Concat(application.Concepts.Select(concept => concept.Name)).Concat((application.Types ?? []).Select(type => type.Name)).ToHashSet(StringComparer.Ordinal);
        if (!known.Contains(provided.Name) || !known.Contains(declared.Name))
        {
            return true;
        }

        return Underlying(provided.Name) == Underlying(declared.Name) &&
            provided.IsCollection == declared.IsCollection &&
            (!provided.IsOptional || declared.IsOptional);
    }
}

/// <summary>
/// A screen with the scope it is declared in.
/// </summary>
/// <param name="Screen">The <see cref="ScreenSyntax"/>.</param>
/// <param name="Scope">The <see cref="DeclarationScope"/> of its declaration site: the document, a module, a feature or a slice.</param>
/// <param name="InSlice">Whether a slice declares the screen in its own body.</param>
internal sealed record ScopedScreen(ScreenSyntax Screen, DeclarationScope Scope, bool InSlice);

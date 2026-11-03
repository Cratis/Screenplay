// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Keeps explicit passive read models private to command decisions, using scoped view identities.
/// </summary>
internal static class PassiveReadModelValidator
{
    internal static void Validate(ApplicationSyntax application, ConsistencyDeclarations declarations, ParserContext context)
    {
        var passive = PassiveModels(declarations);
        if (passive.Count == 0) return;
        var read = new HashSet<Declaration>();
        foreach (var (slice, scope) in ScreenplayValidator.ScopedSlices(application))
        {
            MarkReads(slice, scope, declarations, passive, read);
            foreach (var query in slice.Queries) ValidateQuery(query, scope, declarations, passive, context);
            foreach (var data in slice.Screens.SelectMany(screen => ScreenplayValidator.AllDirectives(screen.Directives)).OfType<ScreenDataSyntax>())
            {
                ValidateData(data, scope, declarations, passive, context);
            }
        }
        foreach (var (_, model) in passive.Where(entry => !read.Contains(entry.Key)))
        {
            context.Warning(DiagnosticCodes.PassiveReadModelNotRead, $"Passive read model '{model.Name}' is not read by any command.", model.DirectiveLocations.GetValueOrDefault("passive", model.Location));
        }
    }

    static Dictionary<Declaration, ReadModelSyntax> PassiveModels(ConsistencyDeclarations declarations) =>
        declarations.Slices.SelectMany(entry => (entry.Slice.ReadModels ?? [])
            .Where(model => model.IsPassive)
            .Select(model => (Declaration: new Declaration(model.Name, entry.Scope), Model: model)))
            .GroupBy(entry => entry.Declaration)
            .ToDictionary(group => group.Key, group => group.First().Model);

    static void MarkReads(SliceSyntax slice, DeclarationScope scope, ConsistencyDeclarations declarations, Dictionary<Declaration, ReadModelSyntax> passive, HashSet<Declaration> read)
    {
        foreach (var dependency in slice.Commands.SelectMany(command => command.Reads ?? []))
        {
            if (declarations.View(dependency.ReadModel, scope) is { } view && passive.ContainsKey(view)) read.Add(view);
        }
    }

    static void ValidateQuery(QuerySyntax query, DeclarationScope scope, ConsistencyDeclarations declarations, Dictionary<Declaration, ReadModelSyntax> passive, ParserContext context)
    {
        if (declarations.View(query.ReturnType.Name, scope) is not { } view || !passive.TryGetValue(view, out var model)) return;
        context.Error(DiagnosticCodes.PassiveReadModelObserved, $"Query '{query.Name}' returns passive read model '{model.Name}'; a passive read model is replayed for command decisions only and cannot be queried.", query.Location);
    }

    static void ValidateData(ScreenDataSyntax data, DeclarationScope scope, ConsistencyDeclarations declarations, Dictionary<Declaration, ReadModelSyntax> passive, ParserContext context)
    {
        if (declarations.View(data.Type.Name, scope) is not { } view || !passive.TryGetValue(view, out var model)) return;
        context.Error(DiagnosticCodes.PassiveReadModelObserved, $"Screen data references passive read model '{model.Name}'; a passive read model is replayed for command decisions only and cannot be shown.", data.Location);
    }
}

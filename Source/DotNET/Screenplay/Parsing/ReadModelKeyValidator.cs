// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Checks declared read-model keys and named single-instance lookups.
/// </summary>
internal static class ReadModelKeyValidator
{
    internal static void Validate(ApplicationSyntax application, ConsistencyDeclarations declarations, ParserContext context)
    {
        var models = declarations.Slices.SelectMany(entry => entry.Slice.ReadModels ?? []).ToArray();
        var allowed = new HashSet<PropertySyntax>(models.SelectMany(model => model.Properties), ReferenceEqualityComparer.Instance);
        var owners = new KeyOwners(allowed, context);
        owners.VisitApplication(application);
        if (!owners.RequiresLookups) return;
        foreach (var model in models)
        {
            var keys = model.Properties.Where(property => property.IsKey).ToArray();
            foreach (var duplicate in keys.GroupBy(property => property.Name, StringComparer.Ordinal).Where(group => group.Count() > 1).SelectMany(group => group.Skip(1)))
            {
                context.Error(DiagnosticCodes.InvalidReadModelKey, $"Read-model key part '{duplicate.Name}' must be declared exactly once.", duplicate.Location);
            }
            foreach (var property in keys)
            {
                if (keys.Length > 1 && declarations.TypeProperties(property.Type.Name) is not null)
                {
                    context.Error(DiagnosticCodes.InvalidReadModelKey, "A read-model key part must be required and noncollection, cannot combine modifiers, and a multipart key must have scalar parts.", property.Location);
                }
            }
        }
        foreach (var (slice, scope) in declarations.Slices)
        {
            foreach (var query in slice.Queries)
            {
                var key = declarations.ViewKey(query.ReturnType.Name, scope);
                QueryParameterSyntax[] parts = query.By is { } by ? [by] : [.. query.ByParts];
                if (key.Length == 0 || (key.Length == 1 && !key[0].IsKey && !query.ByParts.Any())) continue;
                if (parts.Length == 0)
                {
                    if (!query.ReturnType.IsCollection && key.Any(property => property.IsKey)) Complete(key, [], query.Location);
                    continue;
                }
                if (query.ByParts.Any() && key.Length == 1)
                {
                    Invalid("A by block cannot look up a single-key view.", query.Location);
                }
                if (!query.ReturnType.IsCollection) Complete(key, parts.Select(part => part.Name), query.Location);
                foreach (var part in parts)
                {
                    var target = key.FirstOrDefault(property => property.Name == part.Name);
                    if (target is null || part.Type.IsOptional || part.Type.IsCollection || declarations.Compatible(part.Type, target.Type) == false)
                    {
                        Invalid($"Query key part '{part.Name}' must name a key property with its required compatible type.", part.Location);
                    }
                }
            }
            foreach (var command in slice.Commands)
            {
                foreach (var read in command.Reads ?? []) ValidateRead(read, command.Properties, scope);
            }
            foreach (var trigger in slice.Reactions.SelectMany(reaction => reaction.Triggers))
            {
                var properties = trigger.Data.Where(datum => datum.Type is not null)
                    .Select(datum => new PropertySyntax(datum.Name, datum.Type!, datum.Location)).ToArray();
                if (trigger.Source is NamedTriggerSourceSyntax named)
                {
                    var source = declarations.Event(named.Name, scope)?.Properties;
                    var triggerValues = (application.Triggers ?? []).SingleOrDefault(declared => declared.Name == named.Name)?.Data
                        .Where(datum => datum.Type is not null && trigger.Data.Any(selected => selected.Name == datum.Name))
                        .Select(datum => new PropertySyntax(datum.Name, datum.Type!, datum.Location));
                    properties = [.. properties.Concat(source ?? triggerValues ?? []).DistinctBy(property => property.Name)];
                }
                foreach (var read in trigger.Reads ?? []) ValidateRead(read, properties, scope);
            }
            foreach (var specification in slice.Specifications)
            {
                foreach (var state in (specification.GivenReadModels ?? []).Concat(specification.ThenReadModels ?? []))
                {
                    var key = declarations.ViewKey(state.Name, scope);
                    if (key.Length < 2 && !key.Any(property => property.IsKey)) continue;
                    foreach (var missing in key.Where(part => !state.Properties.Any(value => value.Property == part.Name)))
                    {
                        context.Error(DiagnosticCodes.MissingSpecificationReadModelIdentifier, $"Specification read model '{state.Name}' must state key part '{missing.Name}' in this block.", state.Location);
                    }
                }
                foreach (var absent in specification.ThenAbsentReadModels)
                {
                    var key = declarations.ViewKey(absent.Name, scope);
                    if (key.Length > 1)
                    {
                        var names = absent.Key is ObjectExpressionSyntax obj ? obj.Members.Select(member => member.Name).ToArray() : [];
                        Complete(key, names, absent.Location);
                        foreach (var name in names.Where(name => !key.Any(part => part.Name == name))) Invalid($"Unknown key part '{name}'.", absent.Location);
                        if (absent.Key is ObjectExpressionSyntax value)
                        {
                            SpecificationValueConsistencyValidator.ValidateValues(value.Members.Select(member => new PropertyMappingSyntax(member.Name, member.Value, member.Location)), key, declarations, context);
                        }
                    }
                }
            }
            foreach (var screen in slice.Screens)
            {
                var walker = new Lookups(
                    queryName => declarations.Resolve(queryName, scope, owner => owner.Queries, query => query.Name),
                    ValidateUi,
                    navigation => ValidateNavigation(navigation, scope));
                walker.VisitScreen(screen);
            }
        }
        foreach (var module in application.Modules)
        {
            foreach (var form in module.Forms ?? [])
            {
                if (form.Populate is FormPopulateViaQuerySyntax populate)
                {
                    var query = declarations.Resolve(populate.Query, new DeclarationScope([module.Name]), owner => owner.Queries, item => item.Name);
                    if (query is { } resolved) ValidateUi(resolved, populate.By, populate.Location);
                }
                if (form.OnSubmit is { } navigation) ValidateNavigation(navigation, new DeclarationScope([module.Name]));
            }
        }

        void ValidateNavigation(ScreenNavigateSyntax navigation, DeclarationScope scope)
        {
            if (declarations.Resolve(navigation.Screen, scope, owner => owner.Screens, screen => screen.Name) is not { } resolved) return;
            var collector = new DataLookups();
            collector.VisitScreen(resolved.Node);
            foreach (var data in collector.Data)
            {
                if (declarations.Resolve(data.Query, resolved.Scope, owner => owner.Queries, query => query.Name) is { } query)
                {
                    ValidateUi(query, navigation.By, navigation.Location);
                }
            }
        }

        void ValidateUi((QuerySyntax Node, DeclarationScope Scope) resolved, string? by, SourceLocation location)
        {
            if (!resolved.Node.ReturnType.IsCollection)
            {
                var key = declarations.ViewKey(resolved.Node.ReturnType.Name, resolved.Scope);
                if (key.Length > 1) Complete(key, by is null ? [] : [by], location);
            }
        }

        void ValidateRead(ReadsSyntax read, IEnumerable<PropertySyntax> sources, DeclarationScope scope)
        {
            if (read.By is null && !read.ByParts.Any()) return;
            var key = declarations.ViewKey(read.ReadModel, scope);
            if (key.Length == 0) return;
            if (!read.ByParts.Any())
            {
                if (key.Length > 1) Complete(key, read.By is null ? [] : [read.By], read.Location);
                return;
            }
            if (key.Length == 1) Invalid("A by block cannot look up a single-key view.", read.Location);
            Complete(key, read.ByParts.Select(part => part.Property), read.Location);
            foreach (var part in read.ByParts)
            {
                var target = key.FirstOrDefault(property => property.Name == part.Property);
                var source = part.Source is PathExpressionSyntax path ? RequiredSource(sources, path.Path) : null;
                if (target is null || source is null || declarations.Compatible(source.Type, target.Type) == false)
                {
                    Invalid($"Read key part '{part.Property}' requires a required, noncollection property path of the key part's type, not a literal or unknown part.", part.Location);
                }
            }
        }

        PropertySyntax? RequiredSource(IEnumerable<PropertySyntax> properties, string path)
        {
            var segments = path.Split('.');
            var current = properties;
            for (var index = 0; index < segments.Length; index++)
            {
                var matches = current.Where(property => property.Name == segments[index]).ToArray();
                if (matches.Length != 1 || matches[0].Type.IsOptional || matches[0].Type.IsCollection) return null;
                if (index == segments.Length - 1) return matches[0];
                current = declarations.TypeProperties(matches[0].Type.Name) ?? [];
            }

            return null;
        }

        void Complete(PropertySyntax[] key, IEnumerable<string> supplied, SourceLocation location)
        {
            var missing = key.Select(part => part.Name).Except(supplied, StringComparer.Ordinal).ToArray();
            if (missing.Length > 0)
            {
                context.Error(DiagnosticCodes.IncompleteReadModelKey, $"Read-model lookup is missing key parts: {string.Join(", ", missing)}.", location);
            }
        }

        void Invalid(string message, SourceLocation location) => context.Error(DiagnosticCodes.InvalidReadModelKeyLookup, message, location);
    }

    sealed class KeyOwners(HashSet<PropertySyntax> allowed, ParserContext context) : ScreenplaySyntaxWalker
    {
        internal bool RequiresLookups { get; private set; }

        public override void VisitProperty(PropertySyntax syntax)
        {
            if (syntax.IsKey)
            {
                if (allowed.Contains(syntax)) RequiresLookups = true;
                else context.Error(DiagnosticCodes.InvalidReadModelKey, "The key modifier is only valid on top-level read-model properties.", syntax.Location);
            }
            base.VisitProperty(syntax);
        }

        public override void VisitQuery(QuerySyntax syntax)
        {
            RequiresLookups |= syntax.ByParts.Any();
            base.VisitQuery(syntax);
        }

        public override void VisitReads(ReadsSyntax syntax)
        {
            RequiresLookups |= syntax.ByParts.Any();
            base.VisitReads(syntax);
        }
    }

    sealed class Lookups(
        Func<string, (QuerySyntax Node, DeclarationScope Scope)?> resolve,
        Action<(QuerySyntax Node, DeclarationScope Scope), string?, SourceLocation> check,
        Action<ScreenNavigateSyntax> navigate) : ScreenplaySyntaxWalker
    {
        public override void VisitScreenData(ScreenDataSyntax syntax)
        {
            if (resolve(syntax.Query) is { } query) check(query, syntax.By, syntax.Location);
            base.VisitScreenData(syntax);
        }

        public override void VisitScreenNavigate(ScreenNavigateSyntax syntax)
        {
            navigate(syntax);
            base.VisitScreenNavigate(syntax);
        }
    }

    sealed class DataLookups : ScreenplaySyntaxWalker
    {
        internal List<ScreenDataSyntax> Data { get; } = [];

        public override void VisitScreenData(ScreenDataSyntax syntax)
        {
            Data.Add(syntax);
            base.VisitScreenData(syntax);
        }
    }
}

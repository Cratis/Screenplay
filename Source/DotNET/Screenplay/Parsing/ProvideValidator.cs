// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Checks the values added to <c>$context</c> by <c>provide</c> lines across the application, module, feature and slice levels.
/// </summary>
internal static class ProvideValidator
{
    /// <summary>
    /// Gets the names a <c>provide</c> may not use: the built-in <c>$context</c> members and the job-specific ones.
    /// </summary>
    internal static readonly IReadOnlySet<string> ReservedNames = new HashSet<string>(StringComparer.Ordinal)
    {
        "tenant", "identity", "causedBy", "causation", "occurred", "audiences", "command", "arguments"
    };

    /// <summary>
    /// Validates every provided value of an application.
    /// </summary>
    /// <param name="application">The <see cref="ApplicationSyntax"/> to validate.</param>
    /// <param name="knownTypes">The names of the types a provided value may have.</param>
    /// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
    internal static void Validate(ApplicationSyntax application, IReadOnlySet<string> knownTypes, ParserContext context)
    {
        new ContextPathValidator(context).VisitApplication(application);
        var declarations = new ConsistencyDeclarations(application, [.. ScreenplayValidator.ScopedSlices(application)]);
        var validation = new Validation(declarations, knownTypes, context);
        var root = validation.Declare(new Dictionary<string, ProvideSyntax>(StringComparer.Ordinal), application.Provides);
        foreach (var module in application.Modules)
        {
            var effective = validation.Declare(root, module.Provides);
            foreach (var feature in module.Features) validation.Feature(effective, feature);
        }
    }

    /// <summary>
    /// Gets the names of the provided values a source reads through <c>$context.&lt;name&gt;</c>.
    /// </summary>
    /// <param name="source">The <see cref="ProvideSourceSyntax"/> to inspect.</param>
    /// <returns>The names read.</returns>
    internal static IEnumerable<string> ReadNames(ProvideSourceSyntax source)
    {
        var expressions = source switch
        {
            QueryProvideSourceSyntax query => query.By,
            ValueProvideSourceSyntax value => [value.Value],
            _ => []
        };
        return expressions.OfType<ContextExpressionSyntax>().Select(expression => expression.Root);
    }

    sealed class Validation(ConsistencyDeclarations declarations, IReadOnlySet<string> knownTypes, ParserContext context)
    {
        readonly HashSet<SourceLocation> _cycles = [];

        internal void Feature(Dictionary<string, ProvideSyntax> inherited, FeatureSyntax feature)
        {
            var effective = Declare(inherited, feature.Provides);
            foreach (var slice in feature.Slices) Declare(effective, slice.Provides);
            foreach (var nested in feature.Features) Feature(effective, nested);
        }

        internal Dictionary<string, ProvideSyntax> Declare(Dictionary<string, ProvideSyntax> inherited, IEnumerable<ProvideSyntax> provides)
        {
            var effective = new Dictionary<string, ProvideSyntax>(inherited, StringComparer.Ordinal);
            var declaredHere = new HashSet<string>(StringComparer.Ordinal);
            foreach (var provide in provides)
            {
                if (!declaredHere.Add(provide.Name))
                {
                    context.Error(DiagnosticCodes.DuplicateProvide, $"'{provide.Name}' is provided more than once at this level - declare each value once", provide.Location);
                    continue;
                }

                if (ReservedNames.Contains(provide.Name))
                {
                    context.Error(DiagnosticCodes.ReservedProvideName, $"'{provide.Name}' is a built-in $context value and cannot be provided - no level can change who a request is", provide.Location);
                    continue;
                }

                if (inherited.TryGetValue(provide.Name, out var overridden) && !SameType(overridden.Type, provide.Type))
                {
                    context.Error(DiagnosticCodes.ProvideTypeMismatch, $"'{provide.Name}' is provided as {Describe(overridden.Type)} higher up - an override keeps the type, not {Describe(provide.Type)}", provide.Location);
                }

                if (!knownTypes.Contains(provide.Type.Name))
                {
                    context.Warning(DiagnosticCodes.UnknownType, $"Unknown type '{provide.Type.Name}' on provided value '{provide.Name}'", provide.Type.Location);
                }

                if (provide.Source is QueryProvideSourceSyntax query) ValidateQuery(provide, query);
                effective[provide.Name] = provide;
            }

            ValidateCycles(effective);
            return effective;
        }

        static bool SameType(TypeRefSyntax left, TypeRefSyntax right) =>
            left.Name == right.Name && left.IsCollection == right.IsCollection && left.IsOptional == right.IsOptional;

        static int KeyCount(QuerySyntax query)
        {
            if (query.ByParts.Any())
            {
                return query.ByParts.Count();
            }

            return query.By is null ? 0 : 1;
        }

        static string Describe(TypeRefSyntax type) => $"{type.Name}{(type.IsCollection ? "[]" : string.Empty)}{(type.IsOptional ? " optional" : string.Empty)}";

        static bool Reaches(Dictionary<string, ProvideSyntax> effective, string current, string target, List<string> path, HashSet<string> seen)
        {
            if (!effective.TryGetValue(current, out var provide)) return false;
            foreach (var read in ReadNames(provide.Source).Where(effective.ContainsKey))
            {
                if (read == target)
                {
                    path.Add(read);
                    return true;
                }

                if (!seen.Add(read)) continue;
                path.Add(read);
                if (Reaches(effective, read, target, path, seen)) return true;
                path.RemoveAt(path.Count - 1);
            }

            return false;
        }

        void ValidateQuery(ProvideSyntax provide, QueryProvideSourceSyntax source)
        {
            if (declarations.Resolve(source.Query, new([]), slice => slice.Queries, query => query.Name) is not { } resolved)
            {
                context.Error(DiagnosticCodes.UnknownProvideQuery, $"Unknown or ambiguous query '{source.Query}' for provided value '{provide.Name}'", source.Location);
                return;
            }

            var query = resolved.Node;
            if (query.IsObservable)
            {
                context.Error(DiagnosticCodes.ObservableProvideQuery, $"Query '{source.Query}' is observable - a provided value is resolved once per request, so it needs a snapshot query", source.Location);
            }

            var keys = KeyCount(query);
            var supplied = source.By.Count();
            if (supplied != keys)
            {
                context.Error(DiagnosticCodes.ProvideQueryTypeMismatch, $"Query '{source.Query}' takes {keys} key value(s) but 'by' supplies {supplied}", source.Location);
            }

            if (query.ReturnType.Name != provide.Type.Name || query.ReturnType.IsCollection != provide.Type.IsCollection)
            {
                context.Error(DiagnosticCodes.ProvideQueryTypeMismatch, $"Provided value '{provide.Name}' is {Describe(provide.Type)} but query '{source.Query}' returns {Describe(query.ReturnType)}", source.Location);
            }
        }

        void ValidateCycles(Dictionary<string, ProvideSyntax> effective)
        {
            foreach (var start in effective.Values)
            {
                var path = new List<string>();
                if (Reaches(effective, start.Name, start.Name, path, []) && _cycles.Add(start.Location))
                {
                    context.Error(DiagnosticCodes.ProvideCycle, $"Provided value '{start.Name}' reads itself through {string.Join(" -> ", path.Prepend(start.Name))}", start.Location);
                }
            }
        }
    }
}

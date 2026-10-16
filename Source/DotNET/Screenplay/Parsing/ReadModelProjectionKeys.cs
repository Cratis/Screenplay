// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.Parsing;

internal static class ReadModelProjectionKeys
{
    internal static void Validate(ApplicationSyntax application, ConsistencyDeclarations declarations, ParserContext context)
    {
        foreach (var (slice, scope) in declarations.Slices)
        {
            foreach (var projection in slice.Projections)
            {
                var keys = declarations.ViewKey(projection.ReadModel ?? projection.Name, scope);
                if (keys.Length < 2) continue;
                foreach (var block in projection.Blocks)
                {
                    switch (block)
                    {
                        case FromSyntax from:
                            foreach (var spec in from.Events) Check(from.Key, spec.Key is not null || from.ParentKey is not null, spec.Event, from.Location);
                            break;
                        case RemoveWithSyntax removal:
                            Check(removal.PartsKey, removal.Key is not null || removal.ParentKey is not null, removal.Event, removal.Location);
                            break;
                    }
                }

                void Check(KeySyntax? key, bool scalarOrParent, string eventName, SourceLocation location)
                {
                    if (scalarOrParent || key is not CompositeKeySyntax composite)
                    {
                        context.Error(DiagnosticCodes.IncompleteReadModelKey, "A composite read-model projection requires an untyped block naming every key part.", location);
                        return;
                    }
                    if (!string.IsNullOrEmpty(composite.Type)) Invalid("Flat read-model key parts have no authored composite type.", composite.Location);
                    var seen = new HashSet<string>(StringComparer.Ordinal);
                    var sources = declarations.Event(eventName, scope)?.Properties ?? [];
                    foreach (var part in composite.Parts)
                    {
                        var target = keys.SingleOrDefault(property => property.Name == part.Property);
                        if (!seen.Add(part.Property) || target is null || !Compatible(part.Expression, target.Type, sources))
                        {
                            Invalid($"Projection key part '{part.Property}' must name one key property exactly once and use a compatible simple expression.", part.Location);
                        }
                    }
                    var missing = keys.Where(property => !seen.Contains(property.Name)).Select(property => property.Name).ToArray();
                    if (missing.Length > 0) context.Error(DiagnosticCodes.IncompleteReadModelKey, $"Projection key is missing parts: {string.Join(", ", missing)}.", composite.Location);
                }
            }
        }

        bool Compatible(ExpressionSyntax expression, TypeRefSyntax target, IEnumerable<PropertySyntax> sources)
        {
            if (expression is PathExpressionSyntax path)
            {
                var segments = path.Path.Split('.');
                PropertySyntax? property = null;
                foreach (var segment in segments)
                {
                    var matches = sources.Where(property => property.Name == segment).ToArray();
                    if (matches.Length != 1 || matches[0].Type.IsOptional || matches[0].Type.IsCollection) return false;
                    property = matches[0];
                    sources = declarations.TypeProperties(property.Type.Name) ?? [];
                }
                return property is not null && declarations.Compatible(property.Type, target) != false;
            }
            if (expression is EventSourceIdExpressionSyntax) return true;
            if (expression is EventContextExpressionSyntax eventContext)
            {
                var resolved = EventContextCatalog.Resolve(eventContext.Path);
                if (!resolved.IsKnown) return false;
                if (resolved.Members[0].Name == "eventSourceId") return true;
                var member = resolved.Members[^1];
                var underlying = EventContextCatalog.MembersOf(member);
                var type = underlying.Count == 1 && underlying[0].Name == "value" ? underlying[0].Type : member.Type;
                return Category(type) == Category(Primitive(target));
            }
            if (expression is not LiteralExpressionSyntax literal) return false;
            var enumeration = declarations.Enumeration(target);
            if (enumeration is not null) return literal.Value is string memberName && enumeration.Values.Contains(memberName, StringComparer.Ordinal);
            return Category(Primitive(target)) switch
            {
                "text" => literal.Value is string,
                "uuid" => literal.Value is string uuid && Guid.TryParse(uuid, out _),
                "number" => literal.Value is byte or short or int or long or float or double or decimal,
                "boolean" => literal.Value is bool,
                "date" => literal.Value is string date && DateTimeOffset.TryParse(date, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind, out _),
                _ => false
            };
        }

        string Primitive(TypeRefSyntax type)
        {
            var concept = application.Concepts.SingleOrDefault(concept => concept.Name == type.Name);

            return concept?.IsEnum == true ? "String" : concept?.Type ?? type.Name;
        }
        void Invalid(string message, SourceLocation location) => context.Error(DiagnosticCodes.InvalidReadModelKeyLookup, message, location);
    }

    static string Category(string type) => type.ToLowerInvariant() switch
    {
        "string" => "text",
        "uuid" or "guid" => "uuid",
        "bool" or "boolean" => "boolean",
        "date" or "datetime" or "datetimeoffset" => "date",
        "byte" or "short" or "int" or "int32" or "int64" or "long" or "uint" or "ulong" or "float" or "double" or "decimal" or "number" => "number",
        _ => type
    };
}

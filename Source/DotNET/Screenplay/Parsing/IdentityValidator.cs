// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Resolves caller metadata against the assembled application, never against one physical file.
/// </summary>
internal static class IdentityValidator
{
    internal static void Validate(ApplicationSyntax application, HashSet<string> knownTypes, ParserContext context)
    {
        var details = (application.Identity?.Details ?? []).ToArray();
        var names = details.Select(detail => detail.Name).ToHashSet(StringComparer.Ordinal);
        new IdentityPathValidator(names, context).VisitApplication(application);
        foreach (var duplicate in details.GroupBy(detail => detail.Name, StringComparer.Ordinal).SelectMany(group => group.Skip(1)))
        {
            context.Error(DiagnosticCodes.DuplicateIdentityDetail, $"Duplicate identity detail '{duplicate.Name}' - detail names must be unique", duplicate.Location);
        }

        var slices = ScreenplayValidator.ScopedSlices(application).ToArray();
        var declarations = new ConsistencyDeclarations(application, slices);
        var readModels = slices.SelectMany(entry => (entry.Slice.ReadModels ?? []).Select(model => model.Name)).ToHashSet(StringComparer.Ordinal);
        foreach (var detail in details)
        {
            if (ContextExpressionSyntax.KnownIdentityProperties.Contains(detail.Name))
            {
                context.Error(DiagnosticCodes.BuiltInIdentityDetail, $"Identity detail '{detail.Name}' redeclares a built-in caller property", detail.Location);
            }

            if (!knownTypes.Contains(detail.Type.Name) && !readModels.Contains(detail.Type.Name))
            {
                context.Warning(DiagnosticCodes.UnknownType, $"Unknown type '{detail.Type.Name}' on identity detail '{detail.Name}'", detail.Type.Location);
            }

            if (detail.Source is not QueryIdentitySourceSyntax source) continue;
            if (!IsTokenKey(source.By))
            {
                context.Error(DiagnosticCodes.InvalidIdentityQueryKey, $"Identity detail '{detail.Name}' query key may reference only caller built-ins, claims or literals, never another detail", source.By.Location);
            }

            if (declarations.Resolve(source.Query, new([]), slice => slice.Queries, query => query.Name) is not { } resolved)
            {
                context.Error(DiagnosticCodes.UnknownIdentityQuery, $"Unknown or ambiguous identity query '{source.Query}'", source.Location);
                continue;
            }

            var query = resolved.Node;
            if (query.By is null || query.ReturnType.IsCollection)
            {
                context.Error(DiagnosticCodes.InvalidIdentityQuery, $"Identity query '{source.Query}' must be keyed and return a single, possibly optional result", source.Location);
            }

            if (query.ReturnType.Name != detail.Type.Name || detail.Type.IsCollection || (query.ReturnType.IsOptional && !detail.Type.IsOptional))
            {
                context.Error(DiagnosticCodes.IdentityQueryTypeMismatch, $"Identity detail '{detail.Name}' must match query '{source.Query}' result type, including optionality", detail.Type.Location);
            }

            var dependencies = new DetailReferences(names);
            foreach (var authorization in Authorizations(application, resolved.Scope, query))
            {
                foreach (var reference in authorization.References())
                {
                    foreach (var policy in application.Policies.Where(policy => policy.Name == reference.Name)) dependencies.VisitPolicy(policy);
                }
            }

            if (dependencies.Found.Count > 0)
            {
                context.Error(DiagnosticCodes.IdentityQueryAuthorizationDependency, $"Identity query '{source.Query}' authorization depends on identity detail '{dependencies.Found[0].Path.Split('.')[0]}' - caller details cannot authorize their own resolution", source.Location);
            }
        }
    }

    internal static bool IsTokenKey(ExpressionSyntax expression) => expression switch
    {
        LiteralExpressionSyntax => true,
        IdentityExpressionSyntax identity => ContextExpressionSyntax.KnownIdentityProperties.Contains(identity.Path.Split('.')[0]),
        ContextExpressionSyntax context when context.Path.StartsWith("identity.", StringComparison.Ordinal) => ContextExpressionSyntax.KnownIdentityProperties.Contains(context.Path.Split('.')[1]),
        _ => false
    };

    static IEnumerable<AuthorizeSyntax> Authorizations(ApplicationSyntax application, DeclarationScope scope, QuerySyntax query)
    {
        if (query.Authorize is not null) yield return query.Authorize;
        foreach (var module in application.Modules.Where(module => scope.Segments.Count > 0 && module.Name == scope.Segments[0]))
        {
            if (module.Authorize is not null) yield return module.Authorize;
            foreach (var authorization in FeatureAuthorizations(module.Features, [.. scope.Segments.Skip(1)])) yield return authorization;
        }
    }

    static IEnumerable<AuthorizeSyntax> FeatureAuthorizations(IEnumerable<FeatureSyntax> features, string[] segments)
    {
        if (segments.Length == 0) yield break;
        foreach (var feature in features.Where(feature => feature.Name == segments[0]))
        {
            if (feature.Authorize is not null) yield return feature.Authorize;
            foreach (var authorization in FeatureAuthorizations(feature.Features, [.. segments.Skip(1)])) yield return authorization;
        }
    }

    internal sealed class DetailReferences(IReadOnlySet<string> names) : ScreenplaySyntaxWalker
    {
        internal List<IdentityExpressionSyntax> Found { get; } = [];

        public override void VisitIdentityExpression(IdentityExpressionSyntax syntax)
        {
            if (names.Contains(syntax.Path.Split('.')[0])) Found.Add(syntax);
        }
    }
}

/// <summary>
/// Checks the caller root after all detail declarations are known.
/// </summary>
internal sealed class IdentityPathValidator(IReadOnlySet<string> details, ParserContext context) : ScreenplaySyntaxWalker
{
    public override void VisitIdentityExpression(IdentityExpressionSyntax syntax)
    {
        var property = syntax.Path.Split('.')[0];
        if (!details.Contains(property) && !ContextExpressionSyntax.KnownIdentityProperties.Contains(property))
        {
            context.Warning(
                DiagnosticCodes.UnknownContextIdentityProperty,
                $"Unknown $identity property '{property}' - expected {string.Join(", ", ContextExpressionSyntax.KnownIdentityProperties.Concat(details))}",
                syntax.Location);
        }
    }
}

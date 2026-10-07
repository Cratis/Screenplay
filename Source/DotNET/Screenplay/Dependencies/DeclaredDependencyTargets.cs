// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Parsing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Dependencies;

/// <summary>
/// Resolves declared dependencies against container children sets, never cousins.
/// </summary>
internal static class DeclaredDependencyTargets
{
    internal static ReferenceResolver.Resolution Resolve(string reference, DeclarationScope from, IReadOnlyList<Declaration> declarations)
    {
        var segments = reference.Split('.');
        if (segments.Length > 1)
        {
            var matches = declarations.Where(declaration => declaration.Name == segments[^1] && declaration.Scope.EndsWith(segments[..^1])).ToArray();
            return matches.Length == 1 ? new(matches[0], []) : new(null, matches);
        }

        for (var depth = from.Depth - 1; depth >= 0; depth--)
        {
            var matches = declarations.Where(declaration => declaration.Name == reference && declaration.Scope.Depth == depth && declaration.Scope.Segments.SequenceEqual(from.Segments.Take(depth))).ToArray();
            if (matches.Length > 0) return matches.Length == 1 ? new(matches[0], []) : new(null, matches);
        }

        return new(null, []);
    }

    internal static ApplicationSyntax Validate(ApplicationSyntax application, ParserContext context)
    {
        var declarations = new List<Declaration>();
        var hasDependencies = false;
        foreach (var module in application.Modules)
        {
            declarations.Add(new(module.Name, new([])));
            hasDependencies |= module.DependsOn.Any();
            foreach (var feature in module.Features) Inventory(feature, [module.Name]);
        }

        if (!hasDependencies) return application;
        declarations = [.. declarations.DistinctBy(declaration => (declaration.Name, Scope: string.Join('.', declaration.Scope.Segments)))];

        return application with
        {
            Modules = [.. application.Modules.Select(module => module with
            {
                DependsOn = Keep(module.DependsOn, [module.Name]),
                Features = [.. module.Features.Select(feature => Normalize(feature, [module.Name]))]
            })]
        };

        void Inventory(FeatureSyntax feature, string[] parent)
        {
            declarations.Add(new(feature.Name, new(parent)));
            hasDependencies |= feature.DependsOn.Any();
            foreach (var child in feature.Features) Inventory(child, [.. parent, feature.Name]);
        }

        FeatureSyntax Normalize(FeatureSyntax feature, string[] parent)
        {
            string[] address = [.. parent, feature.Name];
            return feature with
            {
                DependsOn = Keep(feature.DependsOn, address),
                Features = [.. feature.Features.Select(child => Normalize(child, address))]
            };
        }

        DependsOnSyntax[] Keep(IEnumerable<DependsOnSyntax> dependencies, string[] address)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var kept = new List<DependsOnSyntax>();
            var owner = string.Join('.', address);
            foreach (var dependency in dependencies)
            {
                var resolution = Resolve(dependency.Target, new(address), declarations);
                var target = resolution.Resolved is { } resolved ? string.Join('.', resolved.Scope.Segments.Append(resolved.Name)) : null;
                var key = target is null ? $"text:{dependency.Target}" : $"resolved:{target}";
                if (!seen.Add(key))
                {
                    context.Warning(DiagnosticCodes.RepeatedDependencyDeclaration, $"Dependency '{dependency.Target}' is already declared on this container - this repeated declaration is ignored", dependency.Location);
                    continue;
                }

                kept.Add(dependency);
                if (resolution.Ambiguous.Count > 0)
                {
                    var candidates = string.Join(", ", resolution.Ambiguous.Select(candidate => string.Join('.', candidate.Scope.Segments.Append(candidate.Name))));
                    context.Warning(DiagnosticCodes.AmbiguousReference, $"Ambiguous dependency target '{dependency.Target}' - candidates: {candidates}", dependency.Location);
                }
                else if (target is null || target == owner || target.StartsWith(owner + ".", StringComparison.Ordinal) || owner.StartsWith(target + ".", StringComparison.Ordinal))
                {
                    context.Warning(DiagnosticCodes.InvalidDependencyTarget, $"Invalid dependency target '{dependency.Target}' on '{owner}' - the target must resolve to another module or feature, not self, an ancestor, or a descendant", dependency.Location);
                }
            }

            return [.. kept];
        }
    }
}

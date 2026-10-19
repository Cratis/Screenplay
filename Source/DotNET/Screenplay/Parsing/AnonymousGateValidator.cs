// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Holds an anonymous policy to being the whole effective authorization of every command and query it gates.
/// </summary>
/// <remarks>
/// The reference semantics AND every gate on a command's path, while a generated application lets
/// <c>[AllowAnonymous]</c> override <c>[Authorize]</c>. Refusing an anonymous gate next to any other gate keeps the
/// two in agreement.
/// </remarks>
internal static class AnonymousGateValidator
{
    internal static void Validate(ApplicationSyntax application, ParserContext context)
    {
        var anonymous = application.Policies
            .Where(policy => policy is { Condition: AnonymousConditionSyntax, Code: null, File: null })
            .Select(policy => policy.Name)
            .ToHashSet(StringComparer.Ordinal);
        if (anonymous.Count == 0)
        {
            return;
        }

        foreach (var module in application.Modules)
        {
            ValidateFeatures(module.Features, [module.Authorize], anonymous, context);
        }

        foreach (var authorize in Gates(application).Where(authorize => authorize.Requirement is not PolicyReferenceSyntax && authorize.References().Any(policy => anonymous.Contains(policy.Name))))
        {
            context.Error(DiagnosticCodes.AnonymousPolicyInCompositeGate, "An anonymous policy admits every caller and must be the whole 'authorize' line; do not combine it with another policy", authorize.Location);
        }
    }

    static void ValidateFeatures(IEnumerable<FeatureSyntax> features, IReadOnlyList<AuthorizeSyntax?> enclosing, HashSet<string> anonymous, ParserContext context)
    {
        foreach (var feature in features)
        {
            List<AuthorizeSyntax?> path = [.. enclosing, feature.Authorize];
            foreach (var slice in feature.Slices)
            {
                foreach (var command in slice.Commands)
                {
                    ValidatePath(command.Name, [.. path, command.Authorize], command.Location, anonymous, context);
                }

                foreach (var query in slice.Queries)
                {
                    ValidatePath(query.Name, [.. path, query.Authorize], query.Location, anonymous, context);
                }
            }

            ValidateFeatures(feature.Features, path, anonymous, context);
        }
    }

    static void ValidatePath(string artifact, IReadOnlyList<AuthorizeSyntax?> path, SourceLocation location, HashSet<string> anonymous, ParserContext context)
    {
        var gates = path.OfType<AuthorizeSyntax>().ToArray();
        if (gates.Length < 2 || !gates.Any(gate => gate.References().Any(policy => anonymous.Contains(policy.Name))))
        {
            return;
        }

        context.Error(
            DiagnosticCodes.AnonymousPolicyUnderEnclosingGate,
            $"'{artifact}' is gated by an anonymous policy and by another 'authorize' on its module, feature or itself; an anonymous policy must be the only gate on the path",
            path[^1]?.Location ?? location);
    }

    static IEnumerable<AuthorizeSyntax> Gates(ApplicationSyntax application)
    {
        foreach (var module in application.Modules)
        {
            if (module.Authorize is not null) yield return module.Authorize;
            foreach (var gate in Gates(module.Features))
            {
                yield return gate;
            }
        }
    }

    static IEnumerable<AuthorizeSyntax> Gates(IEnumerable<FeatureSyntax> features)
    {
        foreach (var feature in features)
        {
            if (feature.Authorize is not null) yield return feature.Authorize;
            foreach (var slice in feature.Slices)
            {
                foreach (var command in slice.Commands.Where(command => command.Authorize is not null))
                {
                    yield return command.Authorize!;
                }

                foreach (var query in slice.Queries.Where(query => query.Authorize is not null))
                {
                    yield return query.Authorize!;
                }
            }

            foreach (var gate in Gates(feature.Features))
            {
                yield return gate;
            }
        }
    }
}

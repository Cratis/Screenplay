// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Mcp;

sealed class McpAuthoringReadiness(ApplicationSyntax application)
{
    readonly AuthoringProductionResolver _productions = new(application);
    readonly Dictionary<SyntaxNode, SliceSyntax> _owners = Owners(application);
    readonly Dictionary<SyntaxNode, bool> _operations = new(ReferenceEqualityComparer.Instance);
    readonly Dictionary<SliceSyntax, string[]> _scopes = Scopes(application);
    readonly ILookup<string, (CommandSyntax Command, string[] Scope)> _commands = Scopes(application)
        .SelectMany(entry => entry.Key.Commands.Select(command => (Command: command, Scope: entry.Value))).ToLookup(entry => entry.Command.Name, StringComparer.Ordinal);
    readonly ILookup<string, ImportSyntax> _imports = application.Imports.ToLookup(import => import.Name, StringComparer.Ordinal);

    // Member readiness describes its own constructs and referenced command actions, not unrelated
    // application declarations. Model readiness separately includes every physical declaration.
    internal bool ModelSyntaxOnly => SyntaxOnly(application);

    internal string? ModelExecutionReadiness => ExecutionReadiness(application);

    internal bool SyntaxOnly(SyntaxNode node) => RequiredVersion(node) > 0;

    internal string? ExecutionReadiness(SyntaxNode node, string? suffix = "use Authoring validation.") =>
        RequiredVersion(node) is var version && version > 0 ? $"Unavailable until ESM v{version} (PLAY0268){(suffix is null ? "." : $"; {suffix}")}" : null;

    internal IEnumerable<string> ProducedEvents(CommandSyntax command) => command.Produces
        .Where(production => _owners.TryGetValue(command, out var slice) && _productions.IsEventProduction(production, slice))
        .Select(production => production.Event);

    static Dictionary<SyntaxNode, SliceSyntax> Owners(ApplicationSyntax application)
    {
        var owners = new Dictionary<SyntaxNode, SliceSyntax>(ReferenceEqualityComparer.Instance);
        void Feature(FeatureSyntax feature)
        {
            foreach (var slice in feature.Slices)
            {
                owners[slice] = slice;
                foreach (var command in slice.Commands) owners[command] = slice;
                foreach (var reaction in slice.Reactions) owners[reaction] = slice;
                foreach (var specification in slice.Specifications) owners[specification] = slice;
            }
            foreach (var nested in feature.Features) Feature(nested);
        }
        foreach (var module in application.Modules)
        {
            foreach (var feature in module.Features) Feature(feature);
        }

        return owners;
    }

    static Dictionary<SliceSyntax, string[]> Scopes(ApplicationSyntax application)
    {
        var scopes = new Dictionary<SliceSyntax, string[]>(ReferenceEqualityComparer.Instance);
        void Feature(FeatureSyntax feature, string[] parent)
        {
            string[] scope = [.. parent, feature.Name];
            foreach (var slice in feature.Slices) scopes[slice] = [.. scope, slice.Name];
            foreach (var nested in feature.Features) Feature(nested, scope);
        }
        foreach (var module in application.Modules)
        {
            foreach (var feature in module.Features) Feature(feature, [module.Name]);
        }

        return scopes;
    }

    int RequiredVersion(SyntaxNode node)
    {
        var local = node switch
        {
            EventSourceSyntax or EventStreamSyntax or CommandStreamSyntax => 10,
            CommandSyntax { Stream: not null } => 10,
            CommandSyntax command when command.Response is not null || command.Properties.Any(property => property.IsGenerated) => 8,
            SpecificationSyntax specification => Math.Max(
                specification.ThenReturns is not null || (specification.When?.GeneratedValues.Any() ?? false) ? 8 : 0,
                ActionCommands(specification).Select(entry => RequiredVersion(entry.Command)).DefaultIfEmpty().Max()),
            SliceSyntax slice => slice.Commands.Cast<SyntaxNode>().Concat(slice.Specifications).Select(RequiredVersion).DefaultIfEmpty().Max(),
            ApplicationSyntax => Math.Max(application.EventSources.Any() ? 10 : 0, _owners.Keys.OfType<SliceSyntax>().Select(RequiredVersion).DefaultIfEmpty().Max()),
            _ => 0
        };

        return Math.Max(local, Operations(node) ? 9 : 0);
    }

    (CommandSyntax Command, string[] Scope)[] ActionCommands(SpecificationSyntax specification)
    {
        if (specification.When is null || !_owners.TryGetValue(specification, out var slice)) return [];
        var from = _scopes[slice];
        (CommandSyntax Command, string[] Scope)[] Candidates(string reference)
        {
            var segments = reference.Split('.');
            var named = _commands[segments[^1]].ToArray();
            var qualifiers = segments[..^1];
            if (qualifiers.Length > 0)
                return [.. named.Where(entry => qualifiers.Length <= entry.Scope.Length && entry.Scope.Skip(entry.Scope.Length - qualifiers.Length).SequenceEqual(qualifiers, StringComparer.Ordinal))];
            for (var depth = from.Length; depth >= 0; depth--)
            {
                var visible = named.Where(entry => depth <= entry.Scope.Length && from.Take(depth).SequenceEqual(entry.Scope.Take(depth), StringComparer.Ordinal)).ToArray();
                if (visible.Length > 0) return visible;
            }

            return [];
        }
        var candidates = Candidates(specification.When.CommandType);
        if (candidates.Length == 0 && _imports[specification.When.CommandType].ToArray() is [var imported]) candidates = Candidates(imported.QualifiedName);

        return candidates;
    }

    bool Operations(SyntaxNode node)
    {
        if (_operations.TryGetValue(node, out var cached)) return cached;
        var result = node switch
        {
            SystemSyntax or OperationSyntax => true,
            ApplicationSyntax => application.Systems.Any() || _owners.Keys.OfType<SliceSyntax>().Any(Operations),
            CommandSyntax command => _owners.TryGetValue(command, out var slice) && command.Produces.Any(production => !_productions.IsEventProduction(production, slice)),
            SpecificationSyntax specification => specification.GivenOperationFailures.Any() || specification.ThenOperations.Any() || specification.ThenCompensated.Any() || ActionCommands(specification).Any(entry => Operations(entry.Command)),
            SliceSyntax slice => OperationDeclarations.In(slice).Any() || slice.Commands.Any(Operations) || slice.Specifications.Any(Operations) || slice.Reactions.SelectMany(reaction => reaction.Triggers).SelectMany(trigger => trigger.Produces ?? []).Any(production => !_productions.IsEventProduction(production, slice)),
            _ => false
        };
        _operations[node] = result;

        return result;
    }
}

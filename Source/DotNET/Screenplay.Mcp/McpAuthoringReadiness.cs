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
    readonly ILookup<string, SliceSyntax> _slicesByScope = Scopes(application).ToLookup(entry => string.Join('\0', entry.Value), entry => entry.Key, StringComparer.Ordinal);
    readonly ILookup<string, (CommandSyntax Command, string[] Scope)> _commands = Scopes(application)
        .SelectMany(entry => entry.Key.Commands.Select(command => (Command: command, Scope: entry.Value))).ToLookup(entry => entry.Command.Name, StringComparer.Ordinal);
    readonly ILookup<string, ImportSyntax> _imports = application.Imports.ToLookup(import => import.Name, StringComparer.Ordinal);

    internal bool SyntaxOnly(SyntaxNode node) => Operations(node) || node switch
    {
        CommandSyntax command => command.Response is not null || command.Properties.Any(property => property.IsGenerated),
        SpecificationSyntax specification => specification.ThenReturns is not null || (specification.When?.GeneratedValues.Any() ?? false),
        SliceSyntax slice => slice.Commands.Any(SyntaxOnly) || slice.Specifications.Any(SyntaxOnly),
        _ => false
    };

    internal string? ExecutionReadiness(SyntaxNode node, string? suffix = "use Authoring validation.") =>
        SyntaxOnly(node) ? $"Unavailable until ESM v{(Operations(node) ? 9 : 8)} (PLAY0268){(suffix is null ? "." : $"; {suffix}")}" : null;

    internal IEnumerable<string> ProducedEvents(CommandSyntax command) => command.Produces
        .Where(production => _owners.TryGetValue(command, out var slice) && _productions.IsEventProduction(production, slice))
        .Select(production => production.Event);

    internal AuthoringProductionResolution ResolveProduction(string reference, string[] scope) =>
        _slicesByScope[string.Join('\0', scope)].ToArray() is [var slice]
            ? _productions.Resolve(reference, slice) : new(AuthoringProductionKind.Unresolved, null, []);

    internal string[] ProductionKinds(ProducesSyntax production, SyntaxNode? owner)
    {
        if (owner is null || !_owners.TryGetValue(owner, out var slice)) return ["Event", "Operation"];
        var resolution = _productions.Resolve(production.Event, slice);

        return resolution.Kind switch
        {
            AuthoringProductionKind.Operation => ["Operation"],
            AuthoringProductionKind.Event => ["Event"],
            _ => ["Event", "Operation"]
        };
    }

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

    bool ActionOperations(SpecificationSyntax specification)
    {
        if (specification.When is null || !_owners.TryGetValue(specification, out var slice)) return false;
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

        return candidates.Any(entry => Operations(entry.Command));
    }

    bool Operations(SyntaxNode node)
    {
        if (_operations.TryGetValue(node, out var cached)) return cached;
        var result = node switch
        {
            SystemSyntax or OperationSyntax => true,
            ApplicationSyntax => application.Systems.Any() || _owners.Keys.OfType<SliceSyntax>().Any(Operations),
            CommandSyntax command => _owners.TryGetValue(command, out var slice) && command.Produces.Any(production => !_productions.IsEventProduction(production, slice)),
            SpecificationSyntax specification => specification.GivenOperationFailures.Any() || specification.ThenOperations.Any() || specification.ThenCompensated.Any() || ActionOperations(specification),
            SliceSyntax slice => OperationDeclarations.In(slice).Any() || slice.Commands.Any(Operations) || slice.Specifications.Any(Operations) || slice.Reactions.SelectMany(reaction => reaction.Triggers).SelectMany(trigger => trigger.Produces ?? []).Any(production => !_productions.IsEventProduction(production, slice)),
            _ => false
        };
        _operations[node] = result;

        return result;
    }
}

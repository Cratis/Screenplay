// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Mcp;

sealed class McpAuthoringReadiness(ApplicationSyntax application)
{
    readonly AuthoringProductionResolver _productions = new(application);
    readonly EffectiveSpecificationApplication _effective = SpecificationExamples.Expand(application);
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

    internal bool SyntaxOnly(SyntaxNode node) => UnadmittedFeatures(node).Length > 0;

    internal string? ExecutionReadiness(SyntaxNode node, string? suffix = "use Authoring validation.") =>
        UnadmittedFeatures(node) is var features && features.Length > 0 ? $"Not admitted by any supported executable model (ESM) version yet (PLAY0268): {string.Join(", ", features)}{(suffix is null ? "." : $"; {suffix}")}" : null;

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

    static IEnumerable<string> Feature(bool present, string feature) => present ? [feature] : [];

    static bool ReactionRefusals(SyntaxNode node)
    {
        var walker = new RefusalReadinessWalker();
        switch (node)
        {
            case ApplicationSyntax application: walker.VisitApplication(application); break;
            case SliceSyntax slice: walker.VisitSlice(slice); break;
            case ReactionSyntax reaction: walker.VisitReaction(reaction); break;
            case ReactionTriggerSyntax trigger: walker.VisitReactionTrigger(trigger); break;
            case InvokesSyntax invocation: walker.VisitInvokes(invocation); break;
            case CommandSyntax command: walker.VisitCommand(command); break;
            case SpecificationSyntax specification: walker.VisitSpecification(specification); break;
            case SpecificationExampleSyntax example: walker.VisitSpecificationExample(example); break;
            default: walker.VisitNode(node); break;
        }

        return walker.Unadmitted;
    }

    bool GeneratedConceptRules(CommandSyntax command) => command.Properties
        .Where(property => property.IsGenerated)
        .SelectMany(property => application.Concepts.Where(concept => concept.Name == property.Type.Name))
        .Any(concept => (concept.Validations ?? []).Any(validation => validation is CodeValidateSyntax ||
            (validation is DeclarativeValidateSyntax declarative && (declarative.Rules.Any() || (declarative.Requirements ?? []).Any()))));

    string[] UnadmittedFeatures(SyntaxNode node)
    {
        const string streams = "property-path stream id mappings (#407)";
        IEnumerable<string> RouteFeatures(CommandStreamSyntax route) => Feature(
            new[] { route.StreamId?.Source }.Concat(route.StreamIdParts.Select(part => part.Source))
                .Any(source => source is PathExpressionSyntax path && path.Path.Contains('.', StringComparison.Ordinal)),
            streams)
            .Concat(Feature(!SemanticModelBinder.CompositeStreamIdsJoin && route.StreamIdParts.Any(), "composite stream ids (#462)"));
        IEnumerable<string> FixtureFeatures(SpecificationStreamSyntax? route, SpecificationNoStreamSyntax? unrouted) =>
            Feature(!SemanticModelBinder.SpecificationRoutesJoin && (route is not null || unrouted is not null), "specification event routes (#457)")
                .Concat(Feature(!SemanticModelBinder.CompositeStreamIdsJoin && route?.StreamIdParts.Any() == true, "composite stream ids (#462)"));
        IEnumerable<string> LocalFeatures() => node switch
        {
            ReadModelSyntax model => Feature(model.Properties.Count(property => property.IsKey) > 1, "composite read-model keys (#599)"),
            QuerySyntax query => Feature(query.ByParts.Any(), "composite read-model keys (#599)"),
            EventSourceSyntax source => source.Streams.SelectMany(UnadmittedFeatures),
            EventStreamSyntax stream => Feature(!SemanticModelBinder.CompositeStreamIdsJoin && stream.StreamIdParts.Any(), "composite stream ids (#462)"),
            EventStreamIdPartSyntax => Feature(!SemanticModelBinder.CompositeStreamIdsJoin, "composite stream ids (#462)"),
            CommandStreamSyntax route => RouteFeatures(route),
            SpecificationStreamSyntax route => FixtureFeatures(route, null),
            SpecificationNoStreamSyntax unrouted => FixtureFeatures(null, unrouted),
            SpecificationExampleSyntax example => FixtureFeatures(example.Stream, example.NoStream),
            CommandSyntax command =>
                (command.Stream is { } route ? RouteFeatures(route) : [])
                .Concat(Feature(command.Handler is not null, "command handlers"))
                .Concat(Feature(GeneratedConceptRules(command), "generated properties on concepts with validation rules")),
            SpecificationSyntax specification =>
                Feature(specification.ThenNoEvents, "explicit no-event assertions (#433)")
                .Concat(specification.Given.Concat(specification.ThenEvents)
                    .Concat(specification.WhenAppended is { } appended ? [appended] : [])
                    .SelectMany(occurrence => FixtureFeatures(occurrence.Stream, occurrence.NoStream)))
                .Concat(Feature(!SemanticModelBinder.SpecificationRoutesJoin && HasSpecificationRoute(specification), "specification event routes (#457)"))
                .Concat(ActionCommands(specification).SelectMany(entry => UnadmittedFeatures(entry.Command))),
            SliceSyntax slice => slice.Commands.Cast<SyntaxNode>().Concat(slice.ReadModels ?? []).Concat(slice.Queries).Concat(slice.Specifications).Concat(slice.Examples).SelectMany(UnadmittedFeatures),
            ApplicationSyntax => application.EventSources.SelectMany(UnadmittedFeatures)
                .Concat(_owners.Keys.OfType<SliceSyntax>().SelectMany(UnadmittedFeatures))
                .Concat(_effective.ResolvedExamples.Select(example => example.Example).SelectMany(UnadmittedFeatures)),
            _ => []
        };

        return [.. LocalFeatures().Concat(Feature(ReactionRefusals(node), "reaction refusal handling and redelivery (#433)"))
            .Concat(Feature(Operations(node), "operations and systems (#301)"))
            .Concat(Feature(application.SourceOptions.NumericMode == NumericMode.Exact, "exact numbers (#285)"))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(feature => feature switch
            {
                "exact numbers (#285)" => 0,
                "operations and systems (#301)" => 2,
                streams => 3,
                _ => 4
            })];
    }

    bool HasSpecificationRoute(SpecificationSyntax specification)
    {
        var effective = _effective.Specifications.FirstOrDefault(pair => ReferenceEquals(pair.Authored, specification))?.Effective ?? specification;

        return effective.Given.Concat(effective.ThenEvents).Concat(effective.WhenAppended is { } appended ? [appended] : [])
            .Any(occurrence => occurrence.Stream is not null || occurrence.NoStream is not null) ||
            (effective.WhenRedelivered is { } locator && (locator.Stream is not null || locator.NoStream is not null));
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
        var command = _effective.Specifications.FirstOrDefault(value => ReferenceEquals(value.Authored, specification))?.Effective.When?.CommandType ?? specification.When.CommandType;
        var candidates = Candidates(command);
        if (candidates.Length == 0 && _imports[command].ToArray() is [var imported]) candidates = Candidates(imported.QualifiedName);

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

    sealed class RefusalReadinessWalker : ScreenplaySyntaxWalker
    {
        internal bool Unadmitted { get; private set; }

        public override void VisitNode(SyntaxNode node)
        {
            if (node is InvocationRefusalSyntax or RefusalExpressionSyntax or SpecificationRedeliverySyntax) Unadmitted = true;
        }
    }
}

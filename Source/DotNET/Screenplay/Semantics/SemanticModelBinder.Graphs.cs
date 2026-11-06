// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Parsing;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Semantics;

public sealed partial class SemanticModelBinder
{
    private sealed partial class BindingContext
    {
        HashSet<string>? _graphQueryNames;

        HashSet<string> GraphQueryNames => _graphQueryNames ??= ScreenplayValidator.ScopedSlices(syntax).SelectMany(entry => entry.Slice.GraphQueries).Select(query => query.Name).ToHashSet(StringComparer.Ordinal);

        bool HasGraphStep(SpecificationSyntax specification) =>
            specification.GivenGraphs.Any() || specification.ThenGraphs.Any() || specification.ThenQueries.Any(query => GraphQueryNames.Contains(query.Query));

        (ImmutableArray<SemanticGraph> Graphs, ImmutableArray<SemanticGraphProjection> Projections, ImmutableArray<SemanticGraphSpecification> Specifications, ImmutableArray<SemanticGraphQuery> Queries) BindGraphs(ImmutableArray<SemanticModule> modules)
        {
            var scoped = ScreenplayValidator.ScopedSlices(syntax).ToList();
            var slices = scoped.Select(entry => entry.Slice).ToList();
            if (!slices.Exists(slice => slice.Graphs.Any() || slice.GraphProjections.Any() || slice.GraphQueries.Any() || slice.Specifications.Any(HasGraphStep))) return ([], [], [], []);
            var graphs = slices.SelectMany(slice => slice.Graphs).Select(BindGraph).OfType<SemanticGraph>().ToImmutableArray();
            var byName = graphs.GroupBy(graph => graph.Name, StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
            var projections = slices.SelectMany(slice => slice.GraphProjections).Select(projection => BindGraphProjection(projection, byName)).OfType<SemanticGraphProjection>().ToImmutableArray();
            var commands = modules.SelectMany(module => module.Features).SelectMany(AllBoundSlices).SelectMany(slice => slice.Commands)
                .GroupBy(command => command.Name, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
            var specifications = slices
                .SelectMany(slice => slice.Specifications.Where(HasGraphStep).Select(specification => BindGraphSpecification(slice, specification, byName, commands)))
                .OfType<SemanticGraphSpecification>().ToImmutableArray();
            var queries = scoped
                .SelectMany(entry => entry.Slice.GraphQueries.Select(query => BindGraphQuery(query, entry.Scope, byName)))
                .OfType<SemanticGraphQuery>().ToImmutableArray();
            return (graphs, projections, specifications, queries);
        }

        SemanticGraphQuery? BindGraphQuery(GraphQuerySyntax query, DeclarationScope scope, Dictionary<string, SemanticGraph> graphs)
        {
            if (!graphs.ContainsKey(query.Graph))
            {
                Error(DiagnosticCodes.InvalidSemanticBinding, $"Graph query '{query.Name}' graph '{query.Graph}' is unresolved.", query.Location);
                return null;
            }

            if (!_readModels.TryGetValue(query.ReturnType.Name, out var readModel))
            {
                Error(DiagnosticCodes.InvalidSemanticBinding, $"Graph query '{query.Name}' returns read model '{query.ReturnType.Name}', which is unresolved.", query.ReturnType.Location);
                return null;
            }

            var before = _diagnostics.Count(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
            var arguments = (query.By is null ? [] : new[] { (Parameter: query.By, IsKey: true) })
                .Concat(query.Filters.Select(filter => (Parameter: filter, IsKey: false)))
                .Select(argument => new SemanticGraphQueryArgument(argument.Parameter.Name, BindTypeReference(argument.Parameter.Type), argument.IsKey)).ToImmutableArray();
            var returns = ImmutableArray.CreateBuilder<SemanticGraphReturn>();
            foreach (var @return in query.Returns)
            {
                if (readModel.Properties.TryGetValue(@return.Property, out var property)) returns.Add(new(property.Id, @return.Alias, @return.Source));
                else Error(DiagnosticCodes.InvalidSemanticBinding, $"Graph query '{query.Name}' returns '{@return.Property}', which read model '{query.ReturnType.Name}' does not declare.", @return.Location);
            }

            SemanticGraphPatternNode Node(GraphPatternNodeSyntax node) => new(node.Alias, node.Label);
            var matches = query.Matches.Select(match => new SemanticGraphMatch(
                match.IsOptional,
                Node(match.Start),
                [.. match.Hops.Select(hop => new SemanticGraphHop(hop.Alias, hop.Type, hop.Outgoing, Node(hop.Node)))])).ToImmutableArray();
            var conditions = query.Conditions.Select(condition => condition.Value switch
            {
                PathExpressionSyntax argument => new SemanticGraphCondition(condition.Alias, condition.Property) { Argument = argument.Path },
                LiteralExpressionSyntax literal => new SemanticGraphCondition(condition.Alias, condition.Property) { Literal = BindLiteral(literal) },
                _ => null
            }).OfType<SemanticGraphCondition>().ToImmutableArray();
            var module = scope.Segments[0];
            var featurePath = scope.Segments.Skip(1).Take(scope.Segments.Count - 2).ToImmutableArray();
            var authorization = EffectiveAuthorization(query.Authorize, arguments.Where(argument => argument.IsKey).Select(argument => new SemanticProperty(default, argument.Name, argument.Type, false)), module, featurePath, queryFilterNames: query.Filters.Select(filter => filter.Name));
            var cardinality = query.ReturnType.IsCollection ? SemanticQueryCardinality.Many : query.ReturnType.IsOptional ? SemanticQueryCardinality.ZeroOrOne : SemanticQueryCardinality.One;
            return _diagnostics.Count(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error) > before
                ? null
                : new(query.Name, query.Graph, readModel.Model.Id, cardinality, query.IsObservable ? SemanticQueryDelivery.Live : SemanticQueryDelivery.Snapshot, arguments, matches, conditions, returns.ToImmutable()) { Authorization = authorization };
        }

        SemanticGraph? BindGraph(GraphSyntax graph)
        {
            if (graph.Description is not null || graph.Documentation is not null)
            {
                Information(DiagnosticCodes.ReportOnlySemanticSyntax, $"Graph '{graph.Name}' description and documentation are authoring metadata.", graph.Location);
            }

            if (graph.File is not null)
            {
                Information(DiagnosticCodes.ReportOnlySemanticSyntax, $"Graph '{graph.Name}' file reference is realization provenance.", graph.File.Location);
            }

            var before = _diagnostics.Count(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
            var nodes = graph.Nodes.Select(node => new SemanticGraphNode(node.Label, BindGraphProperties(node.Properties))).ToImmutableArray();
            var edges = graph.Edges.Select(edge => new SemanticGraphEdge(edge.Type, edge.Source, edge.Target, BindGraphProperties(edge.Properties)) { IdentifiedBy = edge.IdentifiedBy }).ToImmutableArray();
            return _diagnostics.Count(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error) > before ? null : new(graph.Name, nodes, edges);
        }

        ImmutableArray<SemanticGraphProperty> BindGraphProperties(IEnumerable<PropertySyntax> properties)
        {
            var bound = ImmutableArray.CreateBuilder<SemanticGraphProperty>();
            foreach (var property in properties)
            {
                if (!IsPrimitiveOrConcept(property.Type.Name))
                {
                    Error(DiagnosticCodes.InvalidSemanticBinding, $"Graph property '{property.Name}' type '{property.Type.Name}' is not a primitive or a concept over one.", property.Type.Location);
                    continue;
                }

                bound.Add(new(property.Name, BindTypeReference(property.Type), property.IsKey));
            }

            return bound.ToImmutable();
        }

        bool IsPrimitiveOrConcept(string name) =>
            Array.Exists(["Uuid", "String", "Int", "Decimal", "Bool", "Date", "DateTime"], primitive => primitive == name) || _concepts.ContainsKey(name);

        SemanticGraphProjection? BindGraphProjection(GraphProjectionSyntax projection, Dictionary<string, SemanticGraph> graphs)
        {
            if (projection.File is not null)
            {
                Information(DiagnosticCodes.ReportOnlySemanticSyntax, $"Graph projection '{projection.Name}' file reference is realization provenance.", projection.File.Location);
            }

            if (!graphs.TryGetValue(projection.Graph, out var graph))
            {
                Error(DiagnosticCodes.InvalidSemanticBinding, $"Graph projection '{projection.Name}' graph '{projection.Graph}' is unresolved.", projection.Location);
                return null;
            }

            var before = _diagnostics.Count(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
            var nodes = ImmutableArray.CreateBuilder<SemanticGraphElementHandlers>();
            var edges = ImmutableArray.CreateBuilder<SemanticGraphElementHandlers>();
            foreach (var block in projection.Blocks)
            {
                switch (block)
                {
                    case GraphNodeBlockSyntax node when graph.Nodes.FirstOrDefault(candidate => candidate.Label == node.Label) is { } declared:
                        nodes.Add(new(node.Label, BindGraphHandlers(node.Blocks, declared.Properties)));
                        break;
                    case GraphEdgeBlockSyntax edge when graph.Edges.FirstOrDefault(candidate => candidate.Type == edge.Type) is { } declared:
                        edges.Add(new(edge.Type, BindGraphHandlers(edge.Blocks, declared.Properties)));
                        break;
                    default:
                        Error(DiagnosticCodes.InvalidSemanticBinding, $"Graph projection '{projection.Name}' builds an element graph '{graph.Name}' does not declare.", block.Location);
                        break;
                }
            }

            return _diagnostics.Count(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error) > before ? null : new(projection.Name, graph.Name, nodes.ToImmutable(), edges.ToImmutable());
        }

        ImmutableArray<SemanticGraphHandler> BindGraphHandlers(IEnumerable<ProjectionBlockSyntax> blocks, ImmutableArray<SemanticGraphProperty> properties)
        {
            var every = blocks.OfType<EverySyntax>().SelectMany(block => block.Mappings).ToArray();
            var handlers = ImmutableArray.CreateBuilder<SemanticGraphHandler>();
            foreach (var block in blocks)
            {
                switch (block)
                {
                    case GraphFromSyntax from when _events.TryGetValue(ShortName(from.Event), out var @event):
                        handlers.Add(new(@event.Contract.Id, BindGraphMappings([.. every, .. from.Mappings], properties, @event))
                        {
                            Key = BindGraphValue(from.Key, @event),
                            Source = BindGraphValue(from.Source, @event),
                            Target = BindGraphValue(from.Target, @event),
                            Identity = BindGraphValue(from.Identity, @event)
                        });
                        break;
                    case GraphRemoveSyntax remove when _events.TryGetValue(ShortName(remove.Event), out var @event):
                        handlers.Add(new(@event.Contract.Id, [])
                        {
                            Key = BindGraphValue(remove.Key, @event),
                            Source = BindGraphValue(remove.Source, @event),
                            Target = BindGraphValue(remove.Target, @event),
                            Identity = BindGraphValue(remove.Identity, @event),
                            Removes = true,
                            Detach = remove.Detach
                        });
                        break;
                    case GraphFromSyntax or GraphRemoveSyntax:
                        Error(DiagnosticCodes.InvalidSemanticBinding, "A graph handler event is unresolved.", block.Location);
                        break;
                }
            }

            return handlers.ToImmutable();
        }

        SemanticProjectionValue? BindGraphValue(ExpressionSyntax? expression, BoundEvent @event) =>
            expression is null ? null : BindKeyValue(expression, @event, "graph key");

        ImmutableArray<SemanticGraphMapping> BindGraphMappings(MappingSyntax[] mappings, ImmutableArray<SemanticGraphProperty> properties, BoundEvent @event)
        {
            var bound = ImmutableArray.CreateBuilder<SemanticGraphMapping>();
            var mapped = mappings.Select(mapping => mapping.Property).ToHashSet(StringComparer.Ordinal);
            foreach (var property in properties.Where(property => !mapped.Contains(property.Name) && @event.Properties.ContainsKey(property.Name)))
            {
                bound.Add(new(property.Name, SemanticProjectionOperation.Set, SemanticProjectionValue.EventProperty([@event.Properties[property.Name].Id])));
            }

            foreach (var mapping in mappings)
            {
                if (properties.All(property => property.Name != mapping.Property))
                {
                    Error(DiagnosticCodes.InvalidSemanticBinding, $"Graph property '{mapping.Property}' not found.", mapping.Location);
                    continue;
                }

                if (BindGraphMapping(mapping, @event) is { } value) bound.Add(value);
            }

            return bound.ToImmutable();
        }

        SemanticGraphMapping? BindGraphMapping(MappingSyntax mapping, BoundEvent @event)
        {
            SemanticGraphMapping? Source(SemanticProjectionOperation operation, ExpressionSyntax value, ExpressionSyntax? factor = null)
            {
                var source = BindProjectionValue(value, @event, "graph mapping");
                var multiplier = factor is null ? null : BindProjectionValue(factor, @event, "graph mapping");
                return source is null || (factor is not null && multiplier is null) ? null : new(mapping.Property, operation, source) { Factor = multiplier };
            }

            return mapping switch
            {
                SetMappingSyntax { Source: LiteralExpressionSyntax { Value: null } } => new(mapping.Property, SemanticProjectionOperation.Clear, null),
                SetMappingSyntax set => Source(SemanticProjectionOperation.Set, set.Source),
                ClearMappingSyntax => new(mapping.Property, SemanticProjectionOperation.Clear, null),
                IncrementMappingSyntax or CountMappingSyntax => new(mapping.Property, SemanticProjectionOperation.Increment, null),
                DecrementMappingSyntax => new(mapping.Property, SemanticProjectionOperation.Decrement, null),
                AddMappingSyntax add => Source(SemanticProjectionOperation.Add, add.Value, add.Factor),
                SubtractMappingSyntax subtract => Source(SemanticProjectionOperation.Subtract, subtract.Value, subtract.Factor),
                _ => null
            };
        }

        SemanticGraphSpecification? BindGraphSpecification(SliceSyntax slice, SpecificationSyntax specification, Dictionary<string, SemanticGraph> graphs, Dictionary<string, SemanticCommand> commands)
        {
            var before = _diagnostics.Count(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
            var text = SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text);
            var given = ImmutableArray.CreateBuilder<SemanticSpecificationEvent>();
            foreach (var step in specification.Given)
            {
                if (BindSpecificationEvent(step, commands, historicalFact: true, subjectKey: text) is { } bound) given.Add(bound);
            }

            SemanticSpecificationEvent? appended = null;
            if (specification.WhenAppended is not null) appended = BindSpecificationEvent(specification.WhenAppended, commands, subjectKey: text);
            if (specification.When is not null || specification.WhenQuery is not null || specification.WhenClock is not null || specification.WhenTrigger is not null || specification.WhenCapture is not null || specification.ThenEvents.Any() || (specification.ThenReadModels ?? []).Any() || specification.ThenErrors.Any())
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Graph specification '{specification.Name}' states a command, query or outcome other than 'when append' and 'then graph'.", specification.Location);
            }

            var thenQueries = ImmutableArray.CreateBuilder<SemanticGraphQueryExpectation>();
            foreach (var expectation in specification.ThenQueries)
            {
                if (BindGraphQueryExpectation(slice, expectation) is { } boundExpectation) thenQueries.Add(boundExpectation);
            }

            var givenGraphs = specification.GivenGraphs.Select(step => BindGraphStep(step, graphs)).OfType<SemanticGraphStep>().ToImmutableArray();
            var thenGraphs = specification.ThenGraphs.Select(step => BindGraphStep(step, graphs)).OfType<SemanticGraphStep>().ToImmutableArray();
            return _diagnostics.Count(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error) > before
                ? null
                : new(slice.Name, specification.Name, given.ToImmutable()) { GivenGraphs = givenGraphs, WhenAppended = appended, ThenGraphs = thenGraphs, ThenQueries = thenQueries.ToImmutable() };
        }

        SemanticGraphQueryExpectation? BindGraphQueryExpectation(SliceSyntax slice, SpecificationQuerySyntax expectation)
        {
            var query = ScreenplayValidator.ScopedSlices(syntax).SelectMany(entry => entry.Slice.GraphQueries).FirstOrDefault(candidate => candidate.Name == expectation.Query);
            if (query is null || !_readModels.TryGetValue(query.ReturnType.Name, out var readModel))
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Graph specification of slice '{slice.Name}' asserts query '{expectation.Query}', which is not a graph query with a known read model.", expectation.Location);
                return null;
            }

            var declared = (query.By is null ? [] : new[] { query.By }).Concat(query.Filters).ToDictionary(argument => argument.Name, StringComparer.Ordinal);
            var arguments = ImmutableArray.CreateBuilder<SemanticGraphPropertyValue>();
            foreach (var argument in expectation.Arguments)
            {
                if (!declared.TryGetValue(argument.Property, out var parameter))
                {
                    Error(DiagnosticCodes.InvalidSemanticBinding, $"Graph query '{query.Name}' has no argument '{argument.Property}'.", argument.Location);
                    continue;
                }

                if (BindConcreteValue(argument.Source, BindTypeReference(parameter.Type), "graph query argument", false) is { } value) arguments.Add(new(argument.Property, value));
            }

            var rows = ImmutableArray.CreateBuilder<SemanticGraphRow>();
            foreach (var result in expectation.Results)
            {
                var values = ImmutableArray.CreateBuilder<SemanticGraphPropertyValue>();
                foreach (var property in result.Properties)
                {
                    if (!readModel.Properties.TryGetValue(property.Property, out var target))
                    {
                        Error(DiagnosticCodes.InvalidSemanticBinding, $"Read model '{readModel.Syntax.Name}' has no property '{property.Property}'.", property.Location);
                        continue;
                    }

                    if (BindConcreteValue(property.Source, target.Type, "graph query row", false) is { } value) values.Add(new(property.Property, value));
                }

                rows.Add(new(values.ToImmutable()));
            }

            return new(query.Name, expectation.Exactly || expectation.Results.Any(result => result.Exactly), arguments.ToImmutable(), rows.ToImmutable());
        }

        SemanticGraphStep? BindGraphStep(SpecificationGraphSyntax step, Dictionary<string, SemanticGraph> graphs)
        {
            if (!graphs.TryGetValue(step.Graph, out var graph))
            {
                Error(DiagnosticCodes.InvalidSemanticBinding, $"Graph '{step.Graph}' is unresolved.", step.Location);
                return null;
            }

            var elements = ImmutableArray.CreateBuilder<SemanticGraphElementStep>();
            foreach (var element in step.Elements)
            {
                if (BindGraphElementStep(graph, element) is { } bound) elements.Add(bound);
            }

            return new(graph.Name, elements.ToImmutable());
        }

        SemanticGraphElementStep? BindGraphElementStep(SemanticGraph graph, SpecificationGraphElementSyntax element)
        {
            SemanticValue? Key(string label, ExpressionSyntax expression)
            {
                var key = graph.Nodes.FirstOrDefault(node => node.Label == label)?.Properties.FirstOrDefault(property => property.IsKey);
                if (key is null)
                {
                    Error(DiagnosticCodes.InvalidSemanticBinding, $"Graph node '{label}' is unresolved.", expression.Location);
                    return null;
                }

                return BindConcreteValue(expression, key.Type, "graph key", false);
            }

            ImmutableArray<SemanticGraphPropertyValue> Values(ImmutableArray<SemanticGraphProperty> properties)
            {
                var values = ImmutableArray.CreateBuilder<SemanticGraphPropertyValue>();
                foreach (var value in element.Properties)
                {
                    var property = properties.FirstOrDefault(candidate => candidate.Name == value.Property);
                    if (property is null)
                    {
                        Error(DiagnosticCodes.InvalidSemanticBinding, $"Graph property '{value.Property}' is unresolved.", value.Location);
                        continue;
                    }

                    if (BindConcreteValue(value.Source, property.Type, "graph property", true) is { } bound) values.Add(new(property.Name, bound));
                }

                return values.ToImmutable();
            }

            switch (element)
            {
                case SpecificationGraphNodeSyntax node when graph.Nodes.FirstOrDefault(candidate => candidate.Label == node.Label) is { } declared:
                    return Key(node.Label, node.Key) is { } key ? new(true, node.Absent, node.Label, key, Values(declared.Properties)) : null;
                case SpecificationGraphEdgeSyntax edge when graph.Edges.FirstOrDefault(candidate => candidate.Type == edge.Type) is { } declared:
                    var source = Key(declared.Source, edge.Source);
                    var target = Key(declared.Target, edge.Target);
                    SemanticValue? identity = null;
                    if (edge.Identity is not null)
                    {
                        var property = declared.Properties.FirstOrDefault(candidate => candidate.Name == declared.IdentifiedBy);
                        identity = property is null ? null : BindConcreteValue(edge.Identity, property.Type, "graph edge identity", false);
                    }

                    return source is null || target is null || (edge.Identity is not null && identity is null)
                        ? null
                        : new(false, edge.Absent, edge.Type, source, Values(declared.Properties)) { Target = target, Identity = identity };
                default:
                    Error(DiagnosticCodes.InvalidSemanticBinding, "A graph specification step names a node or edge the graph does not declare.", element.Location);
                    return null;
            }
        }
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Parsing;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Completeness;

static class EventConsumerCompleteness
{
    internal static IEnumerable<Diagnostic> Check(ApplicationSyntax application, ConsistencyDeclarations declarations)
    {
        var consumers = new Consumers(declarations);
        consumers.VisitApplication(application);

        // Only local declarations are candidates. An imported external contract has no local declaration.
        foreach (var @event in declarations.Slices.SelectMany(entry => EventDeclarations.In(entry.Slice)
            .GroupBy(@event => @event.Name, StringComparer.Ordinal).Select(group => group.MaxBy(@event => @event.Generation)!))
            .Where(@event => !consumers.Consumed.Contains(@event)))
        {
            yield return Diagnostic.Warning(
                DiagnosticCodes.UnconsumedEvent,
                $"Event '{@event.Name}' has no declared projection, reducer, reaction, constraint or interaction consumer; specifications do not count",
                @event.Location);
        }
    }

    sealed class Consumers(ConsistencyDeclarations declarations) : ScreenplaySyntaxWalker
    {
        DeclarationScope _scope = new([]);
        internal HashSet<EventSyntax> Consumed { get; } = new(ReferenceEqualityComparer.Instance);

        /// <inheritdoc/>
        public override void VisitModule(ModuleSyntax syntax)
        {
            var previous = _scope;
            _scope = new([.. previous.Segments, syntax.Name]);
            base.VisitModule(syntax);
            _scope = previous;
        }

        /// <inheritdoc/>
        public override void VisitFeature(FeatureSyntax syntax)
        {
            var previous = _scope;
            _scope = new([.. previous.Segments, syntax.Name]);
            base.VisitFeature(syntax);
            _scope = previous;
        }

        /// <inheritdoc/>
        public override void VisitSlice(SliceSyntax syntax)
        {
            var previous = _scope;
            _scope = new([.. previous.Segments, syntax.Name]);
            base.VisitSlice(syntax);
            _scope = previous;
        }

        /// <inheritdoc/>
        public override void VisitSpecification(SpecificationSyntax syntax)
        {
            // Example histories and assertions are evidence, not application consumers.
        }

        /// <inheritdoc/>
        public override void VisitNode(SyntaxNode node)
        {
            var name = node switch
            {
                EventSpecSyntax from => from.Event,
                JoinEventSyntax join => join.Event,
                ProjectionEntersOnSyntax enters => enters.Event,
                ClearWithSyntax clear => clear.Event,
                RemoveWithSyntax remove => remove.Event,
                RemoveViaJoinSyntax remove => remove.Event,
                ReducerRuleSyntax rule => rule.Event,
                NamedTriggerSourceSyntax trigger => trigger.Name,
                UniquePropertyConstraintSyntax unique => unique.Event,
                UniqueEventConstraintSyntax unique => unique.Event,
                EventInteractionTriggerSyntax trigger => trigger.EventName,
                _ => null
            };
            if (name is not null) Consume(name);
            if (node is ConstraintSyntax constraint)
            {
                foreach (var released in constraint.ReleasedBy) Consume(released);
            }
            if (node is AllSyntax)
            {
                // 'all' subscribes to every event type, not just those named elsewhere in this projection.
                Consumed.UnionWith(declarations.Slices.SelectMany(entry => EventDeclarations.In(entry.Slice)));
            }
        }

        void Consume(string name)
        {
            if (declarations.Event(name, _scope) is { } @event) Consumed.Add(@event);
        }
    }
}

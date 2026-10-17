// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Parsing;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.Semantics;

public sealed partial class SemanticModelBinder
{
    private sealed partial class BindingContext
    {
        readonly Dictionary<ProjectionSyntax, ImmutableArray<SemanticProjection>> _decisionProjections = [];
        readonly Dictionary<string, (SemanticDecisionRead Read, BoundReadModel View)> _commandReads = new(StringComparer.Ordinal);
        readonly HashSet<string> _ambiguousReadViews = new(StringComparer.Ordinal);
        Dictionary<SemanticId, SemanticConcept> _decisionReadConcepts = [];
        DiagnosticArgument? _decisionReadCommand;

        bool ProtectedCommandReads => syntax.CommandReadMode is not null;

        void RegisterDecisionReadProjections(ImmutableArray<SemanticConcept> concepts)
        {
            _decisionReadConcepts = concepts.ToDictionary(concept => concept.Id);
            foreach (var (module, featurePath, slice) in AllSlices())
            {
                var address = SemanticAddress.ForSlice(_applicationIdentity, module, featurePath, slice.Name);
                foreach (var projection in slice.Projections) _decisionProjections.TryAdd(projection, [.. BindProjections(address, projection)]);
            }
        }

        ImmutableArray<SemanticDecisionRead> BindDecisionReads(CommandSyntax command, Dictionary<string, SemanticProperty> properties, DeclarationScope scope)
        {
            _commandReads.Clear();
            _ambiguousReadViews.Clear();
            var commandArgument = DiagnosticArgument.Declaration("command", [.. scope.Segments, command.Name]);
            _decisionReadCommand = commandArgument;
            if (syntax.CommandReadMode is null) return [];
            if (command.Concurrency is not null && (command.Reads?.Any() ?? false))
                Error(DiagnosticCodes.DecisionReadsWithConcurrency, $"Command '{command.Name}' cannot declare both protected reads and concurrency.", command.Concurrency.Location, DiagnosticArguments.Empty.With("command", commandArgument));
            foreach (var group in (command.Reads ?? []).GroupBy(read => read.ReadModel, StringComparer.Ordinal).Where(group => group.Count() > 1))
            {
                _ambiguousReadViews.Add(group.Key);
                foreach (var read in group.Where(read => read.Alias is null))
                    Error(DiagnosticCodes.MissingReadsAlias, $"Repeated read '{read.ReadModel}' requires an alias on every instance.", read.Location);
            }
            var reads = ImmutableArray.CreateBuilder<SemanticDecisionRead>();
            foreach (var read in command.Reads ?? [])
            {
                if (!_readModels.TryGetValue(read.ReadModel, out var view))
                {
                    Error(DiagnosticCodes.InvalidSemanticBinding, $"Protected read model '{read.ReadModel}' is unresolved.", read.Location);
                    continue;
                }
                var keyName = read.By ?? command.Properties.SingleOrDefault(property => property.IsIdentifier)?.Name;
                if (keyName is null)
                {
                    Error(DiagnosticCodes.DecisionReadWithoutIdentifier, $"Read '{read.ReadModel}' omits 'by' and command '{command.Name}' has no declared identifier.", read.Location, DiagnosticArguments.Empty.With("command", commandArgument).With("read", DiagnosticArgument.Text(read.Alias ?? read.ReadModel)));
                    continue;
                }
                if (!properties.TryGetValue(keyName, out var key))
                {
                    Error(DiagnosticCodes.InvalidSemanticBinding, $"Decision read key '{keyName}' is not an input property of '{command.Name}'.", read.Location);
                    continue;
                }
                if (DecisionReadRefusal(command, read, view, key, scope) is { } refusal)
                {
                    Error(DiagnosticCodes.UnprotectableDecisionRead, $"Read '{read.ReadModel}' cannot be protected: {refusal}.", read.Location, DiagnosticArguments.Empty
                        .With("command", commandArgument)
                        .With("read", DiagnosticArgument.Text(read.Alias ?? read.ReadModel))
                        .With("readModel", DiagnosticArgument.Declaration("readmodel", _readModelPaths[read.ReadModel]))
                        .With("reason", DiagnosticArgument.Keyword(refusal.Keyword())));
                    continue;
                }
                var name = read.Alias ?? read.ReadModel;
                if (properties.ContainsKey(name))
                {
                    Error(DiagnosticCodes.ReadsAliasConflictsWithProperty, $"Read alias '{name}' collides with a command property.", read.Location);
                    continue;
                }
                var bound = new SemanticDecisionRead(name, view.Model.Id, key.Id, SemanticDecisionReadPresence.Required);
                if (!_commandReads.TryAdd(name, (bound, view)))
                    Error(DiagnosticCodes.DuplicateReadsAlias, $"Read alias '{name}' is duplicated.", read.Location);
                else reads.Add(bound);
            }
            return reads.ToImmutable();
        }

        SemanticDecisionReadRefusal? DecisionReadRefusal(CommandSyntax command, ReadsSyntax read, BoundReadModel view, SemanticProperty key, DeclarationScope scope)
        {
            if (read.ByParts.Any()) return SemanticDecisionReadRefusal.ByParts;
            var declarations = new ConsistencyDeclarations(syntax, [.. ScreenplayValidator.ScopedSlices(syntax)]);
            var viewKey = declarations.ViewKey(read.ReadModel, scope);
            var reducer = declarations.Slices.Any(entry => (entry.Slice.Reducers ?? []).Any(value => value.ReadModel == read.ReadModel));
            if (reducer) return SemanticDecisionReadRefusal.Reducer;
            if (viewKey.Length > 1) return SemanticDecisionReadRefusal.ByParts;
            if (viewKey.Length == 0) return SemanticDecisionReadRefusal.Singleton;
            if (viewKey[0].Type.IsOptional || viewKey[0].Type.IsCollection) return SemanticDecisionReadRefusal.KeyType;
            if (SemanticDecisionReadAdmission.Key(key, _decisionReadConcepts) is { } keyRefusal) return keyRefusal;
            var input = command.Properties.Single(property => property.Name == key.Name);
            if (declarations.Compatible(input.Type, viewKey[0].Type) != true) return SemanticDecisionReadRefusal.KeyType;
            var projections = _decisionProjections.Where(entry => entry.Value.Any(value => value.ReadModel == view.Model.Id)).ToArray();
            var routed = SemanticDecisionReadAdmission.Routed(command.Stream is not null, projections.Any(entry => entry.Key.Sequence is not null), projections.SelectMany(entry => entry.Value), DecisionReadProducers());
            return SemanticDecisionReadAdmission.View(projections.SelectMany(entry => entry.Value), false, routed);
        }

        (SemanticProperty Property, string Read)? BindReadPath(string path, SourceLocation location, string position)
        {
            var parts = path.Split('.');
            if (parts.Length != 2)
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Read path '{path}' in {position} must have exactly one view property.", location);
                return null;
            }
            if (_ambiguousReadViews.Contains(parts[0]))
            {
                Error(DiagnosticCodes.AmbiguousReadPath, $"Read path '{path}' must use an alias because '{parts[0]}' is read more than once.", location, ReadPathArguments(path).With("read", DiagnosticArgument.Text(parts[0])));
                return null;
            }
            if (!_commandReads.TryGetValue(parts[0], out var read) || !read.View.Properties.TryGetValue(parts[1], out var property))
            {
                Error(DiagnosticCodes.UnknownReadPath, $"Read path '{path}' does not resolve to a declared read and view property.", location, ReadPathArguments(path).With("read", DiagnosticArgument.Text(parts[0])));
                return null;
            }
            return (property, read.Read.Name);
        }

        DiagnosticArguments ReadPathArguments(string path)
        {
            var arguments = DiagnosticArguments.Empty.With("path", DiagnosticArgument.Text(path));
            return arguments.With("command", _decisionReadCommand ?? throw new InvalidSemanticContract("A read path was bound outside a command."));
        }

        void ReportLegacyDecisionReads()
        {
            if (syntax.CommandReadMode is not null) return;
            var first = AllSlices().SelectMany(entry => entry.Slice.Commands).SelectMany(command => command.Reads ?? []).FirstOrDefault();
            if (first is null) return;
            var probe = new BindingContext(applicationName, syntax with { CommandReadMode = CommandReadMode.Protected }, documents, steps, expansion, utcAssessmentYear);
            try
            {
                _ = probe.BindApplication();
                var locations = DecisionReadLocations();
                if (!probe.Diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error && (diagnostic.Code == DiagnosticCodes.InvalidSemanticBinding || locations.Contains(diagnostic.Location))))
                    Information(DiagnosticCodes.LegacyCommandReadsRepairAvailable, "All command reads are admissible under an explicit 'command reads protected' preamble; use a reviewed workspace proposal to opt in.", first.Location);
            }
            catch (InvalidSemanticContract)
            {
                // An invalid candidate cannot establish repair eligibility; retain the original legacy refusals.
            }
        }
    }
}

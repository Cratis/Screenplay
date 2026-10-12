// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Parsing;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Captures;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.Workspaces;

sealed class WorkspacePropertyAddPlan(WorkspaceSyntaxIndex index, WorkspacePropertyPlan properties, WorkspacePropertyRequest request, WorkspacePropertyReport report)
{
    readonly WorkspacePropertyAddChain _chain = new(index, properties);
    readonly List<WorkspacePropertyAddition> _additions = [];
    readonly List<(WorkspaceSyntaxEntry Block, string Value)> _mappings = [];
    HashSet<WorkspaceNodeHandle> _connected = [];

    internal IReadOnlyList<WorkspacePropertyAddition> Additions => _additions;
    internal IEnumerable<(WorkspaceSyntaxEntry Block, string Value, WorkspacePropertyAddition Target)> Mappings => _mappings.Select(mapping => (mapping.Block, mapping.Value, MappingTarget(mapping.Block)!));
    internal WorkspacePropertyReport Report { get; private set; } = report;

    internal static SemanticAddress Address(WorkspacePropertyAddition addition) => addition.Owner.Node is EventSyntax @event
        ? SemanticAddress.ForEventProperty(addition.Owner.Address!, new(@event.Generation), addition.Name)
        : SemanticAddress.ForProperty(addition.Owner.Address!, addition.Name);

    internal static WorkspacePropertyCandidate Candidate(WorkspacePropertyAddition addition) => new(Address(addition), addition.Kind, "declaration", addition.Owner.Location);

    internal WorkspacePropertyRefusal? Resolve()
    {
        var owner = index.Find(request.Owner!);
        if (owner is null || WorkspaceReferenceBindings.Name(owner.Node) != request.ExpectedOwnerName) return Refuse(WorkspaceConflictKind.UnsupportedPropertySpan);
        if (Add(owner, request.Name!) is { } refusal) return refusal;
        _connected = _chain.Connected(owner);
        Report = Report with { Target = new(Address(_additions[0]), _additions[0].Kind, (owner.Node as EventSyntax)?.Generation) };
        foreach (var hop in request.Hops)
        {
            if (ResolveHop(hop) is { } hopRefusal) return hopRefusal;
        }
        var offChain = _additions.Where(addition => !_connected.Contains(addition.Owner.Handle)).ToArray();
        if (offChain.Length > 0) return new(WorkspaceConflictKind.HopNotOnChain, [.. offChain.Select(Candidate)]);
        ReportChain();
        return null;
    }

    internal WorkspacePropertyRefusal? InspectSources()
    {
        var missingTargets = _mappings.Where(mapping => MappingTarget(mapping.Block) is null)
            .Select(mapping => Candidate(_additions[0]) with { Role = "mapping-target", Location = mapping.Block.Location });
        var candidates = missingTargets.Concat(MissingProducers()).Concat(MissingConsumers()).ToArray();
        return candidates.Length == 0 ? null : new(WorkspaceConflictKind.SourcesRequired, [.. candidates.Distinct()]);
    }

    static WorkspacePropertyRefusal Refuse(WorkspaceConflictKind kind) => new(kind, []);

    static bool ValidName(string name) => name.Length > 0 && (char.IsLetter(name[0]) || name[0] == '_') && name.All(character => char.IsLetterOrDigit(character) || character == '_');

    WorkspacePropertyRefusal? Add(WorkspaceSyntaxEntry owner, string name)
    {
        if (owner.Member == "inlineEvent") return Refuse(WorkspaceConflictKind.InlineEventRequiresExtraction);
        if (owner.Node is not (CommandSyntax or EventSyntax or ReadModelSyntax) || owner.Address is null) return Refuse(WorkspaceConflictKind.UnsupportedPropertySpan);
        var type = PropertyLineParser.ParseTypeRef(request.Type!, owner.Location);
        if (type.IsOptional || !type.Name.Split('.').All(ValidName)) return Refuse(WorkspaceConflictKind.InvalidOperation);
        var authoredOwner = properties.Links.AdditionOwner(owner);
        if (authoredOwner is null) return Refuse(WorkspaceConflictKind.AmbiguousPropertyReference);
        var addition = new WorkspacePropertyAddition(owner, name, new(name, type with { IsOptional = request.Optional }, owner.Location), authoredOwner.Address);
        if (properties.Historical(owner)) return new(WorkspaceConflictKind.HistoricalGenerationImmutable, [Candidate(addition)]);
        if (_additions.Exists(existing => existing.Owner.Handle == owner.Handle) || WorkspacePropertyLinks.Properties(owner.Node).Any(property => string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))) return new(WorkspaceConflictKind.InvalidOperation, [Candidate(addition)]);
        _additions.Add(addition);
        return null;
    }

    WorkspacePropertyRefusal? ResolveHop(WorkspacePropertyHop hop)
    {
        var entry = index.Find(hop.At);
        if (entry is null || (hop.Name is null) == (hop.Value is null)) return Refuse(WorkspaceConflictKind.InvalidOperation);
        if (hop.Name is { } name)
        {
            if (!ValidName(name)) return Refuse(WorkspaceConflictKind.InvalidOperation);
            return Add(entry, name);
        }
        if (entry.Node is not (ProducesSyntax or FromSyntax or CaptureAppendSyntax)) return Refuse(WorkspaceConflictKind.UnsupportedPropertySpan);
        if (!_chain.BlockOnChain(entry, _connected)) return new(WorkspaceConflictKind.HopNotOnChain, [new(_additions[0].Owner.Address!, _additions[0].Kind, "mapping-target", entry.Location)]);
        if (_mappings.Exists(mapping => mapping.Block.Handle == entry.Handle)) return Refuse(WorkspaceConflictKind.InvalidOperation);
        _mappings.Add((entry, hop.Value!));
        return null;
    }

    WorkspacePropertyAddition? MappingTarget(WorkspaceSyntaxEntry block)
    {
        var owners = block.Node is FromSyntax ? _chain.Feeds.Where(feed => _chain.FeedBlock(feed).Handle == block.Handle).SelectMany(feed => feed.Models).Select(_chain.Entry).OfType<WorkspaceSyntaxEntry>() : _chain.Targets(block);
        var targetOwners = owners.DistinctBy(owner => owner.Handle).ToArray();
        if (targetOwners.Length != 1) return null;
        var targets = _additions.Where(addition => targetOwners.Any(owner => owner.Handle == addition.Owner.Handle)).ToArray();
        return targets is [var target] ? target : null;
    }

    IEnumerable<WorkspacePropertyCandidate> MissingProducers()
    {
        foreach (var addition in _additions.Where(addition => addition.Owner.Node is EventSyntax))
        {
            foreach (var producer in _chain.Producers.Where(producer => _chain.Targets(producer).Any(target => target.Handle == addition.Owner.Handle) && !_mappings.Exists(mapping => mapping.Block.Handle == producer.Handle)))
            {
                yield return Candidate(addition) with { Role = "mapping-target", Location = producer.Location };
            }
        }
    }

    IEnumerable<WorkspacePropertyCandidate> MissingConsumers()
    {
        foreach (var addition in _additions.Where(addition => addition.Owner.Node is ReadModelSyntax))
        {
            foreach (var feed in _chain.Feeds.Where(feed => feed.Models.Any(model => _chain.Entry(model)?.Handle == addition.Owner.Handle)))
            {
                var block = _chain.FeedBlock(feed);
                var explicitMapping = _mappings.Exists(mapping => mapping.Block.Handle == block.Handle);
                var autoMap = feed.Nested is null && feed.AutoMap && feed.Events.Length == 1 && _additions.Exists(source => source.Owner.Handle == _chain.Entry(feed.Events[0])?.Handle && source.Name == addition.Name);
                if (!explicitMapping && !autoMap) yield return Candidate(addition) with { Role = "automap", Location = block.Location };
            }
        }
    }

    void ReportChain() => Report = Report with
    {
        NotChanged = [.. properties.Members.Where(member => !_connected.Contains(member.Owner.Handle) && member.Declaration.Name == request.Name).Select(member => new WorkspacePropertyUnchanged(member.Entry.Address!, member.Kind, "offChain"))],
        ChainCandidates = [.. _chain.Owners.Where(owner => _connected.Contains(owner.Handle) && !_additions.Exists(addition => addition.Owner.Handle == owner.Handle)).OrderBy(owner => owner.Address!, WorkspaceSemanticAddressComparer.Instance).Select(owner => new WorkspacePropertyUnchanged(owner.Address!, owner.Node switch { CommandSyntax => "Command", EventSyntax => "Event", _ => "ReadModel" }, "notSelected", "declaration"))]
    };
}

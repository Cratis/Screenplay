// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Indexing;
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

    static WorkspacePropertyRefusal? InspectAdditionOwner(WorkspaceSyntaxEntry owner)
    {
        if (owner.Member == "inlineEvent") return Refuse(WorkspaceConflictKind.InlineEventRequiresExtraction);
        return SupportsAddition(owner) ? null : Refuse(WorkspaceConflictKind.UnsupportedPropertySpan);
    }

    static bool SupportsAddition(WorkspaceSyntaxEntry owner) => owner.Address is not null && owner.Node switch
    {
        CommandSyntax => true,
        EventSyntax => true,
        ReadModelSyntax => true,
        _ => false
    };

    static bool ValidAdditionType(TypeRefSyntax type) => !type.IsOptional && type.Name.Split('.').All(ValidName);

    WorkspacePropertyRefusal? Add(WorkspaceSyntaxEntry owner, string name)
    {
        if (InspectAdditionOwner(owner) is { } refusal) return refusal;
        var type = PropertyLineParser.ParseTypeRef(request.Type!, owner.Location);
        if (!ValidAdditionType(type)) return Refuse(WorkspaceConflictKind.InvalidOperation);
        var authoredOwner = properties.Links.AdditionOwner(owner);
        if (authoredOwner is null) return Refuse(WorkspaceConflictKind.AmbiguousPropertyReference);
        var addition = new WorkspacePropertyAddition(owner, name, new(name, type with { IsOptional = request.Optional }, owner.Location), authoredOwner.Address);
        return RetainAddition(addition);
    }

    WorkspacePropertyRefusal? RetainAddition(WorkspacePropertyAddition addition)
    {
        if (properties.Historical(addition.Owner)) return new(WorkspaceConflictKind.HistoricalGenerationImmutable, [Candidate(addition)]);
        if (AdditionCollides(addition)) return new(WorkspaceConflictKind.InvalidOperation, [Candidate(addition)]);
        _additions.Add(addition);
        return null;
    }

    bool AdditionCollides(WorkspacePropertyAddition addition) => _additions.Exists(existing => existing.Owner.Handle == addition.Owner.Handle) ||
        WorkspacePropertyLinks.Properties(addition.Owner.Node).Any(property => string.Equals(property.Name, addition.Name, StringComparison.OrdinalIgnoreCase));

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

    IEnumerable<WorkspacePropertyCandidate> MissingConsumers() => _additions.Where(addition => addition.Owner.Node is ReadModelSyntax).SelectMany(MissingConsumerFeeds);

    IEnumerable<WorkspacePropertyCandidate> MissingConsumerFeeds(WorkspacePropertyAddition addition) => _chain.Feeds
        .Where(feed => ConsumesAddition(feed, addition) && !ConsumerHasSource(feed, addition))
        .Select(feed => Candidate(addition) with { Role = "automap", Location = _chain.FeedBlock(feed).Location });

    bool ConsumesAddition(WorkspacePropertyFeed feed, WorkspacePropertyAddition addition) => feed.Models.Any(model => _chain.Entry(model)?.Handle == addition.Owner.Handle);

    bool ConsumerHasSource(WorkspacePropertyFeed feed, WorkspacePropertyAddition addition) => ExplicitConsumerSource(_chain.FeedBlock(feed)) || AutoMapConsumerSource(feed, addition);

    bool ExplicitConsumerSource(WorkspaceSyntaxEntry block) => _mappings.Exists(mapping => mapping.Block.Handle == block.Handle);

    bool AutoMapConsumerSource(WorkspacePropertyFeed feed, WorkspacePropertyAddition addition)
    {
        if (feed.Nested is not null || !feed.AutoMap) return false;
        return feed.Events is [var @event] && MatchingAutoMapSource(@event, addition);
    }

    bool MatchingAutoMapSource(AuthoredDeclaration @event, WorkspacePropertyAddition addition) => _additions.Exists(source => source.Owner.Handle == _chain.Entry(@event)?.Handle && source.Name == addition.Name);

    void ReportChain() => Report = Report with
    {
        NotChanged = [.. properties.Members.Where(member => !_connected.Contains(member.Owner.Handle) && member.Declaration.Name == request.Name).Select(member => new WorkspacePropertyUnchanged(member.Entry.Address!, member.Kind, "offChain"))],
        ChainCandidates = [.. _chain.Owners.Where(owner => _connected.Contains(owner.Handle) && !_additions.Exists(addition => addition.Owner.Handle == owner.Handle)).OrderBy(owner => owner.Address!, WorkspaceSemanticAddressComparer.Instance).Select(owner => new WorkspacePropertyUnchanged(owner.Address!, owner.Node switch { CommandSyntax => "Command", EventSyntax => "Event", _ => "ReadModel" }, "notSelected", "declaration"))]
    };
}

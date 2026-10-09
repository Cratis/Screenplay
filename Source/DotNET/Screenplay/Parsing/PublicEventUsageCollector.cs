// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Captures;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Collects operational event edges, not specification fixtures or declarations.
/// </summary>
internal sealed class PublicEventUsageCollector : ScreenplaySyntaxWalker
{
    bool _command;
    internal List<(string Name, SyntaxNode Node, bool Output, bool Command)> Uses { get; } = [];
    internal List<AllSyntax> AllEvents { get; } = [];
    internal List<(string Target, SyntaxNode Node)> ProjectionTargets { get; } = [];
    internal List<CaptureSourceSyntax> EventSources { get; } = [];

    public override void VisitProjection(ProjectionSyntax syntax)
    {
        // A projection's '=> Name' target may be a read model or, in an outbound translation, an event; resolution decides.
        if (syntax.ReadModel is { } target) ProjectionTargets.Add((target, syntax));
        base.VisitProjection(syntax);
    }

    public override void VisitReducer(ReducerSyntax syntax)
    {
        ProjectionTargets.Add((syntax.ReadModel, syntax));
        base.VisitReducer(syntax);
    }

    public override void VisitCaptureSource(CaptureSourceSyntax syntax)
    {
        if (CaptureEventsSource.IsEvents(syntax))
        {
            EventSources.Add(syntax);
            foreach (var @event in CaptureEventsSource.Events(syntax)) Uses.Add((@event.Value, @event, false, false));
        }

        base.VisitCaptureSource(syntax);
    }

    public override void VisitCommand(CommandSyntax syntax)
    {
        _command = true;
        base.VisitCommand(syntax);
        _command = false;
    }

    public override void VisitProduces(ProducesSyntax syntax)
    {
        if (syntax.InlineOperation is null) Uses.Add((syntax.Event, syntax, true, _command));
    }

    public override void VisitCaptureAppend(CaptureAppendSyntax syntax) => Uses.Add((syntax.Event, syntax, true, false));
    public override void VisitEventSpec(EventSpecSyntax syntax) => Uses.Add((syntax.Event, syntax, false, false));
    public override void VisitJoinEvent(JoinEventSyntax syntax) => Uses.Add((syntax.Event, syntax, false, false));
    public override void VisitClearWith(ClearWithSyntax syntax) => Uses.Add((syntax.Event, syntax, false, false));
    public override void VisitRemoveWith(RemoveWithSyntax syntax) => Uses.Add((syntax.Event, syntax, false, false));
    public override void VisitRemoveViaJoin(RemoveViaJoinSyntax syntax) => Uses.Add((syntax.Event, syntax, false, false));
    public override void VisitReducerRule(ReducerRuleSyntax syntax) => Uses.Add((syntax.Event, syntax, false, false));
    public override void VisitConstraint(ConstraintSyntax syntax)
    {
        foreach (var releasedBy in syntax.ReleasedBy) Uses.Add((releasedBy, syntax, false, false));
        base.VisitConstraint(syntax);
    }

    public override void VisitUniquePropertyConstraint(UniquePropertyConstraintSyntax syntax) => Uses.Add((syntax.Event, syntax, false, false));
    public override void VisitUniqueEventConstraint(UniqueEventConstraintSyntax syntax) => Uses.Add((syntax.Event, syntax, false, false));
    public override void VisitAll(AllSyntax syntax) => AllEvents.Add(syntax);
    public override void VisitSpecification(SpecificationSyntax syntax)
    {
        // Fixtures and assertions describe scenarios, not operational subscriptions or appends.
    }

    public override void VisitReactionTrigger(ReactionTriggerSyntax syntax)
    {
        if (syntax.Source is NamedTriggerSourceSyntax named) Uses.Add((named.Name, named, false, false));
        base.VisitReactionTrigger(syntax);
    }
}

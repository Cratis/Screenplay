// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax;

public abstract partial class ScreenplaySyntaxWalker
{
    /// <summary>Visits one physical event source and its owned streams.</summary>
    /// <param name="syntax">The source declaration.</param>
    public virtual void VisitEventSource(EventSourceSyntax syntax)
    {
        VisitNode(syntax);
        if (syntax.Identifier is not null) VisitTypeRef(syntax.Identifier);
        foreach (var stream in syntax.Streams) VisitEventStream(stream);
    }

    /// <summary>Visits a source-owned stream and its declared id type.</summary>
    /// <param name="syntax">The stream declaration.</param>
    public virtual void VisitEventStream(EventStreamSyntax syntax)
    {
        VisitNode(syntax);
        if (syntax.StreamId is not null) VisitTypeRef(syntax.StreamId);
        foreach (var part in syntax.StreamIdParts) VisitEventStreamIdPart(part);
    }

    /// <summary>Visits a composite stream id part and its type.</summary>
    /// <param name="syntax">The part declaration.</param>
    public virtual void VisitEventStreamIdPart(EventStreamIdPartSyntax syntax)
    {
        VisitNode(syntax);
        VisitTypeRef(syntax.Type);
    }

    /// <summary>Visits an authored command route and its mapping or preserved property candidate.</summary>
    /// <param name="syntax">The command route.</param>
    public virtual void VisitCommandStream(CommandStreamSyntax syntax)
    {
        VisitNode(syntax);
        if (syntax.StreamId is not null) VisitPropertyMapping(syntax.StreamId);
        foreach (var part in syntax.StreamIdParts) VisitPropertyMapping(part);
        if (syntax.PropertyCandidate is not null) VisitProperty(syntax.PropertyCandidate);
    }
}

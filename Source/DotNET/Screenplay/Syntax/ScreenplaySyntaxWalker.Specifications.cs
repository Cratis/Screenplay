// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Traversal of specifications - the Given/When/Then scenarios a slice states about itself.
/// </summary>
public abstract partial class ScreenplaySyntaxWalker
{
    /// <summary>
    /// Visits a <see cref="SpecificationSyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="SpecificationSyntax"/> to visit.</param>
    /// <remarks>
    /// This is one of the four roots - a specification compiled on its own with
    /// <see cref="IScreenplayCompiler.CompileSpecification(string)"/> starts here.
    /// </remarks>
    public virtual void VisitSpecification(SpecificationSyntax syntax)
    {
        VisitNode(syntax);
        foreach (var example in syntax.Examples) VisitSpecificationExample(example);
        foreach (var parameter in syntax.Parameters) VisitSpecificationParameter(parameter);
        foreach (var row in syntax.Cases) VisitSpecificationCase(row);

        if (syntax.File is not null)
        {
            VisitFileReference(syntax.File);
        }

        if (syntax.GivenCallerPersona is not null) VisitSpecificationCallerPersona(syntax.GivenCallerPersona);
        if (syntax.GivenCaller is not null) VisitSpecificationCaller(syntax.GivenCaller);
        if (syntax.GivenClock is not null) VisitSpecificationClock(syntax.GivenClock);

        foreach (var failure in syntax.GivenOperationFailures) VisitSpecificationOperationFailure(failure);
        foreach (var operation in syntax.ThenOperations) VisitSpecificationOperation(operation);
        foreach (var compensation in syntax.ThenCompensated) VisitSpecificationCompensated(compensation);

        foreach (var @event in syntax.Given)
        {
            VisitSpecificationEvent(@event);
        }

        foreach (var readModel in syntax.GivenReadModels ?? [])
        {
            VisitSpecificationReadModel(readModel);
        }

        if (syntax.When is not null)
        {
            VisitSpecificationCommand(syntax.When);
        }

        foreach (var capture in syntax.GivenCaptures)
        {
            VisitSpecificationCapture(capture);
        }

        if (syntax.WhenAppended is not null)
        {
            VisitSpecificationEvent(syntax.WhenAppended);
        }

        if (syntax.WhenRedelivered is not null) VisitSpecificationRedelivery(syntax.WhenRedelivered);
        if (syntax.WhenClock is not null) VisitSpecificationClock(syntax.WhenClock);
        if (syntax.WhenTrigger is not null) VisitSpecificationTrigger(syntax.WhenTrigger);
        if (syntax.WhenCapture is not null) VisitSpecificationCapture(syntax.WhenCapture);
        if (syntax.WhenQuery is not null) VisitSpecificationWhenQuery(syntax.WhenQuery);

        foreach (var @event in syntax.ThenEvents)
        {
            VisitSpecificationEvent(@event);
        }

        foreach (var readModel in syntax.ThenReadModels ?? [])
        {
            VisitSpecificationReadModel(readModel);
        }

        foreach (var absent in syntax.ThenAbsentReadModels)
        {
            VisitSpecificationAbsentReadModel(absent);
        }

        foreach (var query in syntax.ThenQueries)
        {
            VisitSpecificationQuery(query);
        }

        foreach (var result in syntax.ThenResults)
        {
            VisitSpecificationQueryResult(result);
        }

        if (syntax.ThenNoResult is not null) VisitSpecificationNoResult(syntax.ThenNoResult);
        if (syntax.ThenDenied is not null) VisitSpecificationDenied(syntax.ThenDenied);
        if (syntax.ThenReturns is not null) VisitSpecificationReturn(syntax.ThenReturns);

        foreach (var error in syntax.ThenErrors)
        {
            VisitSpecificationError(error);
        }
    }

    /// <summary>
    /// Visits a typed example and its authored values.
    /// </summary>
    /// <param name="syntax">The example to visit.</param>
    public virtual void VisitSpecificationExample(SpecificationExampleSyntax syntax)
    {
        VisitNode(syntax);
        if (syntax.For is not null) VisitExpression(syntax.For);
        if (syntax.Stream is { } stream) VisitSpecificationStream(stream);
        if (syntax.NoStream is { } noStream) VisitSpecificationNoStream(noStream);
        foreach (var value in syntax.Values) VisitPropertyMapping(value);
        foreach (var value in syntax.GeneratedValues) VisitPropertyMapping(value);
    }

    /// <summary>
    /// Visits a redelivery action and its given-occurrence locator.
    /// </summary>
    /// <param name="syntax">The redelivery action.</param>
    public virtual void VisitSpecificationRedelivery(SpecificationRedeliverySyntax syntax)
    {
        VisitNode(syntax);
        if (syntax.For is not null) VisitExpression(syntax.For);
        if (syntax.Stream is { } stream) VisitSpecificationStream(stream);
        if (syntax.NoStream is { } noStream) VisitSpecificationNoStream(noStream);
        foreach (var value in syntax.Values) VisitPropertyMapping(value);
    }

    /// <summary>
    /// Visits a persona caller reference.
    /// </summary>
    /// <param name="syntax">The persona reference.</param>
    public virtual void VisitSpecificationCallerPersona(SpecificationCallerPersonaSyntax syntax) => VisitNode(syntax);

    /// <summary>Visits a caller fixture and its claims.</summary>
    /// <param name="syntax">The caller fixture.</param>
    public virtual void VisitSpecificationCaller(SpecificationCallerSyntax syntax)
    {
        VisitNode(syntax);
        foreach (var claim in syntax.Claims) VisitSpecificationCallerClaim(claim);
    }

    /// <summary>Visits a caller claim.</summary>
    /// <param name="syntax">The claim.</param>
    public virtual void VisitSpecificationCallerClaim(SpecificationCallerClaimSyntax syntax) => VisitNode(syntax);

    /// <summary>Visits a denial assertion.</summary>
    /// <param name="syntax">The assertion.</param>
    public virtual void VisitSpecificationDenied(SpecificationDeniedSyntax syntax) => VisitNode(syntax);

    /// <summary>
    /// Visits a <see cref="SpecificationEventSyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="SpecificationEventSyntax"/> to visit.</param>
    public virtual void VisitSpecificationEvent(SpecificationEventSyntax syntax)
    {
        VisitNode(syntax);
        if (syntax.Stream is { } stream) VisitSpecificationStream(stream);
        if (syntax.NoStream is { } noStream) VisitSpecificationNoStream(noStream);

        if (syntax.For is not null)
        {
            VisitExpression(syntax.For);
        }

        foreach (var value in syntax.Values)
        {
            VisitPropertyMapping(value);
        }
    }

    /// <summary>
    /// Visits a specification route and its literal stream id.
    /// </summary>
    /// <param name="syntax">The route.</param>
    public virtual void VisitSpecificationStream(SpecificationStreamSyntax syntax)
    {
        VisitNode(syntax);
        if (syntax.StreamId is { } streamId) VisitPropertyMapping(streamId);
        foreach (var part in syntax.StreamIdParts) VisitPropertyMapping(part);
    }

    /// <summary>
    /// Visits an unrouted expectation.
    /// </summary>
    /// <param name="syntax">The expectation.</param>
    public virtual void VisitSpecificationNoStream(SpecificationNoStreamSyntax syntax) => VisitNode(syntax);

    /// <summary>
    /// Visits a <see cref="SpecificationCommandSyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="SpecificationCommandSyntax"/> to visit.</param>
    public virtual void VisitSpecificationCommand(SpecificationCommandSyntax syntax)
    {
        VisitNode(syntax);

        foreach (var fixture in syntax.GeneratedValues)
        {
            VisitPropertyMapping(fixture);
        }

        if (syntax.For is not null)
        {
            VisitExpression(syntax.For);
        }

        foreach (var value in syntax.Values)
        {
            VisitPropertyMapping(value);
        }
    }

    /// <summary>
    /// Visits a <see cref="SpecificationReadModelSyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="SpecificationReadModelSyntax"/> to visit.</param>
    public virtual void VisitSpecificationReadModel(SpecificationReadModelSyntax syntax)
    {
        VisitNode(syntax);

        foreach (var property in syntax.Properties)
        {
            VisitPropertyMapping(property);
        }
    }

    /// <summary>Visits a keyed read-model absence assertion and its key.</summary>
    /// <param name="syntax">The absence assertion.</param>
    public virtual void VisitSpecificationAbsentReadModel(SpecificationAbsentReadModelSyntax syntax)
    {
        VisitNode(syntax);
        VisitExpression(syntax.Key);
    }

    /// <summary>
    /// Visits a <see cref="SpecificationQuerySyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="SpecificationQuerySyntax"/> to visit.</param>
    public virtual void VisitSpecificationQuery(SpecificationQuerySyntax syntax)
    {
        VisitNode(syntax);

        foreach (var argument in syntax.Arguments)
        {
            VisitPropertyMapping(argument);
        }

        foreach (var result in syntax.Results)
        {
            VisitSpecificationQueryResult(result);
        }
    }

    /// <summary>
    /// Visits a <see cref="SpecificationQueryResultSyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="SpecificationQueryResultSyntax"/> to visit.</param>
    public virtual void VisitSpecificationQueryResult(SpecificationQueryResultSyntax syntax)
    {
        VisitNode(syntax);

        foreach (var property in syntax.Properties)
        {
            VisitPropertyMapping(property);
        }
    }

    /// <summary>
    /// Visits a <see cref="SpecificationErrorSyntax"/> node.
    /// </summary>
    /// <param name="syntax">The <see cref="SpecificationErrorSyntax"/> to visit.</param>
    public virtual void VisitSpecificationError(SpecificationErrorSyntax syntax)
    {
        VisitNode(syntax);
        if (syntax.CaseValue is { } value) VisitCaseValueExpression(value);
    }

    /// <summary>
    /// Visits a table parameter and its type.
    /// </summary>
    /// <param name="syntax">The parameter.</param>
    public virtual void VisitSpecificationParameter(SpecificationParameterSyntax syntax)
    {
        VisitNode(syntax);
        VisitTypeRef(syntax.Type);
    }

    /// <summary>
    /// Visits a named table case and its values.
    /// </summary>
    /// <param name="syntax">The case.</param>
    public virtual void VisitSpecificationCase(SpecificationCaseSyntax syntax)
    {
        VisitNode(syntax);
        foreach (var value in syntax.Values) VisitPropertyMapping(value);
    }

    /// <summary>
    /// Visits a <see cref="SpecificationClockSyntax"/> node.
    /// </summary>
    /// <param name="syntax">The <see cref="SpecificationClockSyntax"/> to visit.</param>
    public virtual void VisitSpecificationClock(SpecificationClockSyntax syntax) => VisitNode(syntax);

    /// <summary>
    /// Visits a <see cref="SpecificationTriggerSyntax"/> node.
    /// </summary>
    /// <param name="syntax">The <see cref="SpecificationTriggerSyntax"/> to visit.</param>
    public virtual void VisitSpecificationTrigger(SpecificationTriggerSyntax syntax)
    {
        VisitNode(syntax);
        foreach (var value in syntax.Values)
        {
            VisitPropertyMapping(value);
        }
    }

    /// <summary>
    /// Visits a <see cref="SpecificationCaptureSyntax"/> node.
    /// </summary>
    /// <param name="syntax">The <see cref="SpecificationCaptureSyntax"/> to visit.</param>
    public virtual void VisitSpecificationCapture(SpecificationCaptureSyntax syntax)
    {
        VisitNode(syntax);
        foreach (var value in syntax.Record)
        {
            VisitPropertyMapping(value);
        }
    }

    /// <summary>
    /// Visits a <see cref="SpecificationWhenQuerySyntax"/> node.
    /// </summary>
    /// <param name="syntax">The <see cref="SpecificationWhenQuerySyntax"/> to visit.</param>
    public virtual void VisitSpecificationWhenQuery(SpecificationWhenQuerySyntax syntax)
    {
        VisitNode(syntax);
        foreach (var argument in syntax.Arguments)
        {
            VisitPropertyMapping(argument);
        }
    }

    /// <summary>
    /// Visits a <see cref="SpecificationNoResultSyntax"/> node.
    /// </summary>
    /// <param name="syntax">The <see cref="SpecificationNoResultSyntax"/> to visit.</param>
    public virtual void VisitSpecificationNoResult(SpecificationNoResultSyntax syntax) => VisitNode(syntax);
}

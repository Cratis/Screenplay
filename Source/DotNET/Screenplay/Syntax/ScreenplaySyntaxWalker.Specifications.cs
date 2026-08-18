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

        foreach (var @event in syntax.ThenEvents)
        {
            VisitSpecificationEvent(@event);
        }

        foreach (var readModel in syntax.ThenReadModels ?? [])
        {
            VisitSpecificationReadModel(readModel);
        }

        foreach (var error in syntax.ThenErrors)
        {
            VisitSpecificationError(error);
        }
    }

    /// <summary>
    /// Visits a <see cref="SpecificationEventSyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="SpecificationEventSyntax"/> to visit.</param>
    public virtual void VisitSpecificationEvent(SpecificationEventSyntax syntax)
    {
        VisitNode(syntax);

        foreach (var value in syntax.Values)
        {
            VisitPropertyMapping(value);
        }
    }

    /// <summary>
    /// Visits a <see cref="SpecificationCommandSyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="SpecificationCommandSyntax"/> to visit.</param>
    public virtual void VisitSpecificationCommand(SpecificationCommandSyntax syntax)
    {
        VisitNode(syntax);

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

    /// <summary>
    /// Visits a <see cref="SpecificationErrorSyntax"/> node.
    /// </summary>
    /// <param name="syntax">The <see cref="SpecificationErrorSyntax"/> to visit.</param>
    public virtual void VisitSpecificationError(SpecificationErrorSyntax syntax) => VisitNode(syntax);
}

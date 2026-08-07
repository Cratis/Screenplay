// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Traversal of the document level declarations - concepts, types, events, personas, authentication and seeds.
/// </summary>
public abstract partial class ScreenplaySyntaxWalker
{
    /// <summary>
    /// Visits a <see cref="ConceptSyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="ConceptSyntax"/> to visit.</param>
    public virtual void VisitConcept(ConceptSyntax syntax)
    {
        VisitNode(syntax);

        foreach (var attribute in syntax.Attributes)
        {
            VisitConceptAttribute(attribute);
        }

        foreach (var validation in syntax.Validations ?? [])
        {
            VisitValidate(validation);
        }
    }

    /// <summary>
    /// Visits a <see cref="ConceptAttributeSyntax"/> node.
    /// </summary>
    /// <param name="syntax">The <see cref="ConceptAttributeSyntax"/> to visit.</param>
    public virtual void VisitConceptAttribute(ConceptAttributeSyntax syntax) => VisitNode(syntax);

    /// <summary>
    /// Visits a <see cref="TypeSyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="TypeSyntax"/> to visit.</param>
    public virtual void VisitType(TypeSyntax syntax)
    {
        VisitNode(syntax);

        foreach (var property in syntax.Properties)
        {
            VisitProperty(property);
        }
    }

    /// <summary>
    /// Visits an <see cref="EventSyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="EventSyntax"/> to visit.</param>
    public virtual void VisitEvent(EventSyntax syntax)
    {
        VisitNode(syntax);

        foreach (var property in syntax.Properties)
        {
            VisitProperty(property);
        }

        foreach (var tag in syntax.Tags ?? [])
        {
            VisitTag(tag);
        }
    }

    /// <summary>
    /// Visits a <see cref="PersonaSyntax"/> node.
    /// </summary>
    /// <param name="syntax">The <see cref="PersonaSyntax"/> to visit.</param>
    public virtual void VisitPersona(PersonaSyntax syntax) => VisitNode(syntax);

    /// <summary>
    /// Visits a <see cref="ReadModelSyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="ReadModelSyntax"/> to visit.</param>
    public virtual void VisitReadModel(ReadModelSyntax syntax)
    {
        VisitNode(syntax);

        foreach (var property in syntax.Properties)
        {
            VisitProperty(property);
        }
    }

    /// <summary>
    /// Visits a <see cref="ReducerSyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="ReducerSyntax"/> to visit.</param>
    public virtual void VisitReducer(ReducerSyntax syntax)
    {
        VisitNode(syntax);

        foreach (var rule in syntax.Rules)
        {
            VisitReducerRule(rule);
        }
    }

    /// <summary>
    /// Visits a <see cref="ReducerRuleSyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="ReducerRuleSyntax"/> to visit.</param>
    public virtual void VisitReducerRule(ReducerRuleSyntax syntax)
    {
        VisitNode(syntax);

        if (syntax.File is not null)
        {
            VisitFileReference(syntax.File);
        }

        if (syntax.Code is not null)
        {
            VisitCodeBlock(syntax.Code);
        }
    }

    /// <summary>
    /// Visits an <see cref="AuthenticationSyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="AuthenticationSyntax"/> to visit.</param>
    public virtual void VisitAuthentication(AuthenticationSyntax syntax)
    {
        VisitNode(syntax);

        foreach (var provider in syntax.Providers)
        {
            VisitAuthenticationProvider(provider);
        }
    }

    /// <summary>
    /// Visits an <see cref="AuthenticationProviderSyntax"/> node.
    /// </summary>
    /// <param name="syntax">The <see cref="AuthenticationProviderSyntax"/> to visit.</param>
    public virtual void VisitAuthenticationProvider(AuthenticationProviderSyntax syntax) => VisitNode(syntax);

    /// <summary>
    /// Visits a <see cref="SeedSyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="SeedSyntax"/> to visit.</param>
    public virtual void VisitSeed(SeedSyntax syntax)
    {
        VisitNode(syntax);

        foreach (var group in syntax.Groups)
        {
            VisitSeedGroup(group);
        }
    }

    /// <summary>
    /// Visits a <see cref="SeedGroupSyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="SeedGroupSyntax"/> to visit.</param>
    public virtual void VisitSeedGroup(SeedGroupSyntax syntax)
    {
        VisitNode(syntax);

        foreach (var @event in syntax.Events)
        {
            VisitSeedEvent(@event);
        }
    }

    /// <summary>
    /// Visits a <see cref="SeedEventSyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="SeedEventSyntax"/> to visit.</param>
    public virtual void VisitSeedEvent(SeedEventSyntax syntax)
    {
        VisitNode(syntax);

        foreach (var property in syntax.Properties)
        {
            VisitPropertyMapping(property);
        }
    }
}

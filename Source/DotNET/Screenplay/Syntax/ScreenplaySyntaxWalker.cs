// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Represents a walker that traverses a Screenplay syntax tree and calls one method per node kind.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="IApplicationSyntaxVisitor{TApplication}"/> family hands a consumer the root of a tree and
/// leaves the walk to it. This type is the walk: every node kind has a <c>Visit&lt;Kind&gt;</c> method whose
/// default implementation visits that node's children, so a consumer derives from this and overrides only
/// the kinds it cares about. Everything it does not override still gets traversed.
/// </para>
/// <para>
/// That default is the compatibility guarantee. A node kind added to the language later arrives as a new
/// method plus a call to it from the walk of its parent - a consumer that never asked about it keeps
/// compiling and keeps walking, because the base class already knows how to descend through it.
/// </para>
/// <para>
/// The walk is pre-order: <see cref="VisitNode"/> runs for every node before the node's own method visits
/// its children, so overriding <see cref="VisitNode"/> alone sees every node in the tree, including kinds
/// that did not exist when the consumer was written. Children are visited in the order the construct is
/// written in a <c>.play</c> document rather than the order the record declares its parameters.
/// </para>
/// <para>
/// An override that calls its <c>base</c> implementation continues into the node's children; one that does
/// not prunes that subtree. Both are supported - pruning is how a consumer skips a branch it has no use
/// for. A node kind the walker does not recognize - a type derived from one of the abstract bases outside
/// this assembly, or one from a newer version of the language - is visited as a node and then left alone
/// rather than raising an error.
/// </para>
/// <para>
/// <see cref="VisitApplication"/>, <see cref="VisitProjection"/>, <see cref="VisitSpecification"/> and
/// <see cref="VisitCapture"/> are the four roots, matching the four entry points on
/// <see cref="IScreenplayCompiler"/>. Every other method is public too, so a walk can also start part way
/// down - at a single slice or a single screen, say.
/// </para>
/// </remarks>
public abstract partial class ScreenplaySyntaxWalker
{
    /// <summary>
    /// Visits any <see cref="SyntaxNode"/>, before the node's own method visits its children.
    /// </summary>
    /// <param name="node">The <see cref="SyntaxNode"/> being visited.</param>
    /// <remarks>
    /// Every node in the tree passes through here, whatever its kind. Override this to act on all of them
    /// at once; the default implementation does nothing.
    /// </remarks>
    public virtual void VisitNode(SyntaxNode node)
    {
    }

    /// <summary>
    /// Visits an <see cref="ApplicationSyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="ApplicationSyntax"/> to visit.</param>
    public virtual void VisitApplication(ApplicationSyntax syntax)
    {
        VisitNode(syntax);

        if (syntax.Domain is not null)
        {
            VisitDomain(syntax.Domain);
        }

        foreach (var import in syntax.Imports)
        {
            VisitImport(import);
        }

        foreach (var concept in syntax.Concepts)
        {
            VisitConcept(concept);
        }

        foreach (var type in syntax.Types ?? [])
        {
            VisitType(type);
        }

        foreach (var policy in syntax.Policies)
        {
            VisitPolicy(policy);
        }

        foreach (var persona in syntax.Personas ?? [])
        {
            VisitPersona(persona);
        }

        if (syntax.Authentication is not null)
        {
            VisitAuthentication(syntax.Authentication);
        }

        foreach (var uiProfile in syntax.UiProfiles ?? [])
        {
            VisitUiProfile(uiProfile);
        }

        foreach (var module in syntax.Modules)
        {
            VisitModule(module);
        }

        foreach (var seed in syntax.Seeds ?? [])
        {
            VisitSeed(seed);
        }
    }

    /// <summary>
    /// Visits a <see cref="DomainSyntax"/> node.
    /// </summary>
    /// <param name="syntax">The <see cref="DomainSyntax"/> to visit.</param>
    public virtual void VisitDomain(DomainSyntax syntax) => VisitNode(syntax);

    /// <summary>
    /// Visits an <see cref="ImportSyntax"/> node.
    /// </summary>
    /// <param name="syntax">The <see cref="ImportSyntax"/> to visit.</param>
    public virtual void VisitImport(ImportSyntax syntax) => VisitNode(syntax);

    /// <summary>
    /// Visits a <see cref="ModuleSyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="ModuleSyntax"/> to visit.</param>
    public virtual void VisitModule(ModuleSyntax syntax)
    {
        VisitNode(syntax);

        foreach (var layout in syntax.Layouts)
        {
            VisitLayout(layout);
        }

        foreach (var form in syntax.Forms ?? [])
        {
            VisitForm(form);
        }

        foreach (var feature in syntax.Features)
        {
            VisitFeature(feature);
        }
    }

    /// <summary>
    /// Visits a <see cref="LayoutSyntax"/> node.
    /// </summary>
    /// <param name="syntax">The <see cref="LayoutSyntax"/> to visit.</param>
    public virtual void VisitLayout(LayoutSyntax syntax) => VisitNode(syntax);

    /// <summary>
    /// Visits a <see cref="FormSyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="FormSyntax"/> to visit.</param>
    public virtual void VisitForm(FormSyntax syntax)
    {
        VisitNode(syntax);

        foreach (var field in syntax.Fields)
        {
            VisitFormField(field);
        }
    }

    /// <summary>
    /// Visits a <see cref="FormFieldSyntax"/> node.
    /// </summary>
    /// <param name="syntax">The <see cref="FormFieldSyntax"/> to visit.</param>
    public virtual void VisitFormField(FormFieldSyntax syntax) => VisitNode(syntax);

    /// <summary>
    /// Visits a <see cref="FeatureSyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="FeatureSyntax"/> to visit.</param>
    public virtual void VisitFeature(FeatureSyntax syntax)
    {
        VisitNode(syntax);

        foreach (var feature in syntax.Features)
        {
            VisitFeature(feature);
        }

        foreach (var slice in syntax.Slices)
        {
            VisitSlice(slice);
        }
    }

    /// <summary>
    /// Visits a <see cref="SliceSyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="SliceSyntax"/> to visit.</param>
    public virtual void VisitSlice(SliceSyntax syntax)
    {
        VisitNode(syntax);

        foreach (var command in syntax.Commands)
        {
            VisitCommand(command);
        }

        foreach (var @event in syntax.Events)
        {
            VisitEvent(@event);
        }

        foreach (var constraint in syntax.Constraints)
        {
            VisitConstraint(constraint);
        }

        foreach (var query in syntax.Queries)
        {
            VisitQuery(query);
        }

        foreach (var projection in syntax.Projections)
        {
            VisitProjection(projection);
        }

        foreach (var readModel in syntax.ReadModels ?? [])
        {
            VisitReadModel(readModel);
        }

        foreach (var reducer in syntax.Reducers ?? [])
        {
            VisitReducer(reducer);
        }

        foreach (var capture in syntax.Captures)
        {
            VisitCapture(capture);
        }

        foreach (var reactor in syntax.Reactors)
        {
            VisitReactor(reactor);
        }

        foreach (var screen in syntax.Screens)
        {
            VisitScreen(screen);
        }

        foreach (var specification in syntax.Specifications)
        {
            VisitSpecification(specification);
        }
    }

    /// <summary>
    /// Visits a <see cref="PropertySyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="PropertySyntax"/> to visit.</param>
    public virtual void VisitProperty(PropertySyntax syntax)
    {
        VisitNode(syntax);
        VisitTypeRef(syntax.Type);
    }

    /// <summary>
    /// Visits a <see cref="TypeRefSyntax"/> node.
    /// </summary>
    /// <param name="syntax">The <see cref="TypeRefSyntax"/> to visit.</param>
    public virtual void VisitTypeRef(TypeRefSyntax syntax) => VisitNode(syntax);

    /// <summary>
    /// Visits a <see cref="TagSyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="TagSyntax"/> to visit.</param>
    public virtual void VisitTag(TagSyntax syntax)
    {
        VisitNode(syntax);
        VisitExpression(syntax.Value);
    }

    /// <summary>
    /// Visits a <see cref="CodeBlockSyntax"/> node.
    /// </summary>
    /// <param name="syntax">The <see cref="CodeBlockSyntax"/> to visit.</param>
    public virtual void VisitCodeBlock(CodeBlockSyntax syntax) => VisitNode(syntax);

    /// <summary>
    /// Visits a <see cref="FileReferenceSyntax"/> node.
    /// </summary>
    /// <param name="syntax">The <see cref="FileReferenceSyntax"/> to visit.</param>
    public virtual void VisitFileReference(FileReferenceSyntax syntax) => VisitNode(syntax);
}

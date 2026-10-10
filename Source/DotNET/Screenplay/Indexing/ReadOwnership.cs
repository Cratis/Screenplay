// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Captures;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Indexing;

sealed class ReadOwnership : ScreenplaySyntaxWalker
{
    readonly Dictionary<SyntaxNode, SyntaxNode> _owners = new(ReferenceEqualityComparer.Instance);
    SyntaxNode? _owner;

    /// <inheritdoc/>
    public override void VisitNode(SyntaxNode node)
    {
        if (_owner is not null)
        {
            _owners[node] = _owner;
        }
    }

    /// <inheritdoc/>
    public override void VisitIdentity(IdentitySyntax syntax) => Owned(syntax, () => base.VisitIdentity(syntax));

    /// <inheritdoc/>
    public override void VisitIdentityDetail(IdentityDetailSyntax syntax) => Owned(syntax, () => base.VisitIdentityDetail(syntax));

    /// <inheritdoc/>
    public override void VisitModule(ModuleSyntax syntax) => Owned(syntax, () => base.VisitModule(syntax));

    /// <inheritdoc/>
    public override void VisitFeature(FeatureSyntax syntax) => Owned(syntax, () => base.VisitFeature(syntax));

    /// <inheritdoc/>
    public override void VisitSlice(SliceSyntax syntax) => Owned(syntax, () => base.VisitSlice(syntax));

    /// <inheritdoc/>
    public override void VisitCommand(CommandSyntax syntax) => Owned(syntax, () => base.VisitCommand(syntax));

    /// <inheritdoc/>
    public override void VisitQuery(QuerySyntax syntax) => Owned(syntax, () => base.VisitQuery(syntax));

    /// <inheritdoc/>
    public override void VisitOperation(OperationSyntax syntax) => Owned(syntax, () => base.VisitOperation(syntax));

    /// <inheritdoc/>
    public override void VisitSystem(SystemSyntax syntax) => Owned(syntax, () => base.VisitSystem(syntax));

    /// <inheritdoc/>
    public override void VisitEventSource(EventSourceSyntax syntax) => Owned(syntax, () => base.VisitEventSource(syntax));

    /// <inheritdoc/>
    public override void VisitEventStream(EventStreamSyntax syntax) => Owned(syntax, () => base.VisitEventStream(syntax));

    /// <inheritdoc/>
    public override void VisitEvent(EventSyntax syntax) => Owned(syntax, () => base.VisitEvent(syntax));

    /// <inheritdoc/>
    public override void VisitReadModel(ReadModelSyntax syntax) => Owned(syntax, () => base.VisitReadModel(syntax));

    /// <inheritdoc/>
    public override void VisitScreen(ScreenSyntax syntax) => Owned(syntax, () => base.VisitScreen(syntax));

    /// <inheritdoc/>
    public override void VisitConcept(ConceptSyntax syntax) => Owned(syntax, () => base.VisitConcept(syntax));

    /// <inheritdoc/>
    public override void VisitType(TypeSyntax syntax) => Owned(syntax, () => base.VisitType(syntax));

    /// <inheritdoc/>
    public override void VisitPolicy(PolicySyntax syntax) => Owned(syntax, () => base.VisitPolicy(syntax));

    /// <inheritdoc/>
    public override void VisitPersona(PersonaSyntax syntax) => Owned(syntax, () => base.VisitPersona(syntax));

    /// <inheritdoc/>
    public override void VisitLayout(LayoutSyntax syntax) => Owned(syntax, () => base.VisitLayout(syntax));

    /// <inheritdoc/>
    public override void VisitScreenTemplate(ScreenTemplateSyntax syntax) => Owned(syntax, () => base.VisitScreenTemplate(syntax));

    /// <inheritdoc/>
    public override void VisitDialogTemplate(DialogTemplateSyntax syntax) => Owned(syntax, () => base.VisitDialogTemplate(syntax));

    /// <inheritdoc/>
    public override void VisitForm(FormSyntax syntax) => Owned(syntax, () => base.VisitForm(syntax));

    /// <inheritdoc/>
    public override void VisitUiProfile(UiProfileSyntax syntax) => Owned(syntax, () => base.VisitUiProfile(syntax));

    /// <inheritdoc/>
    public override void VisitTheme(ThemeSyntax syntax) => Owned(syntax, () => base.VisitTheme(syntax));

    /// <inheritdoc/>
    public override void VisitTrigger(TriggerSyntax syntax) => Owned(syntax, () => base.VisitTrigger(syntax));

    /// <inheritdoc/>
    public override void VisitReaction(ReactionSyntax syntax) => Owned(syntax, () => base.VisitReaction(syntax));

    /// <inheritdoc/>
    public override void VisitProjection(ProjectionSyntax syntax) => Owned(syntax, () => base.VisitProjection(syntax));

    /// <inheritdoc/>
    public override void VisitReducer(ReducerSyntax syntax) => Owned(syntax, () => base.VisitReducer(syntax));

    /// <inheritdoc/>
    public override void VisitCapture(CaptureSyntax syntax) => Owned(syntax, () => base.VisitCapture(syntax));

    /// <inheritdoc/>
    public override void VisitConstraint(ConstraintSyntax syntax)
    {
        if (_owner is ConstraintSyntax)
        {
            base.VisitConstraint(syntax);
        }
        else
        {
            Owned(syntax, () => base.VisitConstraint(syntax));
        }
    }

    /// <inheritdoc/>
    public override void VisitSpecification(SpecificationSyntax syntax) => Owned(syntax, () => base.VisitSpecification(syntax));

    /// <inheritdoc/>
    public override void VisitSpecificationExample(SpecificationExampleSyntax syntax) => Owned(syntax, () => base.VisitSpecificationExample(syntax));

    internal SyntaxNode? For(SyntaxNode node) => _owners.GetValueOrDefault(node);

    void Owned(SyntaxNode owner, Action visit)
    {
        var previous = _owner;
        _owner = owner;
        visit();
        _owner = previous;
    }
}

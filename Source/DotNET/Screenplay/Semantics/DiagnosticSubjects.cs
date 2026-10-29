// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Captures;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Attaches the owning declaration as the <c>subject</c> argument of binder refusals, by source position.
/// </summary>
/// <remarks>
/// Reporting sites do not know their enclosing declaration, and some refusals are produced before binding starts or
/// re-coded afterwards, so attribution runs once over everything <see cref="SemanticModelBinder"/> returns. A diagnostic is
/// attributed to the innermost declaration at its exact location, else at a node on the same line of the same file.
/// A diagnostic with neither, such as a document-level refusal, carries no subject.
/// </remarks>
sealed class DiagnosticSubjects(bool includeImports = false) : ScreenplaySyntaxWalker
{
    const string Subject = "subject";

    readonly Dictionary<SourceLocation, DiagnosticArgument> _exact = [];
    readonly Dictionary<(string? Path, int Line), DiagnosticArgument> _lines = [];
    readonly List<string> _scope = [];
    DiagnosticArgument? _owner;

    /// <inheritdoc/>
    public override void VisitNode(SyntaxNode node)
    {
        if (_owner is null) return;
        _exact[node.Location] = _owner;
        _lines[(node.Location.Path, node.Location.Line)] = _owner;
        foreach (var location in node.DirectiveLocations.Values)
        {
            _exact[location] = _owner;
            _lines[(location.Path, location.Line)] = _owner;
        }
    }

    /// <inheritdoc/>
    public override void VisitImport(ImportSyntax syntax)
    {
        if (includeImports) Scoped(syntax, "import", syntax.QualifiedName, false, () => base.VisitImport(syntax));
        else base.VisitImport(syntax);
    }

    /// <inheritdoc/>
    public override void VisitModule(ModuleSyntax syntax) => Scoped(syntax, "module", syntax.Name, true, () => base.VisitModule(syntax));

    /// <inheritdoc/>
    public override void VisitFeature(FeatureSyntax syntax) => Scoped(syntax, "feature", syntax.Name, true, () => base.VisitFeature(syntax));

    /// <inheritdoc/>
    public override void VisitSlice(SliceSyntax syntax) => Scoped(syntax, "slice", syntax.Name, true, () => base.VisitSlice(syntax));

    /// <inheritdoc/>
    public override void VisitCommand(CommandSyntax syntax) => Scoped(syntax, "command", syntax.Name, false, () => base.VisitCommand(syntax));

    /// <inheritdoc/>
    public override void VisitQuery(QuerySyntax syntax) => Scoped(syntax, "query", syntax.Name, false, () => base.VisitQuery(syntax));

    /// <inheritdoc/>
    public override void VisitOperation(OperationSyntax syntax) => Scoped(syntax, "operation", syntax.Name, false, () => base.VisitOperation(syntax));

    /// <inheritdoc/>
    public override void VisitSystem(SystemSyntax syntax) => Scoped(syntax, "system", syntax.Name, false, () => base.VisitSystem(syntax));

    /// <inheritdoc/>
    public override void VisitEventSource(EventSourceSyntax syntax) => Scoped(syntax, "eventsource", syntax.Name, false, () => base.VisitEventSource(syntax));

    /// <inheritdoc/>
    public override void VisitEvent(EventSyntax syntax) => Scoped(syntax, "event", syntax.Name, false, () => base.VisitEvent(syntax));

    /// <inheritdoc/>
    public override void VisitReadModel(ReadModelSyntax syntax) => Scoped(syntax, "readmodel", syntax.Name, false, () => base.VisitReadModel(syntax));

    /// <inheritdoc/>
    public override void VisitScreen(ScreenSyntax syntax) => Scoped(syntax, "screen", syntax.Name, false, () => base.VisitScreen(syntax));

    /// <inheritdoc/>
    public override void VisitConcept(ConceptSyntax syntax) => Scoped(syntax, "concept", syntax.Name, false, () => base.VisitConcept(syntax));

    /// <inheritdoc/>
    public override void VisitType(TypeSyntax syntax) => Scoped(syntax, "type", syntax.Name, false, () => base.VisitType(syntax));

    /// <inheritdoc/>
    public override void VisitPolicy(PolicySyntax syntax) => Scoped(syntax, "policy", syntax.Name, false, () => base.VisitPolicy(syntax));

    /// <inheritdoc/>
    public override void VisitPersona(PersonaSyntax syntax) => Scoped(syntax, "persona", syntax.Name, false, () => base.VisitPersona(syntax));

    /// <inheritdoc/>
    public override void VisitTrigger(TriggerSyntax syntax) => Scoped(syntax, "trigger", syntax.Name, false, () => base.VisitTrigger(syntax));

    /// <inheritdoc/>
    public override void VisitReaction(ReactionSyntax syntax) => Scoped(syntax, "reaction", syntax.Name, false, () => base.VisitReaction(syntax));

    /// <inheritdoc/>
    public override void VisitProjection(ProjectionSyntax syntax) => Scoped(syntax, "projection", syntax.Name, false, () => base.VisitProjection(syntax));

    /// <inheritdoc/>
    public override void VisitReducer(ReducerSyntax syntax) => Scoped(syntax, "reducer", syntax.Name, false, () => base.VisitReducer(syntax));

    /// <inheritdoc/>
    public override void VisitCapture(CaptureSyntax syntax) => Scoped(syntax, "capture", syntax.Name, false, () => base.VisitCapture(syntax));

    /// <inheritdoc/>
    public override void VisitSpecification(SpecificationSyntax syntax) => Scoped(syntax, "specification", syntax.Name, false, () => base.VisitSpecification(syntax));

    /// <inheritdoc/>
    public override void VisitSpecificationExample(SpecificationExampleSyntax syntax) => Scoped(syntax, "example", syntax.Name, false, () => base.VisitSpecificationExample(syntax));

    /// <inheritdoc/>
    /// <remarks>
    /// Attribution only needs the locations of the hints, so hints a caller built programmatically without a list or
    /// with a missing entry, which the binder refuses with its own diagnostic, are skipped instead of walked.
    /// </remarks>
    public override void VisitImplementation(ImplementationSyntax syntax)
    {
        VisitNode(syntax);
        foreach (var hint in (syntax.Hints ?? []).Where(hint => hint is not null)) VisitImplementationHint(hint);
    }

    internal static Func<SourceLocation, DiagnosticArgument?> CreateLocator(ApplicationSyntax syntax)
    {
        var walker = new DiagnosticSubjects(includeImports: true);
        walker.VisitApplication(syntax);
        return walker.Find;
    }

    /// <summary>
    /// Attributes every subject-less <see cref="DiagnosticCodes.UnsupportedSemanticSyntax"/> diagnostic to its declaration.
    /// </summary>
    /// <param name="syntax">The syntax that was bound.</param>
    /// <param name="diagnostics">The diagnostics the binder returned.</param>
    /// <returns>The diagnostics with subjects attached where one could be found.</returns>
    internal static IReadOnlyList<Diagnostic> Attach(ApplicationSyntax syntax, IEnumerable<Diagnostic> diagnostics)
    {
        var all = diagnostics.ToList();
        if (!all.Exists(Needs)) return all;
        var walker = new DiagnosticSubjects();
        walker.VisitApplication(syntax);
        return [.. all.Select(diagnostic => Needs(diagnostic) && walker.Find(diagnostic.Location) is { } subject
            ? diagnostic with { Arguments = diagnostic.Arguments.With(Subject, subject) }
            : diagnostic)];
    }

    static bool Needs(Diagnostic diagnostic) => diagnostic.Code == DiagnosticCodes.UnsupportedSemanticSyntax && !diagnostic.Arguments.ContainsKey(Subject);

    DiagnosticArgument? Find(SourceLocation location) =>
        _exact.GetValueOrDefault(location) ?? _lines.GetValueOrDefault((location.Path, location.Line));

    void Scoped(SyntaxNode node, string kind, string name, bool opensScope, Action visit)
    {
        var previous = _owner;
        var depth = _scope.Count;
        _owner = DiagnosticArgument.Declaration(kind, [.. _scope, name]);
        if (opensScope) _scope.Add(name);
        visit();
        _scope.RemoveRange(depth, _scope.Count - depth);
        _owner = previous;
    }
}

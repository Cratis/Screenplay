// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics;

sealed class AdmissibleCompilation(IScreenplayCompiler compiler, ISemanticModelBinder binder)
{
    internal AdmissibleCompilationResult Compile(string applicationName, SemanticDocumentSet documents)
    {
        var texts = documents.Documents.ToDictionary(document => document.DisplayPath, document => document.Text, StringComparer.Ordinal);
        var (_, parsed) = PlayApplicationAssembly.Compile(compiler, texts.Keys, new InMemoryPlayDocumentSource(texts));
        return parsed.Success ? BindSubset(applicationName, parsed, documents) : new(CompilationResult<SemanticCompilation>.Failed(parsed.Diagnostics), []);
    }

    static bool ApplicationRefusal(ApplicationSyntax syntax) =>
        syntax.SourceOptions.NumericMode == NumericMode.Exact ||
        (syntax.ReactionProductionMode is { } mode && mode != ReactionProductionMode.Atomic);

    static AdmissibleCompilationResult Result(CompilationResult<SemanticCompilation> compilation, List<SemanticExclusion> exclusions) =>
        new(compilation, [.. exclusions.OrderBy(exclusion => exclusion.Location.Path, StringComparer.Ordinal)
            .ThenBy(exclusion => exclusion.Location.Line).ThenBy(exclusion => exclusion.Location.Column)
            .ThenBy(exclusion => exclusion.Kind).ThenBy(exclusion => exclusion.Address, StringComparer.Ordinal)]);

    static (ApplicationSyntax Syntax, IEnumerable<SemanticExclusion> Exclusions)? TryPrune(ApplicationSyntax syntax, SemanticIdentityCatalog identities, IEnumerable<Diagnostic> diagnostics)
    {
        var index = new AdmissibleSyntax(syntax, identities);
        index.Index();
        var errors = diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();
        if (errors.Length == 0) return null;
        var plan = new AdmissibleExclusions(index);
        if (!new AdmissibleSecurity(syntax, index).TryPlan(errors, plan)) return null;
        if (plan.Removed.Count == 0) return null;
        return (index.Remove(plan.Removed), plan.Entries);
    }

    AdmissibleCompilationResult BindSubset(string applicationName, CompilationResult<ApplicationSyntax> parsed, SemanticDocumentSet documents)
    {
        var syntax = parsed.Value!;
        var exclusions = new List<SemanticExclusion>();
        CompilationResult<SemanticCompilation>? original = null;
        while (true)
        {
            var compilation = Bind(applicationName, syntax, documents, parsed.Diagnostics);
            original ??= compilation;
            if (compilation.Success) return Result(compilation, exclusions);
            if (ApplicationRefusal(syntax)) return new(original, []);
            if (TryPrune(syntax, documents.IdentityCatalog, compilation.Diagnostics) is not { } pruned) return new(original, []);
            syntax = pruned.Syntax;
            exclusions.AddRange(pruned.Exclusions);
        }
    }

    CompilationResult<SemanticCompilation> Bind(string applicationName, ApplicationSyntax syntax, SemanticDocumentSet documents, IEnumerable<Diagnostic> sourceDiagnostics)
    {
        var bound = binder.Bind(applicationName, syntax, documents);
        IReadOnlyList<Diagnostic> diagnostics = [.. sourceDiagnostics, .. bound.Diagnostics];
        return (bound.Success ? new CompilationResult<SemanticCompilation>(bound.Value, diagnostics) : CompilationResult<SemanticCompilation>.Failed(diagnostics)) with
        {
            ImplementationRequirements = bound.ImplementationRequirements,
            TypedContextDescriptors = bound.TypedContextDescriptors
        };
    }
}

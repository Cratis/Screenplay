// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_reporting_contract_failures;

public class and_two_declarations_fail : Specification
{
    CompilationResult<SemanticCompilation> _result;
    SemanticDocumentSet _documents;

    void Establish()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        const string root = "domain Projects\nimport \"Alpha.play\"\nimport \"Beta.play\"";
        _documents = SemanticDocumentSet.Create(
            [Document("application", root), Document("Alpha", Module("Alpha")), Document("Beta", Module("Beta"))], catalog);

        SemanticSourceDocument Document(string key, string text) => SemanticSourceDocument.Create(catalog.ResolveDocument(key), key, $"{key}.play", text);
    }

    void Because() => _result = new SemanticModelCompiler().Compile("Projects", _documents);

    [Fact] void should_fail_binding() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_both_independent_contract_failures() => Assert.True(Failures.Count() == 2, string.Join('\n', _result.Diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}")));
    [Fact] void should_locate_the_first_specification() => Failures.ShouldContain(diagnostic => diagnostic.Location == new SourceLocation(8, 7, "Alpha.play"));
    [Fact] void should_locate_the_second_specification() => Failures.ShouldContain(diagnostic => diagnostic.Location == new SourceLocation(8, 7, "Beta.play"));
    [Fact] void should_not_blame_any_document_root() => Failures.Any(diagnostic => diagnostic.Location.Line == 1 && diagnostic.Location.Column == 1).ShouldBeFalse();
    [Fact] void should_preserve_the_contract_messages() => Failures.All(diagnostic => diagnostic.Message == "A success specification must contain at least one success outcome.").ShouldBeTrue();

    IEnumerable<Diagnostic> Failures => _result.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidSemanticBinding);

    static string Module(string name) => $$"""
        module {{name}}
          feature Recording
            slice StateChange Record{{name}}
              command Record{{name}}
                produces {{name}}Recorded
              event {{name}}Recorded

              specification MissingOutcome
                when Record{{name}}
        """;
}

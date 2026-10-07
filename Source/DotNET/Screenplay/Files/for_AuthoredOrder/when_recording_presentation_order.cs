// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Files.for_AuthoredOrder;

public class when_recording_presentation_order : Specification
{
    string _syntaxBefore;
    string _syntaxAfter;
    byte[] _bytesBefore;
    byte[] _bytesAfter;
    IReadOnlyDictionary<string, int> _order;
    string[] _pathsBefore;
    string[] _pathsAfter;
    string[] _findings;

    void Because()
    {
        var compiler = new ScreenplayCompiler();
        var source = new InMemoryPlayDocumentSource(new Dictionary<string, string>
        {
            ["application.play"] = "import \"z.play\"\nimport \"a.play\"",
            ["z.play"] = "module Z\n  feature F\n    slice StateView View\n      readmodel P\n        id Uuid\n      query ById => P optional\n        by id Uuid\n      projection P => P\n        from E key id",
            ["a.play"] = "module A\n  feature F\n    slice StateChange Write\n      event E\n        id Uuid"
        });
        var (baselineDocuments, _) = PlayImports.Resolve(["application.play"], source, compiler.Languages);
        var baseline = PlayFolderMerge.Merge([.. baselineDocuments.Select(document => compiler.Parse(document.Source, document.Path, document.Placement))]);
        var (documents, result) = PlayApplicationAssembly.Compile(compiler, ["application.play"], source);
        _syntaxBefore = SyntaxJson.Serialize(baseline.Value!).GetRawText();
        _syntaxAfter = SyntaxJson.Serialize(result.Value!).GetRawText();
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Order"));
        var sources = SemanticDocumentSet.Create([.. baselineDocuments.Select(document => SemanticSourceDocument.Create(catalog.ResolveDocument(document.Path), document.Path, document.Path, document.Source))], catalog);
        var boundBefore = new SemanticModelBinder().Bind("Order", baseline.Value!, sources);
        string.Join('\n', boundBefore.Diagnostics.Where(diagnostic => diagnostic.Severity == Diagnostics.DiagnosticSeverity.Error).Select(diagnostic => diagnostic.Message)).ShouldEqual(string.Empty);
        var boundAfter = new SemanticModelBinder().Bind("Order", result.Value!, sources);
        string.Join('\n', boundAfter.Diagnostics.Where(diagnostic => diagnostic.Severity == Diagnostics.DiagnosticSeverity.Error).Select(diagnostic => diagnostic.Message)).ShouldEqual(string.Empty);
        _bytesBefore = SemanticModelSerializer.Serialize(boundBefore.Value!.Model);
        _bytesAfter = SemanticModelSerializer.Serialize(boundAfter.Value!.Model);
        _order = AuthoredOrder.Record(["application.play"], documents, compiler.Languages);
        _pathsBefore = [.. baselineDocuments.Select(document => document.Path)];
        _pathsAfter = [.. documents.Select(document => document.Path)];
        _findings = [.. result.Diagnostics.Select(diagnostic => diagnostic.Code)];
    }

    [Fact] void should_rank_authored_imports_before_path_order() => _order["[\"Z\"]"].ShouldBeLessThan(_order["[\"A\"]"]);
    [Fact] void should_not_change_syntax_json() => _syntaxAfter.ShouldEqual(_syntaxBefore);
    [Fact] void should_not_change_semantic_bytes() => _bytesAfter.ShouldEqual(_bytesBefore);
    [Fact] void should_not_change_document_order() => _pathsAfter.ShouldEqual(_pathsBefore);
    [Fact] void should_exercise_the_timeline_pass() => _findings.ShouldContainOnly("PLAY0516");
}

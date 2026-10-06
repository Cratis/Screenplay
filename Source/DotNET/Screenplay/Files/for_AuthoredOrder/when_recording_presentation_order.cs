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

    void Because()
    {
        var compiler = new ScreenplayCompiler();
        var (documents, result) = PlayApplicationAssembly.Compile(compiler, ["application.play"], new InMemoryPlayDocumentSource(new Dictionary<string, string>
        {
            ["application.play"] = "import \"z.play\"\nimport \"a.play\"",
            ["z.play"] = "module Z\n  feature F\n    slice StateChange S\n      event E",
            ["a.play"] = "module A\n  feature F\n    slice StateChange S\n      event Other"
        }));
        var application = result.Value!;
        _syntaxBefore = SyntaxJson.Serialize(application).GetRawText();
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Order"));
        var sources = SemanticDocumentSet.Create([.. documents.Select(document => SemanticSourceDocument.Create(catalog.ResolveDocument(document.Path), document.Path, document.Path, document.Source))], catalog);
        _bytesBefore = SemanticModelSerializer.Serialize(new SemanticModelBinder().Bind("Order", application, sources).Value!.Model);
        _order = AuthoredOrder.Record(["application.play"], documents, compiler.Languages);
        _syntaxAfter = SyntaxJson.Serialize(application).GetRawText();
        _bytesAfter = SemanticModelSerializer.Serialize(new SemanticModelBinder().Bind("Order", application, sources).Value!.Model);
    }

    [Fact] void should_rank_authored_imports_before_path_order() => _order["[\"Z\"]"].ShouldBeLessThan(_order["[\"A\"]"]);
    [Fact] void should_not_change_syntax_json() => _syntaxAfter.ShouldEqual(_syntaxBefore);
    [Fact] void should_not_change_semantic_bytes() => _bytesAfter.ShouldEqual(_bytesBefore);
}

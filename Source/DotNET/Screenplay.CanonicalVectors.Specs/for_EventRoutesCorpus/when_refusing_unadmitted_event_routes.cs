// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Text.Json;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_EventRoutesCorpus;

public class when_refusing_unadmitted_event_routes : Specification
{
    [Theory]
    [InlineData("path", "PLAY0268")]
    [InlineData("production-outside-command", "PLAY0650")]
    [InlineData("production-path", "PLAY0268")]
    [InlineData("production-generated", "PLAY0273")]
    [InlineData("handler", "PLAY0268")]
    [InlineData("concurrency", "PLAY0271")]
    [InlineData("generated", "PLAY0273")]
    [InlineData("collision", "PLAY0273")]
    [InlineData("stream-collision", "PLAY0273")]
    [InlineData("default", "PLAY0273")]
    [InlineData("type", "PLAY0504")]
    [InlineData("nfc", "PLAY0504")]
    void should_pin_the_refusal_and_publish_no_model(string key, string code)
    {
        var form = EventRoutesCorpus.RejectionSource(key);
        var catalog = SemanticIdentityCatalogSerializer.Deserialize(form.IdentityCatalogBytes.AsSpan());
        var documents = form.Documents.Select(document => SemanticSourceDocument.Create(catalog.ResolveDocument(document.StableKey), document.StableKey, document.DisplayPath, document.Text));
        var result = new SemanticModelCompiler().Compile("EventRoutes", SemanticDocumentSet.Create([.. documents], catalog));
        Assert.False(result.Success, $"{key}: expected {code}, got {string.Join(';', result.Diagnostics.Select(diagnostic => diagnostic.Code))}");
        Assert.Null(result.Value);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == code);
        var diagnostics = result.Diagnostics.Select(diagnostic => new CanonicalCorpusDiagnosticExpectation { Code = diagnostic.Code, Message = diagnostic.Message }).ToArray();
        if (Environment.GetEnvironmentVariable("SCREENPLAY_REGENERATE_EVENT_ROUTES_CORPUS") == "1")
        {
            var path = Path.Combine(Root(), "Source/DotNET/Screenplay.CanonicalCorpus/Corpus/EventRoutes/rejections/expected");
            Directory.CreateDirectory(path);
            File.WriteAllBytes(Path.Combine(path, $"{key}.json"), JsonSerializer.SerializeToUtf8Bytes(diagnostics));
            Assert.Fail("Event-routes rejection vectors regenerated; review, rebuild and rerun without SCREENPLAY_REGENERATE_EVENT_ROUTES_CORPUS.");
        }
        var expected = EventRoutesCorpus.Rejection(key);
        diagnostics.SequenceEqual(expected.Diagnostics).ShouldBeTrue();
        expected.ArtifactPaths.ShouldBeEmpty();
    }

    static string Root([CallerFilePath] string path = "")
    {
        var directory = Directory.GetParent(path);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "Documentation"))) directory = directory.Parent;

        return directory!.FullName;
    }
}

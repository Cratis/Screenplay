// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_proposing_an_ambiguous_reference_move : given.an_authoring_connection
{
    const string Left = "module Left\n  feature Helper\n  feature Consumer\n    depends on Helper\n";
    const string Right = "module Right\n  feature Helper\n  feature Consumer\n";
    JsonElement _conflict;
    bool _isError;
    string _documentId = string.Empty;

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), Left + Right);
        Initialize();
    }

    void Because()
    {
        var opened = Open();
        _documentId = Page("documents", opened.GetProperty("revision").GetString())[0].GetProperty("documentId").GetString()!;
        var response = Call("propose-source", new
        {
            expectedRevision = opened.GetProperty("revision").GetString(),
            expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
            formatting = "PreserveExactSource", validation = "Authoring", referencePolicy = "Safe",
            documents = new[] { new { operation = "replace-document", documentId = _documentId, source = Left.Replace("    depends on Helper\n", string.Empty, StringComparison.Ordinal) + Right + "    depends on Helper\n" } }
        }).GetProperty("result");
        _isError = response.GetProperty("isError").GetBoolean();
        _conflict = response.GetProperty("structuredContent").GetProperty("conflicts")[0];
    }

    [Fact] void should_return_an_error() => _isError.ShouldBeTrue();
    [Fact] void should_return_an_invalid_operation() => _conflict.GetProperty("kind").GetString().ShouldEqual("InvalidOperation");
    [Fact] void should_return_the_document_identity() => _conflict.GetProperty("document").GetString().ShouldEqual(_documentId);
    [Fact] void should_return_the_document_path() => _conflict.GetProperty("path").GetString().ShouldEqual("application.play");
    [Fact] void should_name_both_occurrences() => _conflict.GetProperty("message").GetString().ShouldContain("disappeared: 'Helper'");
    [Fact] void should_name_the_introduced_occurrence() => _conflict.GetProperty("message").GetString().ShouldContain("introduced: 'Helper'");
    [Fact] void should_not_fabricate_property_candidates() => _conflict.GetProperty("candidates").GetArrayLength().ShouldEqual(0);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow.when_discovering_refactorings;

public class and_the_production_has_no_unique_identifier : given.an_authoring_connection
{
    const string NoIdentifierSource = "module Projects\n  feature Naming\n    slice StateChange Note\n      command Note\n        text String\n        produces event Noted\n          text String = text\n";

    JsonElement[] _extractions = [];
    JsonElement _refusal;

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), NoIdentifierSource);
        Initialize();
    }

    void Because()
    {
        var opened = Open();
        var revision = opened.GetProperty("revision").GetString()!;
        _extractions = [.. Page("refactorings", revision).EnumerateArray().Where(offer => offer.GetProperty("kind").GetString() == "extract-inline-event")];
        _refusal = Call("propose-extract-inline-event", new
        {
            expectedRevision = revision,
            expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
            subject = Node("EventSyntax", revision).GetProperty("handle"),
            formatting = "CanonicalizeTouchedDocuments"
        }).GetProperty("result");
    }

    [Fact] void should_not_offer_extraction() => _extractions.ShouldBeEmpty();
    [Fact] void should_refuse_the_proposal() => _refusal.GetProperty("isError").GetBoolean().ShouldBeTrue();
    [Fact] void should_report_an_invalid_operation() => _refusal.GetProperty("structuredContent").GetProperty("conflicts")[0].GetProperty("kind").GetString().ShouldEqual("InvalidOperation");
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow.when_repairing_command_productions;

public class and_a_matched_repair_is_rejected : for_McpAuthoringWorkflow.given.an_authoring_connection
{
    const string Source = """
        module Projects
          feature Registration
            slice StateChange Register
              command Register
                projectId Uuid identifier
                name String
                produces Registered
                  name = name
        """;

    [Theory]
    [InlineData("reference", DiagnosticCodes.OmittedProductionDestination, "InvalidOperation", "New unresolved", false)]
    [InlineData("routing", DiagnosticCodes.OmittedProductionDestination, "InvalidOperation", "destination", false)]
    [InlineData("consistency", DiagnosticCodes.UnknownEvent, "CompilationFailed", "valid full-language", false)]
    [InlineData("consistency", DiagnosticCodes.UnknownEvent, "CompilationFailed", "valid full-language", true)]
    void should_return_transaction_conflicts_not_unknown_repair(string scenario, string code, string kind, string message, bool discoverFirst)
    {
        var source = scenario switch
        {
            "routing" => Source + "\n      event Registered\n        name String\n",
            "consistency" => Source + "\n      specification Registers\n        when Register\n          name = \"project\"\n        then Registered\n          extra = \"value\"\n",
            _ => Source
        };
        File.WriteAllText(Path.Combine(RootPath, "application.play"), source);
        var original = File.ReadAllBytes(Path.Combine(RootPath, "application.play"));
        Initialize();
        var opened = Open();
        var revision = opened.GetProperty("revision").GetString()!;
        var subject = Node("ProducesSyntax", revision).GetProperty("handle");
        if (discoverFirst)
        {
            Page("repairs", revision).EnumerateArray().Any(repair => repair.GetProperty("diagnosticCode").GetString() == code).ShouldBeFalse();
        }

        var response = Call("propose-repair", new
        {
            expectedRevision = revision,
            expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
            diagnosticCode = code,
            subject,
            formatting = "CanonicalizeTouchedDocuments"
        });
        response.TryGetProperty("error", out _).ShouldBeFalse();
        var result = response.GetProperty("result");
        result.GetProperty("isError").GetBoolean().ShouldBeTrue();
        var content = result.GetProperty("structuredContent");
        content.GetProperty("success").GetBoolean().ShouldBeFalse();
        content.TryGetProperty("proposalId", out _).ShouldBeFalse();
        var conflict = content.GetProperty("conflicts").EnumerateArray().Single();
        conflict.GetProperty("kind").GetString().ShouldEqual(kind);
        conflict.GetProperty("message").GetString().ShouldContain(message);
        if (scenario == "consistency")
        {
            content.GetProperty("authoringDiagnostics").EnumerateArray().Any(diagnostic => diagnostic.GetProperty("code").GetString() == "PLAY0287").ShouldBeTrue();
            content.GetProperty("executableDiagnostics").EnumerateArray().ShouldBeEmpty();
            content.GetProperty("executableReady").GetBoolean().ShouldBeFalse();
        }
        File.ReadAllBytes(Path.Combine(RootPath, "application.play")).SequenceEqual(original).ShouldBeTrue();
    }
}

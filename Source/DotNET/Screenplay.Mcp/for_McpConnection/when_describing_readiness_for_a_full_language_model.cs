// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_describing_readiness_for_a_full_language_model : given.a_connection
{
    const string FullLanguageSource = """
        concept ProjectId : Uuid
        concept ProjectName : String
        module Projects
          feature Registration
            slice StateChange RegisterProject
              command RegisterProject
                projectId ProjectId identifier
                name ProjectName
                produces ProjectRegistered
                  for projectId
                  name = name
              event ProjectRegistered
                name ProjectName
            slice StateView Overview
              readmodel Entry
                name ProjectName
              query AllEntries => observable Entry[]
              projection Entries => Entry
                from ProjectRegistered
                  name = name
        """;

    JsonElement _opened;

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), FullLanguageSource, new System.Text.UTF8Encoding(false));
        Initialize();
    }

    void Because() => _opened = Call("open-workspace").GetProperty("result").GetProperty("structuredContent");

    [Fact] void should_accept_the_model_for_authoring() => _opened.GetProperty("readiness").GetProperty("authoringAccepted").GetBoolean().ShouldBeTrue();
    [Fact] void should_describe_the_model_as_authored() => _opened.GetProperty("readiness").GetProperty("state").GetString().ShouldEqual("authored");
    [Fact] void should_not_claim_executable_readiness() => _opened.GetProperty("readiness").GetProperty("executableReady").GetBoolean().ShouldBeFalse();
    [Fact] void should_report_the_executable_debt_separately() => _opened.GetProperty("readiness").GetProperty("executableDiagnosticCount").GetInt32().ShouldBeGreaterThan(0);
    [Fact] void should_report_no_authoring_diagnostics() => _opened.GetProperty("readiness").GetProperty("authoringDiagnosticCount").GetInt32().ShouldEqual(0);
    [Fact] void should_explain_the_two_verdicts() => _opened.GetProperty("readiness").GetProperty("note").GetString().ShouldNotBeNull();
}

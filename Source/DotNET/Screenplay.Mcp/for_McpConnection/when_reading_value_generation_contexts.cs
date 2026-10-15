// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_reading_value_generation_contexts : given.a_connection
{
    JsonElement _requirement;
    JsonElement _context;
    JsonElement _stale;

    void Because()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), "concept TenantOrderId : String\n  generate rule TenantOrder\n    ```csharp\n    return value;\n    ```");
        Initialize();
        var opened = Content("open-workspace", new { applicationName = "Projects" });
        var revision = opened.GetProperty("revision").GetString();
        var requirements = Content("read-workspace", new { expectedRevision = revision, view = "implementation-requirements", limit = 1 });
        _requirement = requirements.GetProperty("page").GetProperty("items")[0];
        var contexts = Content("read-workspace", new { expectedRevision = revision, view = "typed-contexts", limit = 1 });
        _context = contexts.GetProperty("page").GetProperty("items")[0];
        _stale = Call("read-workspace", new { expectedRevision = revision, view = "typed-contexts", offset = 1, expectedDescriptorContractRevision = "2" }).GetProperty("result");
    }

    [Fact] void should_report_the_value_generation_role() => _requirement.GetProperty("role").GetString().ShouldEqual("ValueGeneration");
    [Fact] void should_publish_the_provider_defined_capability() => _requirement.GetProperty("requiredCapability").GetString().ShouldEqual("provider-defined");
    [Fact] void should_publish_the_body_hash() => _requirement.GetProperty("contentHash").GetString()!.Length.ShouldEqual(64);
    [Fact] void should_publish_context_and_result_version_one()
    {
        _requirement.GetProperty("contextVersion").GetUInt32().ShouldEqual(1u);
        _requirement.GetProperty("resultVersion").GetUInt32().ShouldEqual(1u);
    }
    [Fact] void should_join_the_same_compilation_context() => _context.GetProperty("requirementId").GetString().ShouldEqual(_requirement.GetProperty("requirementId").GetString());
    [Fact] void should_publish_only_the_closed_now_and_tenant_members() => _context.GetProperty("members").EnumerateArray().Select(member => member.GetProperty("name").GetString()).ShouldEqual(["Now", "Tenant"]);
    [Fact] void should_be_wrapper_ready_with_model_provenance()
    {
        _context.GetProperty("isWrapperReady").GetBoolean().ShouldBeTrue();
        _context.GetProperty("modelRevision").GetString().ShouldNotBeEmpty();
    }
    [Fact] void should_reject_a_stale_context_contract_pin() => _stale.GetProperty("isError").GetBoolean().ShouldBeTrue();

    JsonElement Content(string tool, object arguments) => Call(tool, arguments).GetProperty("result").GetProperty("structuredContent");
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_paging_typed_contexts : given.a_connection
{
    JsonElement _first;
    JsonElement _stale;

    void Because()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), Source.Replace("        produces ProjectRegistered\n          for projectId\n          projectId = projectId\n          name = name", "        handler\n          file Handlers/RegisterProject.cs\n        validate csharp\n          ```\n          return true;\n          ```", StringComparison.Ordinal));
        Initialize();
        var opened = Call("open-workspace", new { applicationName = "Projects" }).GetProperty("result").GetProperty("structuredContent");
        var revision = opened.GetProperty("revision").GetString();
        _first = Call("read-workspace", new { expectedRevision = revision, view = "typed-contexts", limit = 1 }).GetProperty("result").GetProperty("structuredContent");
        _stale = Call("read-workspace", new { expectedRevision = revision, view = "typed-contexts", offset = 1, expectedDescriptorContractRevision = "2" }).GetProperty("result");
    }

    [Fact] void should_expose_handler_on_failed_compilation() => _first.GetProperty("page").GetProperty("items")[0].GetProperty("role").GetString().ShouldEqual("CommandHandler");
    [Fact] void should_expose_the_command_properties() => _first.GetProperty("page").GetProperty("items")[0].GetProperty("members")[0].GetProperty("type").GetProperty("properties").GetArrayLength().ShouldEqual(2);
    [Fact] void should_expose_portable_property_type_and_source_identity()
    {
        var item = _first.GetProperty("page").GetProperty("items")[0];
        var property = item.GetProperty("members")[0].GetProperty("type").GetProperty("properties")[0];
        property.GetProperty("type").GetProperty("kind").GetString().ShouldEqual("Concept");
        property.GetProperty("type").GetProperty("target").GetString().ShouldNotBeEmpty();
        item.GetProperty("members")[0].GetProperty("source").GetProperty("semanticId").GetString().ShouldNotBeEmpty();
        item.GetProperty("modelRevision").ValueKind.ShouldEqual(JsonValueKind.Null);
    }
    [Fact] void should_report_partial_handlers_as_unavailable() => _first.GetProperty("available").GetBoolean().ShouldBeFalse();
    [Fact] void should_report_the_handler_as_not_wrapper_ready() => _first.GetProperty("page").GetProperty("items")[0].GetProperty("isWrapperReady").GetBoolean().ShouldBeFalse();
    [Fact] void should_publish_the_referenced_concept_definition() => _first.GetProperty("page").GetProperty("items")[0].GetProperty("types")[0].GetProperty("kind").GetString().ShouldEqual("Concept");
    [Fact] void should_publish_only_handler_descriptors() => _first.GetProperty("page").GetProperty("totalCount").GetInt32().ShouldEqual(1);
    [Fact] void should_pin_its_own_contract_revision() => _first.GetProperty("descriptorContractRevision").GetUInt32().ShouldEqual(1u);
    [Fact] void should_refuse_a_stale_contract_revision() => _stale.GetProperty("isError").GetBoolean().ShouldBeTrue();
}

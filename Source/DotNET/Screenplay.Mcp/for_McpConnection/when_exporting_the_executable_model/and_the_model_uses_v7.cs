// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection.when_exporting_the_executable_model;

public class and_the_model_uses_v7 : given.an_export
{
    JsonElement _first;
    JsonElement _model;
    bool _aligned;
    bool _roundTrips;
    string _staleModel = null!;

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), "concept ProjectId : Uuid\nconcept ReceiptId : Uuid\nmodule Projects\n  feature Registration\n    slice StateChange RegisterProject\n      command RegisterProject\n        projectId ProjectId generated identifier\n        receiptId ReceiptId generated\n        name String\n        produces event ProjectRegistered\n          name String = name\n        returns\n          project = projectId\n          receipt = receiptId\n      specification RegistersProject\n        when RegisterProject\n          for \"11111111-1111-1111-1111-111111111111\"\n          generated receiptId = \"22222222-2222-2222-2222-222222222222\"\n          name = \"Screenplay\"\n        then ProjectRegistered\n          for \"11111111-1111-1111-1111-111111111111\"\n          name = \"Screenplay\"\n        then returns\n          receipt = \"22222222-2222-2222-2222-222222222222\"\n");
        Initialize();
    }

    void Because()
    {
        byte[] bytes;
        (_first, bytes, _aligned) = ExportPages();
        _roundTrips = StrictRoundTrip(bytes);
        using var document = JsonDocument.Parse(bytes);
        _model = document.RootElement.Clone();
        var revision = Content("open-workspace", new { applicationName = "Projects" }).GetProperty("revision").GetString();
        _staleModel = Refusal(new
        {
            expectedRevision = revision,
            view = "executable-model",
            offset = 17,
            limit = 17,
            expectedModelRevision = "stale",
            expectedAttachmentManifestRevision = _first.GetProperty("attachmentManifestRevision").GetString()
        });
    }

    [Fact] void should_label_the_export_as_v7() => _first.GetProperty("schemaVersion").GetInt32().ShouldEqual(7);
    [Fact] void should_reconstruct_exact_canonical_bytes_across_revision_pinned_pages() => _roundTrips.ShouldBeTrue();
    [Fact] void should_align_all_page_offsets_and_byte_counts() => _aligned.ShouldBeTrue();
    [Fact] void should_export_generated_command_properties() => Command().GetProperty("properties").EnumerateArray().Count(property => property.TryGetProperty("generated", out var generated) && generated.GetBoolean()).ShouldEqual(2);
    [Fact] void should_export_response_fields_in_authored_order() => Command().GetProperty("response").GetProperty("fields").EnumerateArray().Select(field => field.GetProperty("name").GetString()).SequenceEqual(["project", "receipt"]).ShouldBeTrue();
    [Fact] void should_export_generation_fixtures_separately_from_inputs() => Specification().GetProperty("when").GetProperty("generatedValues").GetArrayLength().ShouldEqual(2);
    [Fact] void should_not_export_a_generated_identifier_as_a_legacy_destination() => Specification().GetProperty("when").TryGetProperty("eventSource", out _).ShouldBeFalse();
    [Fact] void should_export_the_record_return_subset() => Specification().GetProperty("thenReturns").GetProperty("fields")[0].GetProperty("name").GetString().ShouldEqual("receipt");
    [Fact] void should_reject_a_stale_model_revision_without_a_page() => _staleModel.ShouldContain("StaleRevision");

    JsonElement Command() => _model.GetProperty("application").GetProperty("modules")[0].GetProperty("features")[0].GetProperty("slices")[0].GetProperty("commands")[0];

    JsonElement Specification() => _model.GetProperty("application").GetProperty("modules")[0].GetProperty("features")[0].GetProperty("slices")[0].GetProperty("specifications")[0];
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Api;
using Cratis.Screenplay.Ui;

namespace Cratis.Screenplay.Mcp.for_McpConnection.when_reading_portable_contracts;

public class and_reading_every_page : when_exporting_the_executable_model.given.an_export
{
    byte[] _api;
    byte[] _ui;
    JsonElement _apiFirst;
    JsonElement _uiFirst;

    void Establish() => Initialize();

    void Because()
    {
        var revision = Content("open-workspace", new { applicationName = "Projects" }).GetProperty("revision").GetString()!;
        (_apiFirst, _api) = Read(revision, "api-description");
        (_uiFirst, _ui) = Read(revision, "ui-contract");
    }

    (JsonElement First, byte[] Bytes) Read(string revision, string view)
    {
        var first = Content("read-workspace", new { expectedRevision = revision, view, limit = 64 });
        var modelRevision = first.GetProperty("modelRevision").GetString();
        using var bytes = new MemoryStream();
        var page = first.GetProperty("page");
        while (true)
        {
            bytes.Write(page.GetProperty("bytesBase64").GetBytesFromBase64());
            if (page.GetProperty("nextOffset").ValueKind == JsonValueKind.Null) break;
            page = Content("read-workspace", new { expectedRevision = revision, view, offset = page.GetProperty("nextOffset").GetInt32(), limit = 64, expectedModelRevision = modelRevision })
                .GetProperty("page");
        }

        return (first, bytes.ToArray());
    }

    [Fact] void should_page_the_library_openapi_document() => _api.ShouldEqual(OpenApiDocument.Write(ApiDescription.For(Workspace().Compilation.Value!.Model)));
    [Fact] void should_name_the_openapi_format() => _apiFirst.GetProperty("format").GetString().ShouldEqual("openapi-3.1.0");
    [Fact] void should_page_the_library_ui_contract() => _ui.ShouldEqual(UiContractJson.Write(UiContractBuilder.For(Workspace())));
    [Fact] void should_name_the_ui_contract_version() => _uiFirst.GetProperty("format").GetString().ShouldEqual(UiContract.Version);
    [Fact] void should_count_the_bytes() => _apiFirst.GetProperty("totalBytes").GetInt32().ShouldEqual(_api.Length);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection.when_reading_portable_contracts;

public class and_the_model_has_no_executable_model : when_exporting_the_executable_model.given.an_export
{
    JsonElement _api;
    JsonElement _ui;

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), "module Projects\n  feature Registration\n    slice StateView List\n      projection Broken => Missing\n        from Nothing\n", new System.Text.UTF8Encoding(false));
        Initialize();
    }

    void Because()
    {
        var revision = Content("open-workspace", new { applicationName = "Projects" }).GetProperty("revision").GetString();
        _api = Content("read-workspace", new { expectedRevision = revision, view = "api-description" });
        _ui = Content("read-workspace", new { expectedRevision = revision, view = "ui-contract" });
    }

    [Fact] void should_say_the_api_description_is_unavailable() => _api.GetProperty("available").GetBoolean().ShouldBeFalse();
    [Fact] void should_still_page_the_ui_contract() => _ui.GetProperty("available").GetBoolean().ShouldBeTrue();
    [Fact] void should_carry_no_model_revision_in_the_ui_contract() => _ui.GetProperty("modelRevision").ValueKind.ShouldEqual(JsonValueKind.Null);
}

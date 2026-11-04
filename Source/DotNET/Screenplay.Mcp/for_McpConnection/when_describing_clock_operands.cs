// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_describing_clock_operands : given.a_connection
{
    string _description;

    void Establish() => Initialize();
    void Because() => _description = JsonSerializer.SerializeToElement(McpToolCatalog.Describe(), McpJson.Options).EnumerateArray().Single(tool => tool.GetProperty("name").GetString() == "syntax-schema").GetProperty("description").GetString()!;

    [Fact] void should_name_the_admitted_version() => _description.ShouldContain("ESM v37");
    [Fact] void should_limit_today_offsets_to_days() => _description.ShouldContain("integral day offsets only");
    [Fact] void should_explain_instant_offsets() => _description.ShouldContain("integral day/hour/minute offsets");
    [Fact] void should_convert_after_applying_the_duration() => _description.ShouldContain("duration before calendar conversion");
    [Fact] void should_admit_builtin_and_declared_context_calendars() => _description.ShouldContain("$context.timeZone or a member declared in the context block; other context paths remain refused");
    [Fact] void should_document_zone_precedence() => _description.ShouldContain("command calendar, scenario zone, execution-context zone, application default");
    [Fact] void should_require_deterministic_fixtures() => _description.ShouldContain("given clock (an explicit UTC instant) plus given time zone");
    [Fact] void should_preserve_refused_contexts() => _description.ShouldContain("Concept rules, reaction where, read paths and arbitrary context paths remain refused");
    [Fact] void should_not_claim_today_is_refused() => _description.Contains("context and today remain refused", StringComparison.Ordinal).ShouldBeFalse();
}

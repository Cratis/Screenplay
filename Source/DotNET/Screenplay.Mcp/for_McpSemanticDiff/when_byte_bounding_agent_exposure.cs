// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpSemanticDiff;

public class when_byte_bounding_agent_exposure : given.an_agent_exposure_comparison
{
    readonly string _description = new('ø', 20_000);
    JsonElement _first;
    JsonElement _second;

    void Because()
    {
        CompareSnapshots(Source, Source + when_comparing_agent_exposure.Agents.Replace("List projects", _description, StringComparison.Ordinal).Replace("Register projects", _description, StringComparison.Ordinal));
        _first = ReadExposure().GetProperty("page");
        _second = ReadExposure(new { offset = 1, expectedSourceRevision = Proposal.Workspace.Revision.ToString() }).GetProperty("page");
    }

    [Fact] void should_stop_before_the_second_large_item() => _first.GetProperty("items").GetArrayLength().ShouldEqual(1);
    [Fact] void should_continue_at_the_second_item() => _first.GetProperty("nextOffset").GetInt32().ShouldEqual(1);
    [Fact] void should_keep_the_first_unicode_description_intact() => _first.GetProperty("items")[0].GetProperty("after").GetProperty("description").GetString().ShouldEqual(_description);
    [Fact] void should_obey_the_byte_budget() => Encoding.UTF8.GetByteCount(_first.GetProperty("items").GetRawText()).ShouldBeLessThan(192 * 1024);
    [Fact] void should_keep_the_second_unicode_description_intact() => _second.GetProperty("items")[0].GetProperty("after").GetProperty("description").GetString().ShouldEqual(_description);
}

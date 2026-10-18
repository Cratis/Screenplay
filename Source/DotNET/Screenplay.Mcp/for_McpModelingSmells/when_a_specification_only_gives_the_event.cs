// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpModelingSmells;

public class when_a_specification_only_gives_the_event : given.a_model
{
    const string Approval = "      event RevisionApproved\n        approved Bool";
    const string Given = "      event RevisionApproved\n        approved Bool\n      specification RegisteringAfterAnApproval\n        given RevisionApproved\n          for \"0b9b4a52-9a1d-4b5e-8e2a-5d6f0a1c2b3d\"\n          approved = true\n        when RegisterProject\n          projectId = \"1c9b4a52-9a1d-4b5e-8e2a-5d6f0a1c2b3d\"\n          name = \"Alpha\"\n        then ProjectRegistered\n          for \"1c9b4a52-9a1d-4b5e-8e2a-5d6f0a1c2b3d\"\n          name = \"Alpha\"";
    const string Append = "      event RevisionApproved\n        approved Bool\n      specification AppendingAnApproval\n        when append RevisionApproved\n          for \"0b9b4a52-9a1d-4b5e-8e2a-5d6f0a1c2b3d\"\n          approved = true";

    JsonElement[] _given = [];
    JsonElement[] _append = [];

    void Because()
    {
        _given = Unread(Model.Replace(Approval, Given, StringComparison.Ordinal));
        _append = Unread(Model.Replace(Approval, Append, StringComparison.Ordinal));
    }

    [Fact] void should_not_treat_a_given_fixture_as_a_read() => _given.Single().GetProperty("property").GetString().ShouldEqual("approved");
    [Fact] void should_not_treat_a_when_append_fixture_as_a_read() => _append.Single().GetProperty("property").GetString().ShouldEqual("approved");

    JsonElement[] Unread(string source)
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), source);

        return [.. Call("find-modeling-smells").GetProperty("result").GetProperty("structuredContent").GetProperty("page").GetProperty("items").EnumerateArray().Where(item => item.GetProperty("ruleId").GetString() == "SMELL006")];
    }
}

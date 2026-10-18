// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpModelingSmells;

public class when_asking_about_unread_event_properties : given.a_model
{
    const string ProjectList = "      readmodel ProjectList\n        name ProjectName\n      projection ProjectList\n        from ProjectRegistered\n        from ProjectUpdated";
    const string Approval = "      event RevisionApproved\n        approved Bool";

    JsonElement _before;
    JsonElement _after;
    JsonElement[] _unread = [];
    JsonElement[] _autoMap = [];
    JsonElement[] _explicit = [];
    JsonElement[] _reaction = [];
    JsonElement[] _specification = [];

    void Because()
    {
        _before = Call("diagnostics").GetProperty("result").GetProperty("structuredContent");
        _unread = Unread(Call("find-modeling-smells").GetProperty("result").GetProperty("structuredContent"));
        _after = Call("diagnostics").GetProperty("result").GetProperty("structuredContent");
        _autoMap = Unread(Report(Model.Replace(ProjectList, "      readmodel ProjectList\n        name ProjectName\n        approved Bool\n      projection ProjectList\n        from ProjectRegistered\n        from ProjectUpdated\n        from RevisionApproved", StringComparison.Ordinal)));
        _explicit = Unread(Report(Model.Replace(ProjectList, "      readmodel ProjectList\n        name ProjectName\n        accepted Bool\n      projection ProjectList\n        from ProjectRegistered\n        from ProjectUpdated\n        from RevisionApproved\n          accepted = approved", StringComparison.Ordinal)));
        _reaction = Unread(Report(Model + "\n    slice Automation Follow\n      reaction FollowApproval\n        when RevisionApproved\n        where approved == true"));
        _specification = Unread(Report(Model.Replace(Approval, Approval + "\n      specification ApprovesTheRevision\n        when UpdateProject\n          projectId = \"0b9b4a52-9a1d-4b5e-8e2a-5d6f0a1c2b3d\"\n          name = \"Renamed\"\n        then RevisionApproved\n          approved = true", StringComparison.Ordinal)));
    }

    [Fact] void should_flag_the_unread_property() => _unread.Single().GetProperty("property").GetString().ShouldEqual("approved");
    [Fact] void should_name_the_event_as_the_owner() => _unread.Single().GetProperty("declaration").GetProperty("name").GetString().ShouldEqual("RevisionApproved");
    [Fact] void should_ask_a_question() => _unread.Single().GetProperty("question").GetString()!.EndsWith('?').ShouldBeTrue();
    [Fact] void should_report_information_only() => _unread.Single().GetProperty("severity").GetString().ShouldEqual("info");
    [Fact] void should_preserve_the_compile_verdict() => _after.GetRawText().ShouldEqual(_before.GetRawText());
    [Fact] void should_treat_an_automap_read_as_a_read() => _autoMap.ShouldBeEmpty();
    [Fact] void should_treat_an_explicit_mapping_as_a_read() => _explicit.ShouldBeEmpty();
    [Fact] void should_treat_a_reaction_condition_as_a_read() => _reaction.ShouldBeEmpty();
    [Fact] void should_treat_a_specification_assertion_as_a_read() => _specification.ShouldBeEmpty();

    JsonElement Report(string source)
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), source);

        return Call("find-modeling-smells").GetProperty("result").GetProperty("structuredContent");
    }

    static JsonElement[] Unread(JsonElement report) => [.. report.GetProperty("page").GetProperty("items").EnumerateArray().Where(item => item.GetProperty("ruleId").GetString() == "SMELL006")];
}

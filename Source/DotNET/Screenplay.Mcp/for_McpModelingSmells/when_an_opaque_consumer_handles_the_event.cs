// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpModelingSmells;

public class when_an_opaque_consumer_handles_the_event : given.a_model
{
    const string Summary = "\n    slice StateView Summary\n      readmodel ApprovalSummary\n        count Int\n      reducer ApprovalAudit => ApprovalSummary\n        on RevisionApproved\n";
    const string Reaction = "\n    slice Automation Follow\n      reaction FollowApproval\n        when RevisionApproved\n";

    JsonElement[] _control = [];
    JsonElement[] _fileReducer = [];
    JsonElement[] _inlineReducer = [];
    JsonElement[] _fileReaction = [];
    JsonElement[] _inlineReaction = [];

    void Because()
    {
        _control = Unread(Model + Summary + "          description \"Counts approvals\"");
        _fileReducer = Unread(Model + Summary + "          file Reducers/ApprovalAudit.cs");
        _inlineReducer = Unread(Model + Summary + "          ```csharp\n          return context.State;\n          ```");
        _fileReaction = Unread(Model + Reaction + "          file Reactions/FollowApproval.cs");
        _inlineReaction = Unread(Model + Reaction + "          ```csharp\n          return;\n          ```");
    }

    [Fact] void should_still_flag_a_reducer_rule_without_a_body() => _control.Single().GetProperty("property").GetString().ShouldEqual("approved");
    [Fact] void should_treat_a_reducer_file_handler_as_a_read() => _fileReducer.ShouldBeEmpty();
    [Fact] void should_treat_a_reducer_inline_handler_as_a_read() => _inlineReducer.ShouldBeEmpty();
    [Fact] void should_treat_a_reaction_file_handler_as_a_read() => _fileReaction.ShouldBeEmpty();
    [Fact] void should_treat_a_reaction_inline_code_as_a_read() => _inlineReaction.ShouldBeEmpty();

    JsonElement[] Unread(string source)
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), source);

        return [.. Call("find-modeling-smells").GetProperty("result").GetProperty("structuredContent").GetProperty("page").GetProperty("items").EnumerateArray().Where(item => item.GetProperty("ruleId").GetString() == "SMELL006")];
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_repairing_sibling_authorize_directives : given.an_authoring_connection
{
    const string Head = "policy Staff\n  require authenticated\npolicy Managers\n  require authenticated\npolicy Auditors\n  require authenticated\nmodule M\n  feature F\n    slice StateChange S\n      command C\n        id Uuid identifier\n";
    const string Directives = "        authorize Staff Managers\n        authorize Auditors Staff\n";
    JsonElement _opened;
    JsonElement[] _repairs;

    void Establish()
    {
        File.WriteAllBytes(Path.Combine(RootPath, "application.play"), Encoding.UTF8.GetBytes(Head + Directives));
        Initialize();
        _opened = Open();
        _repairs = [.. Page("repairs", _opened.GetProperty("revision").GetString()).EnumerateArray()
            .Where(repair => repair.GetProperty("diagnosticCode").GetString() == "PLAY0825")];
    }

    [Fact]
    void should_offer_one_repair_per_directive_on_one_subject()
    {
        _repairs.Length.ShouldEqual(2);
        _repairs.Select(repair => repair.GetProperty("location").GetProperty("line").GetInt32()).ShouldContainOnly(12, 13);
        _repairs.Select(repair => repair.GetProperty("subject").GetRawText()).Distinct().Count().ShouldEqual(1);
    }

    [Fact]
    void should_repair_the_first_directive_by_its_line() =>
        Propose(12).ShouldEqual(Head + "        authorize Staff and Managers\n        authorize Auditors Staff\n");

    [Fact]
    void should_repair_the_second_directive_by_its_line() =>
        Propose(13).ShouldEqual(Head + "        authorize Staff Managers\n        authorize Auditors and Staff\n");

    [Fact]
    void should_refuse_an_ambiguous_subject_without_a_line() =>
        (Catch.Exception(() => Propose(null)) as McpFailure).Message.ShouldContain("UnknownRepair");

    [Fact]
    void should_refuse_a_line_that_carries_no_directive() =>
        (Catch.Exception(() => Propose(11)) as McpFailure).Message.ShouldContain("UnknownRepair");

    string Propose(int? line)
    {
        var arguments = new Dictionary<string, object?>
        {
            ["expectedRevision"] = _opened.GetProperty("revision").GetString(),
            ["expectedCatalogRevision"] = _opened.GetProperty("catalogRevision").GetString(),
            ["diagnosticCode"] = "PLAY0825",
            ["subject"] = _repairs[0].GetProperty("subject"),
            ["formatting"] = "PreserveTrivia"
        };
        if (line is not null)
        {
            arguments["line"] = line.Value;
        }

        return Candidate(Result("propose-repair", arguments)).Documents.Single().Text;
    }
}

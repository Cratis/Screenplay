// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json.Nodes;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Workspaces.for_ReleaseBaseline;

public class when_reading_and_admitting : given.a_baseline_workspace
{
    [Fact] void should_distinguish_absence() => Absent.Revision.ShouldEqual("brev1:absent");
    [Fact] void should_have_a_valid_canonical_read() => Read(Baseline).Status.ShouldEqual("valid");
    [Fact] void should_round_trip_canonical_bytes() => ReleaseBaselineStore.Serialize(Read(Baseline).Document!).ShouldEqual(ReleaseBaselineStore.Serialize(Baseline));

    [Fact]
    void should_sort_one_entry_per_lf_line()
    {
        var bytes = ReleaseBaselineStore.Serialize(Baseline with { Entries = [.. Baseline.Entries.Reverse()] });
        bytes.ShouldEqual(ReleaseBaselineStore.Serialize(Baseline));
        var text = Encoding.UTF8.GetString(bytes);
        text.EndsWith('\n').ShouldBeTrue();
        text.Contains('\r').ShouldBeFalse();
        text.Split('\n').Count(line => line.StartsWith("{\"event\":", StringComparison.Ordinal)).ShouldEqual(2);
        Read(Baseline).Revision.ShouldEqual(ReleaseBaselineStore.Revision(bytes));
    }

    [Theory]
    [InlineData("conflict")]
    [InlineData("schema")]
    [InlineData("application")]
    [InlineData("hash")]
    [InlineData("duplicate")]
    [InlineData("missing")]
    [InlineData("optional")]
    [InlineData("criminal")]
    [InlineData("unknown")]
    void should_never_treat_invalid_bytes_as_absent(string defect)
    {
        var bytes = ReleaseBaselineStore.Serialize(Baseline);
        var json = JsonNode.Parse(bytes)!;
        switch (defect)
        {
            case "conflict": bytes = Encoding.UTF8.GetBytes("<<<<<<< main\n=======\n>>>>>>> branch"); break;
            case "schema": json["schema"] = "other"; break;
            case "application": json["applicationIdentity"] = ApplicationIdentity.Create("Other").ToString(); break;
            case "hash": json["entries"]![0]!["schemaHash"] = "rsh1:wrong"; break;
            case "duplicate": json["entries"]!.AsArray().Add(json["entries"]![0]!.DeepClone()); break;
            case "missing": json.AsObject().Remove("entries"); break;
            case "optional": json["entries"]![0]!["schema"]!["properties"]![0]!.AsObject().Remove("optional"); break;
            case "criminal": json["entries"]![0]!["schema"]!["properties"]![0]!["type"]!["properties"]![1]!["type"]!["compliance"]!["personal"]!.AsObject().Remove("criminal"); break;
            case "unknown": json["unknown"] = true; break;
        }

        if (defect != "conflict") bytes = Encoding.UTF8.GetBytes(json.ToJsonString());
        var read = ReleaseBaselineStore.ReadBytes(bytes, Workspace.IdentityCatalog.Application);
        read.Status.ShouldEqual("unreadable");
        read.Revision.ShouldBeNull();
        ReleaseBaseline.Check(Workspace, read).Failure.ShouldEqual("UnreadableReleaseBaseline");
        ReleaseBaseline.Admit(Workspace, read, "v2", At).Failure.ShouldEqual("UnreadableReleaseBaseline");
    }

    [Theory]
    [InlineData("personal")]
    [InlineData("secret")]
    [InlineData("both")]
    void should_round_trip_optional_compliance_markers(string markers)
    {
        var compliance = new SemanticConceptCompliance(markers == "secret" ? null : new(), markers == "personal" ? null : new());
        var schema = new ReleaseSchema("Recorded", [new("value", false, false, new("primitive", "Text", compliance))]);
        var entry = Baseline.Entries[0] with { Schema = schema, SchemaHash = ReleaseSchemaCanonical.Hash(schema) };
        var document = Baseline with { Entries = [entry] };
        var read = Read(document);
        Assert.Equal("valid", read.Status);
        read.Document!.Entries[0].Schema.Properties[0].Type.Compliance.ShouldEqual(compliance);
        ReleaseBaselineStore.Serialize(read.Document).ShouldEqual(ReleaseBaselineStore.Serialize(document));
    }

    [Fact]
    void should_keep_original_admission_labels()
    {
        var result = ReleaseBaseline.Admit(Workspace, Read(Baseline), "v2", At.AddDays(1));
        result.Success.ShouldBeTrue();
        ReleaseBaselineStore.Serialize(result.Document!).ShouldEqual(ReleaseBaselineStore.Serialize(Baseline));
    }

    [Fact]
    void should_add_only_unadmitted_revisions()
    {
        var partial = Baseline with { Entries = [Baseline.Entries[0]] };
        var result = ReleaseBaseline.Admit(Workspace, Read(partial), "v2", At.AddDays(1));
        Assert.True(result.Success, result.Reason);
        result.Document!.Entries[0].AdmittedIn.ShouldEqual("v1.0.0");
        result.Document.Entries[1].AdmittedIn.ShouldEqual("v2");
    }

    [Fact]
    void should_refuse_readmitting_changed_revisions()
    {
        var result = ReleaseBaseline.Admit(Changed(Source.Replace("sent = 4", "sent = 7", StringComparison.Ordinal)), Read(Baseline), "v2", At);
        result.Success.ShouldBeFalse();
        result.Violations.Length.ShouldEqual(2);
    }

    [Fact]
    void should_preserve_missing_entries_during_admission_but_report_them_on_check()
    {
        var extra = Baseline.Entries[0] with { Event = EventContractId.Create(Workspace.IdentityCatalog.Application, "removed").ToString() };
        var previous = Baseline with { Entries = Baseline.Entries.Add(extra) };
        var result = ReleaseBaseline.Admit(Workspace, Read(previous), "v2", At);
        result.Success.ShouldBeTrue();
        result.Document!.Entries.Length.ShouldEqual(3);
        ReleaseBaseline.Check(Workspace, Read(previous)).Violations.Single().Kind.ShouldEqual("BaselinedRevisionMissing");
    }

    [Fact]
    void should_require_a_binding_model()
    {
        var broken = Changed(Source.Replace("note String?", "note Missing", StringComparison.Ordinal));
        ReleaseBaseline.Admit(broken, Absent, "v1", At).Failure.ShouldEqual("ModelDoesNotBind");
    }

    [Fact]
    void should_admit_strictly_read_shipped_bytes()
    {
        var shipped = SemanticModelSerializer.Deserialize(SemanticModelSerializer.Serialize(Workspace.Compilation.Value!.Model));
        ReleaseBaseline.Admit(Workspace, Absent, "shipped", At, shipped).Document!.Entries.Select(entry => entry.SchemaHash).ShouldEqual(Baseline.Entries.Select(entry => entry.SchemaHash));
    }

    [Fact]
    void should_refuse_shipped_names_that_do_not_match_a_pin()
    {
        var shipped = Workspace.Compilation.Value!.Model;
        var source = Source.Replace("event Recorded generation 1", "event Recorded generation 1\n        id \"OldRecorded\"", StringComparison.Ordinal).Replace("event Recorded generation 2", "event Recorded generation 2\n        id \"OldRecorded\"", StringComparison.Ordinal);
        var result = ReleaseBaseline.Admit(Changed(source), Absent, "v1", At, shipped);
        result.Failure.ShouldEqual("ShippedStoredNameMismatch");
        result.Reason!.ShouldContain("OldRecorded");
        result.Reason.ShouldContain("Recorded");
    }
}

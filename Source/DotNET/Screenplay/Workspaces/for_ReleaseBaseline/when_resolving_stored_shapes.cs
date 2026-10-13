// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_ReleaseBaseline;

public class when_resolving_stored_shapes : given.a_baseline_workspace
{
    [Fact] void should_admit_every_historical_and_current_revision() => Baseline.Entries.Select(entry => entry.Revision).ShouldEqual([1U, 2U]);

    [Fact]
    void should_expand_composites_compliance_and_numbered_enums()
    {
        var json = Encoding.UTF8.GetString(ReleaseSchemaCanonical.Serialize(Baseline.Entries[0].Schema));
        json.ShouldEqual("{\"eventType\":\"Recorded\",\"properties\":[{\"name\":\"details\",\"optional\":false,\"collection\":true,\"type\":{\"kind\":\"composite\",\"properties\":[{\"name\":\"status\",\"optional\":false,\"collection\":false,\"type\":{\"kind\":\"enum\",\"members\":[{\"name\":\"draft\",\"number\":1},{\"name\":\"sent\",\"number\":4}]}},{\"name\":\"value\",\"optional\":false,\"collection\":false,\"type\":{\"kind\":\"primitive\",\"primitive\":\"Text\",\"compliance\":{\"personal\":{\"special\":\"health\",\"criminal\":true}}}}]}}]}");
        Baseline.Entries[0].SchemaHash.ShouldEqual("rsh1:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))).ToLowerInvariant());
    }

    [Fact]
    void should_ignore_concept_names_and_behavior_metadata()
    {
        var original = Workspace.Compilation.Value!.Model.Application;
        var renamed = original with { Concepts = [.. original.Concepts.Select(concept => concept with { Name = concept.Name + "Renamed", Validations = [], Generation = new SemanticGeneration("random") })] };
        var properties = original.Modules[0].Features[0].Slices[0].Events[0].Properties;
        ReleaseSchemaCanonical.Hash(ReleaseSchemaCanonical.Resolve(renamed, "Recorded", properties)).ShouldEqual(Baseline.Entries[1].SchemaHash);
    }

    [Theory]
    [InlineData("pii special health", "pii special genetic", 2)]
    [InlineData("sent = 4", "sent = 7", 2)]
    [InlineData("value Protected", "value String", 2)]
    [InlineData("note String?", "note String", 1)]
    void should_report_each_affected_frozen_revision(string before, string after, int affected)
    {
        var result = ReleaseBaseline.Check(Changed(Source.Replace(before, after, StringComparison.Ordinal)), Read(Baseline));
        result.Failure.ShouldBeNull();
        result.Violations.Length.ShouldEqual(affected);
        result.Violations.All(violation => violation.Kind == "BaselinedRevisionChanged" && violation.Path is not null).ShouldBeTrue();
    }

    [Fact]
    void should_ignore_descriptions_and_tags()
    {
        var source = Source.Replace("event Recorded generation 2", "event Recorded generation 2\n        description \"A fact\"\n        tag \"business\"", StringComparison.Ordinal);
        ReleaseBaseline.Check(Changed(source), Read(Baseline)).Success.ShouldBeTrue();
    }

    [Fact]
    void should_detect_stored_name_changes()
    {
        var source = Source.Replace("event Recorded generation 1", "event Recorded generation 1\n        id \"OldRecorded\"", StringComparison.Ordinal).Replace("event Recorded generation 2", "event Recorded generation 2\n        id \"OldRecorded\"", StringComparison.Ordinal);
        var result = ReleaseBaseline.Check(Changed(source), Read(Baseline));
        result.Violations.Length.ShouldEqual(2);
        result.Violations[0].Path.ShouldEqual("$.eventType");
    }
}

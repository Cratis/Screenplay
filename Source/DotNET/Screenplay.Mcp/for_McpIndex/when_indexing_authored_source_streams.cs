// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Mcp.for_McpIndex;

public class when_indexing_authored_source_streams : Specification
{
    const string Source = "concept Month : Int\neventsource Account\n  stream Transactions\n    streamId Month\n";
    const string Command = "module Banking\n  feature Deposits\n    slice StateChange Deposit\n      command Deposit\n        month Month\n        stream Account.Transactions\n          streamId = month\n      specification Routed\n        when Deposit\n";
    McpSnapshot _snapshot = null!;
    McpSyntaxIndex _index = null!;

    void Establish() => _snapshot = new([given.synthetic_model.Document("source", Source), given.synthetic_model.Document("command", Command)]);
    void Because() => _index = _snapshot.Index;

    [Fact] void should_index_source_owned_streams() => _index.Find("Account.Transactions", "EventStream").Length.ShouldEqual(1);
    [Fact] void should_resolve_the_authored_stream() => _index.Resolve(new("Account.Transactions", ["EventStream"], ["Banking", "Deposits", "Deposit"], SourceLocation.Start)).Length.ShouldEqual(1);
    [Fact] void should_not_resolve_an_arbitrary_suffix() => _index.Resolve(new("Transactions", ["EventStream"], [], SourceLocation.Start)).Length.ShouldEqual(0);
    [Fact] void should_not_accept_foreign_qualification() => _index.Resolve(new("Banking.Account.Transactions", ["EventStream"], [], SourceLocation.Start)).Length.ShouldEqual(0);
    [Fact] void should_index_identifier_types_under_the_stream_owner() => _index.References.Any(reference => reference.Name == "Month" && reference.Owner?.Kind == "EventStream").ShouldBeTrue();
    [Fact] void should_index_command_source_and_stream_links() => _index.Outgoing("Banking.Deposits.Deposit.Deposit").Select(reference => reference.Role).ShouldContain("commandStream");
    [Fact] void should_disclose_the_model_as_admitted() => _index.Readiness.ModelExecutionReadiness.ShouldBeNull();
    [Fact] void should_disclose_a_specification_using_the_routed_command_as_admitted() => _index.Readiness.ExecutionReadiness(_index.Find("Banking.Deposits.Deposit.Routed", "Specification")[0].Syntax).ShouldBeNull();

    [Fact]
    void should_index_part_types_under_the_stream_owner_without_part_declarations()
    {
        var snapshot = new McpSnapshot([given.synthetic_model.Document("composite", "concept Period : String\neventsource A\n  stream S\n    streamId\n      period Period\n      key Uuid")]);
        snapshot.Index.References.Any(reference => reference.Name == "Period" && reference.Owner?.Kind == "EventStream").ShouldBeTrue();
        snapshot.Index.Find("period", "EventStreamIdPart").ShouldBeEmpty();
        var part = snapshot.Compilation.Value!.EventSources.Single().Streams.Single().StreamIdParts.First();
        snapshot.Index.Readiness.SyntaxOnly(part).ShouldBeFalse();
    }

    [Fact]
    void should_keep_duplicate_parent_ownership_ambiguous_even_with_one_child()
    {
        var duplicate = new McpSnapshot([given.synthetic_model.Document("source", Source), given.synthetic_model.Document("duplicate", "eventsource Account\n  stream Other\n"), given.synthetic_model.Document("command", Command)]).Index;
        var reference = duplicate.References.Single(reference => reference.Role == "commandStream");
        var edge = new McpReferenceEdge(reference, duplicate.Resolve(reference));
        edge.Targets.Length.ShouldEqual(1);
        edge.Resolution.ShouldEqual("ambiguous");
        duplicate.Find("Account", "EventSource").Length.ShouldEqual(2);
    }
}

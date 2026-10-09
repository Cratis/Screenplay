// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_using_routes_in_examples_and_redelivery
{
    const string Prefix = "eventsource Account\n  identifier String\n  stream Ledger\n    streamId String\nmodule M\n  feature F\n    slice Automation S\n      event Recorded\n        amount Int\n      reaction Observer\n        when Recorded\n          produces Observed\n      event Observed\n      example September : Recorded\n        for \"account\"\n        stream Account.Ledger\n          streamId = \"september\"\n        amount = 1\n";

    [Fact]
    void should_select_the_example_expanded_given_by_route()
    {
        var result = new ScreenplayCompiler().Compile(Prefix + "      specification Recovering\n        given September\n        given September\n          stream Account.Ledger\n            streamId = \"october\"\n        when redelivered Recorded to Observer\n          stream Account.Ledger\n            streamId = \"september\"\n        then no events");
        result.Diagnostics.Where(diagnostic => diagnostic.Code == "PLAY0543" || diagnostic.Code == "PLAY0548" || diagnostic.Code == "PLAY0526").ShouldBeEmpty();
        var effective = SpecificationExamples.Expand(result.Value!);
        var steps = effective.Specifications.Single().Steps;
        ((SpecificationEventSyntax)steps[0].Effective).Stream.ShouldNotBeNull();
        steps[0].Route!.Origin.ShouldEqual(SpecificationValueOrigin.Example);
        steps[1].Route!.Origin.ShouldEqual(SpecificationValueOrigin.Override);
        steps[1].Route!.OverriddenValue.ShouldEqual(steps[1].Example!.Stream);
    }

    [Theory]
    [InlineData("given", true)]
    [InlineData("when append", true)]
    [InlineData("then", false)]
    void should_check_no_stream_on_the_effective_role(string role, bool refused)
    {
        var source = Prefix.Replace("stream Account.Ledger\n          streamId = \"september\"", "no stream");
        var result = new ScreenplayCompiler().Compile(source + $"      specification Role\n        {role} September");
        var diagnostics = result.Diagnostics.Where(diagnostic => diagnostic.Code == "PLAY0547").ToArray();
        diagnostics.Length.ShouldEqual(refused ? 1 : 0);
        if (refused)
        {
            diagnostics[0].Message.ShouldContain("September");
            diagnostics[0].Location.Line.ShouldEqual(19);
        }
    }

    [Fact]
    void should_check_an_invalid_declaration_once_even_when_all_routes_are_replaced()
    {
        var source = Prefix.Replace("Account.Ledger\n          streamId", "Account.Unknown\n          streamId");
        var result = new ScreenplayCompiler().Compile(source + "      specification Replaced\n        given September\n          stream Account.Ledger\n            streamId = \"one\"\n        given September\n          stream Account.Ledger\n            streamId = \"two\"");
        result.Diagnostics.Count(diagnostic => diagnostic.Code == "PLAY0549").ShouldEqual(1);
    }

    [Theory]
    [InlineData("")]
    [InlineData("      specification Inherited\n        given September\n        given September")]
    [InlineData("      specification Overridden\n        given September\n          for \"valid\"\n        given September\n          for \"also-valid\"")]
    void should_validate_the_examples_own_for_once(string steps)
    {
        var result = new ScreenplayCompiler().Compile(Prefix.Replace("for \"account\"", "for 42", StringComparison.Ordinal) + steps);
        var errors = result.Diagnostics.Where(diagnostic => diagnostic.Code == "PLAY0550").ToArray();
        errors.Length.ShouldEqual(1);
        errors[0].Location.Line.ShouldEqual(15);
    }

    [Fact]
    void should_recheck_for_when_a_step_replaces_the_source()
    {
        const string Source = "eventsource Other\n  identifier Uuid\n  stream Ledger\n    streamId String\n" + Prefix;
        var result = new ScreenplayCompiler().Compile(Source + "      specification Moved\n        given September\n          stream Other.Ledger\n            streamId = \"september\"");
        var error = result.Diagnostics.Single(diagnostic => diagnostic.Code == "PLAY0550");
        error.Location.Line.ShouldEqual(24);
        error.Message.ShouldContain("September");
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    void should_let_a_definite_payload_mismatch_rule_out_an_unknown_route(bool samePayload, bool unmatched)
    {
        var result = new ScreenplayCompiler().Compile(Prefix + $"      specification Unknown\n        given September\n        given September\n          amount = {(samePayload ? 1 : 2)}\n          stream Account.Ledger\n            streamId = unknown\n        when redelivered Recorded to Observer\n          amount = 1\n          stream Account.Ledger\n            streamId = \"september\"\n        then no events");
        result.Diagnostics.Any(diagnostic => diagnostic.Code == "PLAY0543").ShouldEqual(unmatched);
    }

    [Fact]
    void should_keep_duplicate_givens_and_the_route_wildcard()
    {
        var result = new ScreenplayCompiler().Compile(Prefix + "      specification Ambiguous\n        given September\n        given September\n          stream Account.Ledger\n            streamId = \"october\"\n        when redelivered Recorded to Observer\n        then no events");
        result.Diagnostics.Single(diagnostic => diagnostic.Code == "PLAY0543").Message.ShouldContain("use 'for', values, 'stream' or 'no stream'");
    }

    [Fact]
    void should_select_only_an_unrouted_given()
    {
        var result = new ScreenplayCompiler().Compile(Prefix + "      specification Unrouted\n        given September\n        given Recorded\n          amount = 1\n        when redelivered Recorded to Observer\n          no stream\n        then no events");
        result.Diagnostics.Any(diagnostic => diagnostic.Code == "PLAY0543" || diagnostic.Code == "PLAY0548").ShouldBeFalse();
    }

    [Fact]
    void should_keep_top_level_stream_id_as_payload()
    {
        var result = new ScreenplayCompiler().Parse("example Fixture : Recorded\n  streamId = \"payload\"");
        result.Diagnostics.ShouldBeEmpty();
        result.Value!.Examples.Single().Values.Single().Property.ShouldEqual("streamId");
    }
}

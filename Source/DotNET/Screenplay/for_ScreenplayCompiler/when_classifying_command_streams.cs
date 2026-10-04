// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_classifying_command_streams : given.a_compiler
{
    const string Prefix = "module M\n  feature F\n    slice StateChange S\n      command C\n        ";
    const string Source = "\neventsource Account\n  identifier AccountId\n  stream Transactions\n    streamId Month\nconcept AccountId : Uuid\nconcept Month : String\n";

    [Theory]
    [InlineData("stream String", "stream")]
    [InlineData("stream Account.Transactions optional", "stream")]
    [InlineData("stream Account.Transactions[]", "stream")]
    [InlineData("stream Account.Transactions generated identifier", "stream")]
    [InlineData("@stream Account.Transactions", "stream")]
    [InlineData("identifier String", "identifier")]
    [InlineData("eventsource String", "eventsource")]
    [InlineData("from String", "from")]
    [InlineData("streamId String", "streamId")]
    void should_keep_property_forms_even_with_a_source(string body, string name)
    {
        var parsed = _compiler.Parse(Prefix + body + Source);
        parsed.Success.ShouldBeTrue();
        var command = Command(parsed.Value!);
        command.Stream.ShouldBeNull();
        command.Properties.Single().Name.ShouldEqual(name);
    }

    [Theory]
    [InlineData("stream Missing.Transactions\n          deeper String")]
    [InlineData("@stream Account.Transactions\n          deeper String")]
    void should_not_steal_deeper_legacy_members(string body)
    {
        var parsed = _compiler.Compile(Prefix + body + Source);
        Command(parsed.Value!).Stream.ShouldBeNull();
        string.Join(',', Command(parsed.Value!).Properties.Select(property => property.Name)).ShouldEqual("stream,deeper");
        parsed.Diagnostics.Any(diagnostic => diagnostic.Code == "PLAY0504").ShouldBeFalse();
    }

    [Fact]
    void should_resolve_forward_source_declarations()
    {
        var parsed = _compiler.Parse(Prefix + "month Month\n        stream Account.Transactions\n          streamId = month" + Source);
        parsed.Success.ShouldBeTrue();
        Command(parsed.Value!).Stream!.StreamId!.Property.ShouldEqual("streamId");
        Command(parsed.Value!).Properties.Single().Name.ShouldEqual("month");
    }

    [Fact]
    void should_retain_both_interpretations_when_a_real_imported_type_also_resolves()
    {
        var parsed = _compiler.Parse("import Account.Transactions\ntype Transactions\n  value String\n" + Prefix + "stream Account.Transactions" + Source);
        parsed.Diagnostics.Any(diagnostic => diagnostic.Code == "PLAY0505").ShouldBeTrue();
        var command = Command(parsed.Value!);
        command.Properties.Single().Type.Name.ShouldEqual("Account.Transactions");
        command.Stream!.PropertyCandidate.ShouldNotBeNull();
    }

    [Fact]
    void should_keep_a_qualified_imported_type_without_a_source()
    {
        var parsed = _compiler.Compile("import Account.Transactions\ntype Transactions\n  value String\n" + Prefix + "stream Account.Transactions\n          deeper String");
        Command(parsed.Value!).Stream.ShouldBeNull();
        Command(parsed.Value!).Properties.Count().ShouldEqual(2);
        parsed.Diagnostics.Any(diagnostic => diagnostic.Code == "PLAY0505").ShouldBeFalse();
    }

    static CommandSyntax Command(ApplicationSyntax application) => application.Modules.Single().Features.Single().Slices.Single().Commands.Single();
}

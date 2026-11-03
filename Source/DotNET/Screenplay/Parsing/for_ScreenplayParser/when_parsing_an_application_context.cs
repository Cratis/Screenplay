// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Parsing.for_ScreenplayParser;

public class when_parsing_an_application_context : Specification
{
    [Fact]
    void should_keep_named_zones_description_and_locations()
    {
        var result = new ScreenplayCompiler().Parse("context\n  description \"Business zones\"\n  businessTimeZone time zone\n  customerTimeZone time   zone\n");
        result.Diagnostics.ShouldBeEmpty();
        var context = result.Value!.Context!;
        context.Description.ShouldEqual("Business zones");
        context.Location.Line.ShouldEqual(1);
        context.Members.Select(member => member.Name).ShouldEqual(["businessTimeZone", "customerTimeZone"]);
        context.Members.First().Location.Line.ShouldEqual(3);
    }

    [Theory]
    [InlineData("context invalid\n", DiagnosticCodes.InvalidContextDeclaration)]
    [InlineData("context\n  invalid\n", DiagnosticCodes.InvalidContextDeclaration)]
    [InlineData("context\n  zone time zone\n    body\n", DiagnosticCodes.InvalidContextDeclaration)]
    [InlineData("context\n  zone String\n", DiagnosticCodes.UnknownContextMemberType)]
    [InlineData("context\n  zone time zone\n  @zone time zone\n", DiagnosticCodes.DuplicateContextMember)]
    [InlineData("context\n  timeZone time zone\n", DiagnosticCodes.ReservedContextMemberName)]
    [InlineData("context\n  identity time zone\n", DiagnosticCodes.ReservedContextMemberName)]
    [InlineData("context\n  @command time zone\n", DiagnosticCodes.ReservedContextMemberName)]
    [InlineData("context\ncontext\n", DiagnosticCodes.InvalidContextDeclaration)]
    public void should_report_invalid_declarations(string source, string code)
    {
        var result = new ScreenplayCompiler().Parse(source);
        result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldEqual([code]);
    }

    [Fact]
    void should_keep_only_the_first_member()
    {
        var context = new ScreenplayCompiler().Parse("context\n  zone time zone\n  @zone time zone\n").Value!.Context!;
        context.Members.Single().Location.Line.ShouldEqual(2);
    }

    [Fact]
    void should_drop_reserved_members() => new ScreenplayCompiler().Parse("context\n  timeZone time zone\n").Value!.Context!.Members.ShouldBeEmpty();

    [Fact]
    void should_accept_an_empty_block()
    {
        var result = new ScreenplayCompiler().Parse("context\n");
        result.Diagnostics.ShouldBeEmpty();
        result.Value!.Context!.Members.ShouldBeEmpty();
    }

    [Fact]
    void should_require_document_placement()
    {
        var result = new ScreenplayCompiler().Parse("context\n  zone time zone\n", "module.play", new PlayPlacement(["Module"]));
        result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(DiagnosticCodes.InvalidContextDeclaration);
        result.Value!.Context.ShouldBeNull();
    }

    [Fact]
    void should_omit_absent_context_from_legacy_json() => SyntaxJson.Serialize(new ScreenplayCompiler().Parse("concept Id : Uuid\n").Value!).TryGetProperty("context", out _).ShouldBeFalse();
}

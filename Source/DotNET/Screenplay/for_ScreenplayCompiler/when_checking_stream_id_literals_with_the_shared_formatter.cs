// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_checking_stream_id_literals_with_the_shared_formatter : given.a_compiler
{
    const string Prefix = "concept Key : Int\nconcept Id : Uuid\neventsource A\n  identifier Id\n  stream S\n    streamId String\nmodule M\n  feature F\n    slice StateChange S\n      command C\n        id Id identifier\n";

    [Theory]
    [InlineData("\"\"")]
    [InlineData("\"e\u0301\"")]
    [InlineData("9007199254740992")]
    [InlineData("-9007199254740992")]
    void should_refuse_command_literals_as_errors(string literal) => Refuses(Route(literal), "PLAY0504");

    [Fact]
    void should_refuse_lone_surrogates_without_echoing_the_value()
    {
        foreach (var literal in new[] { "\"\uD800\"", "\"\uDC00\"" })
        {
            Refuses(Route(literal), "PLAY0504");
            Refuses(SpecificationRoute(literal, "given"), "PLAY0549");
        }
    }

    [Theory]
    [InlineData("given")]
    [InlineData("then")]
    void should_refuse_invalid_specification_literals(string step)
    {
        foreach (var literal in new[] { "\"e\u0301\"", "9007199254740992", "-9007199254740992" }) Refuses(SpecificationRoute(literal, step), "PLAY0549");
    }

    [Theory]
    [InlineData("9007199254740990")]
    [InlineData("9007199254740991")]
    [InlineData("-9007199254740990")]
    [InlineData("-9007199254740991")]
    [InlineData("\"\u00e9\"")]
    [InlineData("\" \"")]
    void should_accept_valid_literals(string literal) => _compiler.Compile(Route(literal)).Success.ShouldBeTrue();

    [Fact]
    void should_leave_exact_mode_integers_unbounded()
    {
        _compiler.Compile("numbers exact\n" + Route("9007199254740993")).Success.ShouldBeTrue();
        _compiler.Compile("numbers exact\n" + SpecificationRoute("9007199254740993", "given")).Success.ShouldBeTrue();
    }

    static string Route(string literal) => Prefix.Replace("streamId String", "streamId " + (literal.StartsWith('"') ? "String" : "Key")) + "        stream A.S\n          streamId = " + literal;
    static string SpecificationRoute(string literal, string step) => Prefix.Replace("streamId String", "streamId " + (literal.StartsWith('"') ? "String" : "Key")) + "      event Recorded\n      specification Example\n        " + step + " Recorded\n          for \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"\n          stream A.S\n            streamId = " + literal;

    void Refuses(string text, string code)
    {
        var result = _compiler.Compile(text);
        result.Success.ShouldBeFalse();
        result.Diagnostics.Any(diagnostic => diagnostic.Code == code && diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeTrue();
    }
}

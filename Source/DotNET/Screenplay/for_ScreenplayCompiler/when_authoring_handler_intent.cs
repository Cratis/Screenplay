// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_authoring_handler_intent : given.a_compiler
{
    const string Prefix = "module M\n  feature F\n    slice StateChange S\n      command C\n        handler\n";

    [Theory]
    [InlineData("          implementation")]
    [InlineData("          implementation\n            hint \" Keep whitespace \"")]
    [InlineData("          file Handler.cs")]
    [InlineData("          implementation\n            hint \"Keep \\\"quotes\\\" and // text\"\n            file Handler.cs")]
    [InlineData("          ```csharp\n          return null;\n          ```")]
    [InlineData("          implementation\n            ```typescript\n            hint implementation file\n            ```")]
    void should_roundtrip_valid_forms(string body)
    {
        var parsed = _compiler.Parse(Prefix + body);
        parsed.Success.ShouldBeTrue();
        var printed = new ScreenplayPrinter().Print(parsed.Value!);
        var reparsed = _compiler.Parse(printed);
        reparsed.Success.ShouldBeTrue();
        SyntaxJson.StructurallyEqual(parsed.Value!, reparsed.Value!).ShouldBeTrue();
        SyntaxJson.StructurallyEqual(parsed.Value!, SyntaxJson.Deserialize(SyntaxJson.Serialize(parsed.Value!))).ShouldBeTrue();
    }

    [Theory]
    [InlineData("          implementation\n            hint unquoted", "PLAY0493")]
    [InlineData("          implementation\n            hint \" \"", "PLAY0493")]
    [InlineData("          implementation\n            hint \"one\" \"two\"", "PLAY0493")]
    [InlineData("          implementation\n            hint \"one\"\n              nested", "PLAY0493")]
    [InlineData("          implementation\n            provider csharp", "PLAY0492")]
    [InlineData("          implementation\n          implementation", "PLAY0492")]
    [InlineData("          implementation\n            implementation", "PLAY0492")]
    [InlineData("          implementation\n            file One.cs\n            file Two.cs", "PLAY0494")]
    [InlineData("          implementation\n            file One.cs\n            ```csharp\n            return null;\n            ```", "PLAY0494")]
    [InlineData("          implementation\n          file Direct.cs", "PLAY0494")]
    [InlineData("          file Direct.cs\n          implementation", "PLAY0494")]
    void should_diagnose_invalid_forms(string body, string code) =>
        _compiler.Parse(Prefix + body).Diagnostics.Any(diagnostic => diagnostic.Code == code).ShouldBeTrue();

    [Fact]
    void should_keep_old_transport_readable() =>
        ((HandlerSyntax)SyntaxJson.Deserialize(JsonDocument.Parse("{\"kind\":\"HandlerSyntax\",\"file\":{\"kind\":\"FileReferenceSyntax\",\"path\":\"C.cs\"},\"code\":null}").RootElement)).Implementation.ShouldBeNull();

    [Fact]
    void should_reject_unknown_metadata_fields() => Catch.Exception(() => SyntaxJson.Deserialize(JsonDocument.Parse("{\"kind\":\"ImplementationSyntax\",\"hints\":[],\"confirmed\":true}").RootElement)).ShouldBeOfExactType<InvalidSyntaxJson>();

    [Fact]
    void should_reject_conflicting_typed_payloads() => Catch.Exception(() => SyntaxJson.Serialize(new HandlerSyntax(new("C.cs", SourceLocation.Start), new("csharp", "return null;", SourceLocation.Start), SourceLocation.Start) { Implementation = new([], SourceLocation.Start) })).ShouldBeOfExactType<InvalidSyntaxJson>();

    [Fact]
    void should_reject_blank_typed_hints() => Catch.Exception(() => SyntaxJson.Serialize(new ImplementationHintSyntax(" \t", SourceLocation.Start))).ShouldBeOfExactType<InvalidSyntaxJson>();

    [Fact]
    void should_preserve_the_published_constructor() => typeof(HandlerSyntax).GetConstructors().Single().GetParameters().Length.ShouldEqual(3);

    [Fact]
    void should_visit_each_edge_once()
    {
        var syntax = _compiler.Parse(Prefix + "          implementation\n            hint \"first\"\n            hint \"second\"\n            file C.cs").Value!;
        var walker = new CountingWalker();
        walker.VisitApplication(syntax);
        walker.Nodes.OfType<HandlerSyntax>().Count().ShouldEqual(1);
        walker.Nodes.OfType<ImplementationSyntax>().Count().ShouldEqual(1);
        walker.Nodes.OfType<ImplementationHintSyntax>().Count().ShouldEqual(2);
        walker.Nodes.OfType<FileReferenceSyntax>().Count().ShouldEqual(1);
    }

    sealed class CountingWalker : ScreenplaySyntaxWalker
    {
        internal List<SyntaxNode> Nodes { get; } = [];
        public override void VisitNode(SyntaxNode node) => Nodes.Add(node);
    }
}

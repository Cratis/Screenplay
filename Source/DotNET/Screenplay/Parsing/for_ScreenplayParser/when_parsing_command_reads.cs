// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_ScreenplayParser;

public class when_parsing_command_reads : Specification
{
    [Theory]
    [InlineData("command reads protected")]
    [InlineData("command  reads  protected // decision consistency")]
    public void should_parse_the_explicit_root_mode(string source)
    {
        var parsed = new ScreenplayCompiler().Parse(source);
        parsed.Success.ShouldBeTrue();
        parsed.Value!.CommandReadMode.ShouldEqual(CommandReadMode.Protected);
    }

    [Theory]
    [InlineData("command reads protected\ncommand reads protected")]
    [InlineData("command reads protected\ncommand reads legacy")]
    [InlineData("command reads unknown")]
    [InlineData("command reads protected extra")]
    [InlineData("command reads")]
    public void should_refuse_duplicate_conflicting_or_incomplete_modes(string source)
    {
        var parsed = new ScreenplayCompiler().Parse(source);
        parsed.Success.ShouldBeFalse();
        parsed.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidCommandReadsDeclaration);
    }

    [Theory]
    [InlineData("M")]
    [InlineData("M", "F")]
    public void should_refuse_a_preamble_in_a_placed_document(params string[] scope)
    {
        var parsed = new ScreenplayCompiler().Parse("command reads protected", "fragment.play", new PlayPlacement(scope));
        parsed.Success.ShouldBeFalse();
        parsed.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidCommandReadsDeclaration);
        parsed.Value!.CommandReadMode.ShouldBeNull();
    }

    [Theory]
    [InlineData("Submit")]
    [InlineData("reads")]
    public void should_not_mistake_an_ordinary_command_for_the_preamble(string name)
    {
        var parsed = new ScreenplayCompiler().Parse($"module M\n  feature F\n    slice StateChange S\n      command {name}\n        value String");
        parsed.Success.ShouldBeTrue();
        parsed.Value!.CommandReadMode.ShouldBeNull();
        parsed.Value.Modules.Single().Features.Single().Slices.Single().Commands.Single().Name.ShouldEqual(name);
    }

    [Fact]
    void should_bind_the_explicit_preamble_without_reads()
    {
        var syntax = new ScreenplayCompiler().Parse("command reads protected").Value!;
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("ProtectedReads"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("single"), "single", "single.play", "command reads protected");
        var result = new SemanticModelBinder().Bind("ProtectedReads", syntax, SemanticDocumentSet.Create([document], catalog));
        result.Success.ShouldBeTrue();
        result.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V25);
    }
}

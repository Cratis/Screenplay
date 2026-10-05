// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Files;
using Cratis.Screenplay.Languages;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax.Captures;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Syntax.for_SourceOptions;

public class when_closing_phase_a_source_contracts
{
    readonly ScreenplayCompiler _compiler = new();

    [Theory]
    [InlineData("seed wrong\n", "PLAY0128")]
    [InlineData("seed\n  wrong\n", "PLAY0129")]
    [InlineData("seed\n  for \"global\"\n    wrong\n", "PLAY0130")]
    [InlineData("seed\n  for \"global\"\n    Added\n      amount =\n", "PLAY0131")]
    [InlineData("seed\n  for \"global\"\n    Added\n      amount\n        numbers exact\n", "PLAY0131")]
    [InlineData("seed\n  for \"global\"\n    Add𐐀\n      amount = 1\n", "PLAY0130")]
    [InlineData("policy P\n  numbers exact\n", "PLAY0113")]
    [InlineData("policy wrong-name\n  require authenticated\n", "PLAY0112")]
    [InlineData("policy P\n", "PLAY0114")]
    [InlineData("policy P\n  require authenticated\n  require authenticated\n", "PLAY0441")]
    [InlineData("policy P\n  require authenticated\n  file p.cs\n", "PLAY0440")]
    [InlineData("policy P\n  require role\n", "PLAY0118")]
    [InlineData("policy P\n  require claim\n", "PLAY0119")]
    [InlineData("policy P\n  require claim \"limit\"\n", "PLAY0120")]
    [InlineData("policy P\n  require claim \"limit\" matches\n", "PLAY0121")]
    [InlineData("policy P\n  file a.cs\n  file b.cs\n", "PLAY0113")]
    void should_pin_exact_seed_and_policy_rejections_in_physical_documents_and_folders(string body, string code)
    {
        var result = _compiler.Parse("numbers exact\n" + body, "input.play");
        result.Success.ShouldBeFalse();
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == code);
        var source = new InMemoryPlayDocumentSource(new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["root.play"] = "import \"child.play\"\n",
            ["child.play"] = "numbers exact\n" + body
        });
        var (_, folder) = PlayApplicationAssembly.Compile(_compiler, ["root.play"], source);
        folder.Success.ShouldBeFalse();
        folder.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == code);
    }

    [Theory]
    [InlineData("projection", "PLAY0055")]
    [InlineData("capture", "PLAY0077")]
    [InlineData("specification", "PLAY0093")]
    void should_require_a_declaration_in_empty_exact_family_documents(string family, string code)
    {
        var diagnostics = family switch
        {
            "projection" => _compiler.CompileProjection("numbers exact\n# comment\n").Diagnostics,
            "capture" => _compiler.CompileCapture("numbers exact\n# comment\n").Diagnostics,
            _ => _compiler.CompileSpecification("numbers exact\n# comment\n").Diagnostics
        };
        diagnostics.ShouldContain(diagnostic => diagnostic.Code == code);
    }

    [Theory]
    [InlineData(false, "screen")]
    [InlineData(true, "screen")]
    [InlineData(false, "handler")]
    [InlineData(true, "handler")]
    void should_consume_custom_legacy_form_code_before_discovering_physical_imports(bool exact, string owner)
    {
        var declaration = owner == "screen" ? "screen S" : "command C\n        handler";
        var indent = owner == "screen" ? "        " : "          ";
        var prefix = exact ? "numbers exact\n" : "";
        var text = $"{prefix}module M\n  feature F\n    slice StateChange S\n      {declaration}\n{indent}custom\n{indent}  ```custom\nimport \"fake.play\"\nnumbers exact\n``` not a closing fence\n{indent}  ```\n";
        var source = new InMemoryPlayDocumentSource(new Dictionary<string, string>(StringComparer.Ordinal) { ["root.play"] = text });
        var (physical, result) = PlayApplicationAssembly.Compile(new ScreenplayCompiler(new ScreenplayLanguageRegistry(["custom"])), ["root.play"], source);
        result.Success.ShouldBeTrue();
        physical.Select(document => document.Path).ShouldEqual(new[] { "root.play" });
    }

    [Theory]
    [InlineData("capture C\n  map\n    value = café𐐀\n", "PLAY0152")]
    [InlineData("capture C\n  map\n    split café by \",\"\n      café𐐀\n", "PLAY0083")]
    [InlineData("capture C\n  map\n    value = café translate\n      \"x\" => café𐐀\n", "PLAY0081")]
    void should_consume_the_full_unsupported_identifier_rather_than_a_valid_prefix(string body, string code)
    {
        var result = _compiler.CompileCapture("numbers exact\n" + body);
        result.Success.ShouldBeFalse();
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == code);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_retain_native_unicode_capture_paths_and_transitions(bool exact)
    {
        var prefix = exact ? "numbers exact\n" : "";
        var result = _compiler.CompileCapture(prefix + "capture C\n  map\n    result = café\n    split café by \",\"\n      café.name\n    translated = café translate\n      \"yes\" => café\n  append Added\n    when café from café to café\n");
        result.Success.ShouldBeTrue();
        var wire = SyntaxJson.Serialize(result.Value!).GetRawText();
        wire.ShouldContain("PathExpressionSyntax");
        wire.ShouldNotContain("RawExpressionSyntax");
        wire.ShouldContain("caf");
        var restored = (CaptureSyntax)SyntaxJson.Deserialize(SyntaxJson.Serialize(result.Value!));
        var reparsed = _compiler.CompileCapture(new ScreenplayPrinter().Print(restored));
        reparsed.Success.ShouldBeTrue();
        SyntaxJson.StructurallyEqual(restored, reparsed.Value!).ShouldBeTrue();
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Files;
using Cratis.Screenplay.Languages;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Syntax.for_SourceOptions;

public class when_addressing_second_phase_a_review
{
    readonly ScreenplayCompiler _compiler = new();

    [Theory]
    [InlineData("from E\n    numbers exact", "PLAY0075")]
    [InlineData("from E\n    bad line", "PLAY0075")]
    [InlineData("every\n    bad line", "PLAY0075")]
    [InlineData("all\n    clear with", "PLAY0075")]
    [InlineData("from E\n    key Composite {\n      bad line\n    }", "PLAY0072")]
    [InlineData("from E\n    key Composite {\n      }", "PLAY0074")]
    [InlineData("from E\n    key Composite {\n      part = `value ${1}`\n    }", "PLAY0073")]
    void should_pin_native_exact_projection_mapping_and_key_diagnostics(string body, string code)
    {
        var result = _compiler.CompileProjection("numbers exact\nprojection P\n  " + body + "\n");
        result.Success.ShouldBeFalse();
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == code);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    void should_use_the_callers_registry_through_real_imports_and_placed_discovery(bool exact, bool placed)
    {
        var compiler = new ScreenplayCompiler(new ScreenplayLanguageRegistry(["custom"]));
        var prefix = exact ? "numbers exact\n" : "";
        var child = prefix + (placed
            ? "slice StateChange S\n  command C\n    handler\n      ```custom\nimport \"fake.play\"\nnumbers exact\n      ```\n"
            : "module M\n  feature F\n    slice StateChange S\n      command C\n        handler\n          ```custom\nimport \"fake.play\"\nnumbers exact\n          ```\n");
        var files = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["root.play"] = placed ? prefix + "module M\n  feature F\n    import \"barrel.play\"\n" : "import \"barrel.play\"\n",
            ["barrel.play"] = "import \"child.play\"\n",
            ["child.play"] = child,
            ["fake.play"] = "numbers legacy\nconcept Fake : Decimal\n"
        };
        var documents = new InMemoryPlayDocumentSource(files);
        var (physical, result) = PlayApplicationAssembly.Compile(compiler, ["root.play"], documents);
        physical.Select(document => document.Path).ShouldEqual((string[])["root.play", "barrel.play", "child.play"]);
        result.Success.ShouldBeTrue();
        result.Value!.Concepts.ShouldBeEmpty();
        result.Value.SourceOptions.ShouldEqual(exact ? SourceOptions.Exact : SourceOptions.Legacy);
        result.Value.Modules.Single().Features.Single().Slices.Single().Commands.Single().Handler!.Code!.Code.ShouldEqual("import \"fake.play\"\nnumbers exact");
    }

    [Theory]
    [InlineData("custom")]
    [InlineData("csharp")]
    [InlineData("typescript")]
    [InlineData("react")]
    [InlineData("html")]
    [InlineData("sql")]
    void should_keep_fenced_imports_and_modes_out_of_root_barrel_discovery(string language)
    {
        var registry = new ScreenplayLanguageRegistry([language]);
        var source = new InMemoryPlayDocumentSource(new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["root.play"] = "import \"barrel.play\"\n",
            ["barrel.play"] = $"numbers exact\npolicy P\n  ```{language}\nimport \"fake.play\"\nnumbers legacy\n  ```\nimport \"child.play\"\n",
            ["child.play"] = "numbers exact\nconcept A : Decimal\n",
            ["fake.play"] = "numbers legacy\nconcept Fake : Decimal\n"
        });
        var (documents, diagnostics) = PlayImports.Resolve(["root.play"], source, registry);
        documents.Select(document => document.Path).ShouldEqual((string[])["root.play", "barrel.play", "child.play"]);
        diagnostics.ShouldBeEmpty();
        var (_, application) = PlayApplicationAssembly.Compile(new ScreenplayCompiler(registry), ["root.play"], source);
        application.Success.ShouldBeTrue();
        application.Value!.SourceOptions.ShouldEqual(SourceOptions.Exact);
        application.Value.Concepts.Single().Name.ShouldEqual("A");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_keep_invalid_native_neutral_barrel_options(bool nullOptions)
    {
        var barrel = _compiler.Parse("import \"child.play\"\n", "barrel.play");
        barrel = barrel with { Value = barrel.Value! with { SourceOptions = nullOptions ? null! : new((NumericMode)(-1)) } };
        var declaration = _compiler.Parse("numbers exact\nconcept A : Decimal\n", "child.play");
        foreach (var documents in new CompilationResult<ApplicationSyntax>[][] { [barrel], [barrel, declaration], [declaration, barrel] })
        {
            var merged = PlayFolderMerge.Merge(documents);
            merged.Success.ShouldBeFalse();
            Catch.Exception(() => SyntaxJson.Serialize(merged.Value!)).ShouldBeOfExactType<InvalidSyntaxJson>();
            Catch.Exception(() => new ScreenplayPrinter().Print(merged.Value!)).ShouldBeOfExactType<InvalidSyntaxJson>();
        }
    }
}

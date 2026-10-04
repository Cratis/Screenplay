// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Files.for_PlayFileWriter;

public class when_preserving_stream_properties_through_syntax_json
{
    const string Declarations = "import Account.Transactions\ntype Transactions\n  value String\neventsource Account\n  stream Transactions\n";
    const string Prefix = "module M\n  feature F\n    slice StateChange S\n      command C\n        ";

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_preserve_an_escaped_property_after_json_folder_expansion_and_assembly(bool reverse)
    {
        var compiler = new ScreenplayCompiler();
        var original = compiler.Compile(Declarations + Prefix + "@stream Account.Transactions\n          deeper String");
        original.Success.ShouldBeTrue();
        var json = SyntaxJson.Serialize(original.Value!);
        json.GetRawText().ShouldNotContain("nameWasEscaped");
        var decoded = (ApplicationSyntax)SyntaxJson.Deserialize(json);
        Command(decoded).Properties.First().NameWasEscaped.ShouldBeFalse();
        SyntaxJson.StructurallyEqual(original.Value!, decoded).ShouldBeTrue();
        var files = new PlayFileWriter().Expand(decoded).ToArray();
        files.Single(file => file.RelativePath == Path.Combine("M", "F", "S", "S.play")).Content.ShouldContain("@stream Account.Transactions");
        var restored = Assemble(files, reverse);
        restored.Success.ShouldBeTrue();
        Command(restored.Value!).Stream.ShouldBeNull();
        SyntaxJson.StructurallyEqual(Command(decoded), Command(restored.Value!)).ShouldBeTrue();
    }

    [Fact]
    void should_preserve_a_programmatic_property_in_a_fragment_without_declarations_or_escape_metadata()
    {
        var compiler = new ScreenplayCompiler();
        var application = compiler.Parse(Declarations + Prefix + "other String").Value!;
        var module = application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        var command = Command(application) with
        {
            Properties = [new PropertySyntax("stream", new TypeRefSyntax("Account.Transactions", false, false, SourceLocation.Start), SourceLocation.Start)]
        };
        var fragment = application with
        {
            EventSources = [], Imports = [], Types = [],
            Modules = [module with { Features = [feature with { Slices = [slice with { Commands = [command] }] }] }]
        };
        var printed = new ScreenplayPrinter().Print(fragment);
        printed.ShouldContain("@stream Account.Transactions");
        var restored = compiler.Compile(Declarations + printed);
        restored.Success.ShouldBeTrue();
        Command(restored.Value!).Stream.ShouldBeNull();
        SyntaxJson.StructurallyEqual(command, Command(restored.Value!)).ShouldBeTrue();
    }

    [Fact]
    void should_keep_an_unknown_qualified_property_when_other_files_later_declare_the_source()
    {
        var compiler = new ScreenplayCompiler();
        var original = compiler.Parse(Prefix + "stream Account.Transactions\n          deeper String").Value!;
        var decoded = (ApplicationSyntax)SyntaxJson.Deserialize(SyntaxJson.Serialize(original));
        var files = new PlayFileWriter().Expand(decoded).Append(new PlayFileContent("sources.play", "eventsource Account\n  stream Transactions")).ToArray();
        var restored = Assemble(files, false);
        restored.Success.ShouldBeTrue();
        Command(restored.Value!).Stream.ShouldBeNull();
        SyntaxJson.StructurallyEqual(Command(decoded), Command(restored.Value!)).ShouldBeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_refuse_ambiguity_export_after_json_in_either_file_order(bool reverse)
    {
        var original = new ScreenplayCompiler().Compile(Declarations + Prefix + "stream Account.Transactions\n          deeper String");
        original.Diagnostics.Any(diagnostic => diagnostic.Code == "PLAY0505").ShouldBeTrue();
        var decoded = (ApplicationSyntax)SyntaxJson.Deserialize(SyntaxJson.Serialize(original.Value!));
        Command(decoded).StreamCandidates.Single().PropertyCandidate.ShouldNotBeNull();
        var error = Catch.Exception(() => _ = new PlayFileWriter().Expand(decoded).ToArray());
        error.ShouldBeOfExactType<InvalidSyntaxJson>();
        SyntaxJson.StructurallyEqual(original.Value!, decoded).ShouldBeTrue();
        Assemble([new("model.play", Declarations + Prefix + "stream Account.Transactions\n          deeper String")], reverse).Success.ShouldBeFalse();
    }

    [Fact]
    void should_retain_ambiguity_for_every_order_of_separately_declared_types_sources_and_commands()
    {
        var documents = new[]
        {
            new PlayFileContent("sources.play", "eventsource Account\n  stream Transactions"),
            new PlayFileContent("types.play", "import Account.Transactions\ntype Transactions\n  value String"),
            new PlayFileContent("commands.play", Prefix + "stream Account.Transactions\n          deeper String")
        };
        foreach (var first in documents)
        {
            foreach (var second in documents.Where(document => document != first))
            {
                var third = documents.Single(document => document != first && document != second);
                var result = Assemble([first, second, third], false);
                result.Success.ShouldBeFalse();
                result.Diagnostics.Any(diagnostic => diagnostic.Code == "PLAY0505").ShouldBeTrue();
                var decoded = (ApplicationSyntax)SyntaxJson.Deserialize(SyntaxJson.Serialize(result.Value!));
                Catch.Exception(() => _ = new PlayFileWriter().Expand(decoded).ToArray()).ShouldBeOfExactType<InvalidSyntaxJson>();
                Command(decoded).StreamCandidates.Single().PropertyCandidate.ShouldNotBeNull();
                Command(decoded).Properties.Single().Name.ShouldEqual("deeper");
            }
        }
    }

    [Theory]
    [InlineData("domain Example\nimport Account.Transactions\n  type Transactions\n    value String")]
    [InlineData("import Account.Transactions\n  type Transactions\n    value String")]
    [InlineData("import Account.Transactions\n\ttype Transactions\n\t  value String")]
    void should_inventory_deeper_declarations_in_every_file_order(string types)
    {
        var documents = new[]
        {
            new PlayFileContent("sources.play", "eventsource Account\n  stream Transactions"),
            new PlayFileContent("types.play", types),
            new PlayFileContent("commands.play", Prefix + "stream Account.Transactions\n          deeper String")
        };
        foreach (var first in documents)
        {
            foreach (var second in documents.Where(document => document != first))
            {
                var result = Assemble([first, second, documents.Single(document => document != first && document != second)], false);
                result.Diagnostics.Any(diagnostic => diagnostic.Code == "PLAY0505").ShouldBeTrue();
                Command(result.Value!).StreamCandidates.Single().PropertyCandidate.ShouldNotBeNull();
                Command(result.Value!).Properties.Single().Name.ShouldEqual("deeper");
            }
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_use_placed_module_and_feature_leaf_rules(bool reverse)
    {
        var result = Assemble(
            [
                new("application.play", "import Account.Transactions\neventsource Account\n  stream Transactions\nmodule M\n  import \"module.play\"\n  feature F\n    import \"feature.play\""),
                new("module.play", "description \"Module\"\n  type Transactions\n    value String"),
                new("feature.play", "description \"Feature\"\n  slice StateChange S\n    command C\n      stream Account.Transactions\n        deeper String")
            ],
            reverse);
        result.Diagnostics.Any(diagnostic => diagnostic.Code == "PLAY0505").ShouldBeTrue();
        Command(result.Value!).StreamCandidates.Single().PropertyCandidate.ShouldNotBeNull();
        Command(result.Value!).Properties.Single().Name.ShouldEqual("deeper");
    }

    [Fact]
    void should_not_escape_an_unqualified_scalar_property()
    {
        var application = new ScreenplayCompiler().Parse(Prefix + "stream String").Value!;
        var printed = new ScreenplayPrinter().Print(application);
        printed.ShouldContain("stream String");
        printed.ShouldNotContain("@stream String");
        SyntaxJson.StructurallyEqual(application, new ScreenplayCompiler().Parse(printed).Value!).ShouldBeTrue();
    }

    static CompilationResult<ApplicationSyntax> Assemble(IEnumerable<PlayFileContent> files, bool reverse)
    {
        var texts = files.ToDictionary(file => file.RelativePath, file => file.Content, StringComparer.Ordinal);
        var roots = reverse ? texts.Keys.Reverse() : texts.Keys;
        return PlayApplicationAssembly.Compile(new ScreenplayCompiler(), roots, new InMemoryPlayDocumentSource(texts)).Result;
    }

    static CommandSyntax Command(ApplicationSyntax application) => application.Modules.Single().Features.Single().Slices.Single().Commands.Single();
}

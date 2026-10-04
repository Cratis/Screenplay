// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Syntax.for_SourceOptions;

public class when_printing_exact_command_routes
{
    [Fact]
    void should_print_directly_restored_and_expanded_with_one_preamble_and_a_fixed_point()
    {
        var compiler = new ScreenplayCompiler();
        const string source = "numbers exact\nconcept Key : Int\neventsource Orders\n  stream Changes\n    streamId Key\nmodule M\n  feature F\n    slice StateChange S\n      command C\n        stream Orders.Changes\n          streamId = 9007199254740993\n";
        var parsed = compiler.Parse(source);
        parsed.Success.ShouldBeTrue();
        var printer = new ScreenplayPrinter();
        foreach (var root in new[] { parsed.Value!, (ApplicationSyntax)SyntaxJson.Deserialize(SyntaxJson.Serialize(parsed.Value!)) })
        {
            var printed = printer.Print(root);
            printed.StartsWith("numbers exact\n", StringComparison.Ordinal).ShouldBeTrue();
            printed.ShouldContain("streamId = 9007199254740993");
            printer.Print(compiler.Parse(printed).Value!).ShouldEqual(printed);
            var files = new PlayFileWriter().Expand(root).ToArray();
            foreach (var file in files)
            {
                file.Content.StartsWith("numbers exact\n", StringComparison.Ordinal).ShouldBeTrue();
                file.Content.Split("numbers exact", StringSplitOptions.None).Length.ShouldEqual(2);
            }
            var sourceFiles = new InMemoryPlayDocumentSource(files.ToDictionary(file => file.RelativePath, file => file.Content, StringComparer.Ordinal));
            var (_, expanded) = PlayApplicationAssembly.Compile(compiler, files.Select(file => file.RelativePath), sourceFiles);
            expanded.Success.ShouldBeTrue();
            printer.Print(expanded.Value!).ShouldEqual(printed);
        }
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Numbers"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("input"), "input", "input.play", source);
        var binding = new SemanticModelBinder().Bind("Numbers", parsed.Value!, SemanticDocumentSet.Create([document], catalog));
        binding.Success.ShouldBeFalse();
        binding.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.UnsupportedSemanticSyntax);
        binding.Value.ShouldBeNull();
    }
}

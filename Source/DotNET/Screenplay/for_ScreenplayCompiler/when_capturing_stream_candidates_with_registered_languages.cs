// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Files;
using Cratis.Screenplay.Languages;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_capturing_stream_candidates_with_registered_languages
{
    const string Prefix = "module M\n  feature F\n    slice StateChange S\n      command C\n        stream Fake.S\n          deeper String\n        handler\n          ";
    const string Code = "\neventsource Fake\n  stream S\nimport Account.Transactions\ntype Transactions\n  value String\n// JSON token: ` not a code fence\n          ```\n";

    [Theory]
    [InlineData("python")]
    [InlineData("csharp")]
    [InlineData("typescript")]
    [InlineData("react")]
    [InlineData("html")]
    [InlineData("sql")]
    void should_use_the_same_registry_for_capture_and_committed_parse(string language)
    {
        var compiler = new ScreenplayCompiler(new ScreenplayLanguageRegistry(["python"]));
        var source = Prefix + "```" + language + Code;
        foreach (var result in new[] { compiler.Compile(source), compiler.Parse(source, "input.play"), compiler.Parse(source, "input.play", PlayPlacement.Document) })
        {
            result.Success.ShouldBeTrue();
            result.Value!.EventSources.ShouldBeEmpty();
            result.Value.Types!.ShouldBeEmpty();
            var command = Command(result.Value);
            command.Stream.ShouldBeNull();
            command.StreamCandidates.ShouldBeEmpty();
            command.Properties.Select(property => property.Name).SequenceEqual(["stream", "deeper"]).ShouldBeTrue();
            command.Handler!.Code!.Language.ShouldEqual(language);
        }
        var texts = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["application.play"] = "module M\n  feature F\n    import \"barrel.play\"",
            ["barrel.play"] = "import \"command.play\"",
            ["command.play"] = "slice StateChange S\n  command C\n    stream Fake.S\n      deeper String\n    handler\n      ```" + language + Code,
            ["sources.play"] = "import Account.Transactions\neventsource Account\n  stream Transactions\nmodule Types\n  feature Types\n    slice StateChange Types\n      command Typed\n        stream Account.Transactions"
        };
        foreach (var roots in new[] { texts.Keys.AsEnumerable(), texts.Keys.Reverse() })
        {
            var assembled = PlayApplicationAssembly.Compile(compiler, roots, new InMemoryPlayDocumentSource(texts)).Result;
            assembled.Success.ShouldBeTrue();
            assembled.Value!.Types!.ShouldBeEmpty();
            assembled.Value.EventSources.Single().Name.ShouldEqual("Account");
            var command = assembled.Value.Modules.Single(module => module.Name == "M").Features.Single().Slices.Single().Commands.Single();
            command.Stream.ShouldBeNull();
            command.Properties.Count().ShouldEqual(2);
            assembled.Value.Modules.Single(module => module.Name == "Types").Features.Single().Slices.Single().Commands.Single().Stream!.PropertyCandidate.ShouldBeNull();
        }
    }

    [Fact]
    void should_keep_unregistered_language_reporting_without_an_implicit_registry_expansion()
    {
        const string Source = "module M\n  feature F\n    slice StateChange S\n      command C\n        handler\n          ```python\n          print('eventsource Fake')\n          ```";
        var compiler = new ScreenplayCompiler();
        var result = compiler.Compile(Source);
        result.Success.ShouldBeFalse();
        result.Diagnostics.Any(diagnostic => diagnostic.Code == "PLAY0163").ShouldBeTrue();
        compiler.Parse(Source).Diagnostics.Select(diagnostic => diagnostic.Code).SequenceEqual(result.Diagnostics.Select(diagnostic => diagnostic.Code)).ShouldBeTrue();
    }

    static CommandSyntax Command(ApplicationSyntax application) => application.Modules.Single().Features.Single().Slices.Single().Commands.Single();
}

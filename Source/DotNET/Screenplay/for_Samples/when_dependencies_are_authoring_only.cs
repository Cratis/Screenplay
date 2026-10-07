// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Dependencies.for_DependencyGraph.given;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.for_Samples;

public class when_dependencies_are_authoring_only
{
    [Theory]
    [InlineData("Library", false)]
    [InlineData("Invoicing", false)]
    [InlineData("Commerce", false)]
    [InlineData("TimeTracking", false)]
    void should_preserve_syntax_or_executable_bytes_and_revision(string sample, bool binds)
    {
        var folder = Path.Combine(a_conformance_suite.Root(), "Samples", sample);
        var files = Directory.GetFiles(folder, "*.play", SearchOption.AllDirectories).Order(StringComparer.Ordinal)
            .ToDictionary(path => Path.GetRelativePath(folder, path).Replace('\\', '/'), File.ReadAllText, StringComparer.Ordinal);
        var compiler = new ScreenplayCompiler();
        var (_, compilation) = PlayApplicationAssembly.Compile(compiler, files.Keys, new InMemoryPlayDocumentSource(files), compiler.Languages, out _);
        var before = compilation.Value!;
        var after = before with
        {
            Modules = [.. before.Modules.Select(module => module with
            {
                DependsOn = [new("OtherModule", module.Location)],
                Features = [.. module.Features.Select(Add)]
            })]
        };
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create(sample));
        var documents = SemanticDocumentSet.Create([.. files.Select(file => SemanticSourceDocument.Create(catalog.ResolveDocument(file.Key.Replace('/', '_')), file.Key.Replace('/', '_'), file.Key, file.Value))], catalog);
        var binder = new SemanticModelBinder();
        var baseline = binder.Bind(sample, before, documents);
        var declared = binder.Bind(sample, after, documents);
        baseline.Success.ShouldEqual(binds);
        declared.Success.ShouldEqual(baseline.Success);
        declared.Diagnostics.Select(diagnostic => (diagnostic.Code, diagnostic.Message)).ShouldEqual(baseline.Diagnostics.Select(diagnostic => (diagnostic.Code, diagnostic.Message)));
        if (baseline.Success)
        {
            SemanticModelCanonicalJson.Serialize(declared.Value!.Model).ShouldEqual(SemanticModelCanonicalJson.Serialize(baseline.Value!.Model));
            declared.Value.Model.Revision.ShouldEqual(baseline.Value.Model.Revision);
        }
        else
        {
            var stripped = after with
            {
                Modules = [.. after.Modules.Select(module => module with { DependsOn = [], Features = [.. module.Features.Select(Strip)] })]
            };
            SyntaxJson.StructurallyEqual(before, stripped).ShouldBeTrue();
        }
    }

    static FeatureSyntax Add(FeatureSyntax feature) => feature with { DependsOn = [new("OtherFeature", feature.Location)], Features = [.. feature.Features.Select(Add)] };
    static FeatureSyntax Strip(FeatureSyntax feature) => feature with { DependsOn = [], Features = [.. feature.Features.Select(Strip)] };
}

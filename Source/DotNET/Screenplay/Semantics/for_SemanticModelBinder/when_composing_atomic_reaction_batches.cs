// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_composing_atomic_reaction_batches : Specification
{
    const string Module = "module M\n  feature F\n    slice Automation S\n      event Started\n      event Finished\n      reaction FollowUp\n        when Started\n          produces Finished\n";

    [Fact]
    void should_inherit_the_root_mode_without_redeclaring_it_in_imports()
    {
        var sources = new InMemoryPlayDocumentSource(new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["application.play"] = "reaction batches atomic\nimport \"module.play\"\n",
            ["module.play"] = Module
        });
        var (_, compilation) = PlayApplicationAssembly.Compile(new ScreenplayCompiler(), ["application.play"], sources);
        compilation.Success.ShouldBeTrue();
        var application = compilation.Value!;
        application.ReactionProductionMode.ShouldEqual(ReactionProductionMode.Atomic);
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("AtomicImports"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("single"), "single", "single.play", new ScreenplayPrinter().Print(application with { FileImports = [] }));
        var compiled = new SemanticModelCompiler().Compile("AtomicImports", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compiled.Success, string.Join('\n', compiled.Diagnostics.Select(diagnostic => diagnostic.Message)));
        compiled.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V22);
    }

    [Fact]
    void should_keep_the_mode_only_in_the_layout_root()
    {
        var parsed = new ScreenplayCompiler().Parse("reaction batches atomic\n" + Module).Value!;
        var expanded = WorkspaceFolderLayout.Expand(parsed, new ScreenplayPrinter()).ToArray();
        expanded.Count(file => file.Content.Contains("reaction batches atomic", StringComparison.Ordinal)).ShouldEqual(1);
        var compiler = new ScreenplayCompiler();
        var merged = PlayFolderMerge.Merge([.. expanded.Select(file => compiler.Parse(file.Content))]);
        merged.Success.ShouldBeTrue();
        merged.Value!.ReactionProductionMode.ShouldEqual(ReactionProductionMode.Atomic);
    }

    [Fact]
    void should_refuse_duplicate_modes_across_physical_documents()
    {
        var compiler = new ScreenplayCompiler();
        var merged = PlayFolderMerge.Merge([compiler.Parse("reaction batches atomic"), compiler.Parse("reaction batches atomic\n" + Module)]);
        merged.Success.ShouldBeFalse();
        merged.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidReactionBatchesDeclaration);
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.for_SemanticModelCompiler;

public class when_compiling_file_and_folder_forms : Specification
{
    const string Concepts =
        """
        concept ProjectId : Uuid
        concept ProjectName : String
        """;
    const string Behavior =
        """
        type ProjectDetails
          projectId ProjectId
          name ProjectName
        module Projects
          feature Registration
            slice StateChange RegisterProject
        """;

    readonly ApplicationIdentity _applicationIdentity = ApplicationIdentity.Create("Projects");
    CompilationResult<SemanticCompilation> _folder;
    CompilationResult<SemanticCompilation> _single;

    void Because()
    {
        var compiler = new SemanticModelCompiler();
        _single = compiler.Compile("Projects", Documents(("single", "application.play", $"{Concepts}\n{Behavior}")));
        _folder = compiler.Compile(
            "Projects",
            Documents(
                ("behavior", "Projects/Registration.play", Behavior),
                ("concepts", "Concepts.play", Concepts)));
    }

    [Fact] void should_compile_the_single_file() => _single.Success.ShouldBeTrue();
    [Fact] void should_compile_the_folder() => _folder.Success.ShouldBeTrue();
    [Fact] void should_have_no_single_file_diagnostics() => _single.Diagnostics.ShouldBeEmpty();
    [Fact] void should_have_no_folder_diagnostics() => _folder.Diagnostics.ShouldBeEmpty();
    [Fact] void should_produce_the_same_semantic_revision() => _folder.Value!.Model.Revision.ShouldEqual(_single.Value!.Model.Revision);
    [Fact]
    void should_produce_the_same_canonical_semantic_bytes() =>
        Serialization.SemanticModelSerializer.Serialize(_folder.Value!.Model)
            .SequenceEqual(Serialization.SemanticModelSerializer.Serialize(_single.Value!.Model))
            .ShouldBeTrue();

    SemanticDocumentSet Documents(params (string StableKey, string Path, string Text)[] sources)
    {
        var catalog = SemanticIdentityCatalog.Empty(_applicationIdentity);
        var documents = sources
            .Select(source => SemanticSourceDocument.Create(
                catalog.ResolveDocument(source.StableKey),
                source.StableKey,
                source.Path,
                source.Text))
            .ToImmutableArray();
        return SemanticDocumentSet.Create(documents, catalog);
    }
}

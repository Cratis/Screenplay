// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Files.for_PlayFileWriter;

/// <summary>
/// The gate on the two directions agreeing: the sample exercises the whole language, so writing it out as a
/// folder and compiling that folder back has to give the same application - and expanding that application
/// again has to give the same files, byte for byte.
/// </summary>
public class when_round_tripping_the_invoicing_sample_through_a_folder : Specification
{
    DirectoryInfo _root;
    ScreenplayPrinter _printer;
    ApplicationSyntax _original;
    IEnumerable<PlayFileContent> _written;
    ApplicationCompilation<ApplicationSyntax> _recompiled;
    IEnumerable<PlayFileContent> _rewritten;

    void Establish()
    {
        _root = Directory.CreateTempSubdirectory("playroundtrip");
        _printer = new();
        _original = new ScreenplayCompiler().Compile(for_ScreenplayCompiler.given.Samples.Invoicing).Value!;
    }

    void Because()
    {
        var writer = new PlayFileWriter();
        _written = writer.Expand(_original);
        writer.WriteTo(_original, _root.FullName);
        _recompiled = new PlayFileCompiler().CompileFolder(_root.FullName);
        _rewritten = writer.Expand(_recompiled.Result.Value!);
    }

    [Fact] void should_compile_the_folder_back() => _recompiled.Result.Success.ShouldBeTrue();
    [Fact] void should_resolve_every_reference_across_the_folder() => _recompiled.Result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_discover_every_file_it_wrote() => _recompiled.Sources.Count().ShouldEqual(_written.Count());
    [Fact] void should_write_a_file_per_module_feature_slice_and_one_for_the_application() => _written.Count().ShouldEqual(1 + Modules + Features + Slices);
    [Fact] void should_give_back_an_equivalent_application() => _printer.Print(_recompiled.Result.Value!).ShouldEqual(_printer.Print(InPathOrder(_original)));
    [Fact] void should_expand_to_the_same_files_again() => _rewritten.Select(file => file.RelativePath).ShouldContainOnly(_written.Select(file => file.RelativePath));
    [Fact] void should_expand_to_the_same_content_again() => _rewritten.Select(Content).ShouldContainOnly(_written.Select(Content));

    void Destroy() => _root.Delete(true);

    int Modules => _original.Modules.Count();

    int Features => _original.Modules.Sum(module => module.Features.Sum(Count));

    int Slices => _original.Modules.Sum(module => module.Features.Sum(feature => All(feature).Sum(nested => nested.Slices.Count())));

    static int Count(FeatureSyntax feature) => All(feature).Count();

    static IEnumerable<FeatureSyntax> All(FeatureSyntax feature) => [feature, .. feature.Features.SelectMany(All)];

    static string Content(PlayFileContent file) => $"{file.RelativePath}\n{file.Content}";

    /// <summary>
    /// Reorders the tree the way a folder does. A file system has no declaration order - it has paths - so a
    /// folder gives modules, features and slices back sorted by name rather than in the order they were
    /// authored. Everything else is expected to come back exactly as it went in.
    /// </summary>
    /// <param name="application">The <see cref="ApplicationSyntax"/> to reorder.</param>
    /// <returns>The reordered <see cref="ApplicationSyntax"/>.</returns>
    static ApplicationSyntax InPathOrder(ApplicationSyntax application) =>
        application with { Modules = [.. application.Modules.OrderBy(module => module.Name, StringComparer.Ordinal).Select(InPathOrder)] };

    static ModuleSyntax InPathOrder(ModuleSyntax module) =>
        module with { Features = [.. module.Features.OrderBy(feature => feature.Name, StringComparer.Ordinal).Select(InPathOrder)] };

    static FeatureSyntax InPathOrder(FeatureSyntax feature) =>
        feature with
        {
            Features = [.. feature.Features.OrderBy(nested => nested.Name, StringComparer.Ordinal).Select(InPathOrder)],
            Slices = [.. feature.Slices.OrderBy(slice => slice.Name, StringComparer.Ordinal)]
        };
}

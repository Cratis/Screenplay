// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Files.for_PlayFileWriter;

public class when_preserving_typed_examples_across_files : Specification
{
    CompilationResult<ApplicationSyntax> _result;
    ApplicationSyntax _sliceDocument;

    void Because()
    {
        var compiler = new ScreenplayCompiler();
        var printer = new ScreenplayPrinter();
        var original = compiler.Parse("module M\n  example ModuleExample : E\n    p = 1\n  feature F\n    example FeatureExample : E\n      p = 2\n    slice StateChange S\n      event E\n        p Int\n      example SliceExample : E\n        p = 3").Value!;
        var module = original.Modules.Single();
        var feature = module.Features.Single();
        _sliceDocument = PlayFileDocument.ForSlice(module, [], feature, feature.Slices.Single());
        _result = PlayFolderMerge.Merge(
        [
            compiler.Parse(printer.Print(PlayFileDocument.ForModule(module)), "module.play"),
            compiler.Parse(printer.Print(PlayFileDocument.ForFeature(module, [], feature)), "feature.play"),
            compiler.Parse(printer.Print(_sliceDocument), "slice.play")
        ]);
    }

    [Fact] void should_merge_without_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_keep_each_module_example_once() => _result.Value!.Modules.Single().Examples.Single().Name.ShouldEqual("ModuleExample");
    [Fact] void should_keep_each_feature_example_once() => _result.Value!.Modules.Single().Features.Single().Examples.Single().Name.ShouldEqual("FeatureExample");
    [Fact] void should_keep_each_slice_example_once() => _result.Value!.Modules.Single().Features.Single().Slices.Single().Examples.Single().Name.ShouldEqual("SliceExample");
    [Fact] void should_not_copy_module_examples_into_slice_scaffolding() => _sliceDocument.Modules.Single().Examples.ShouldBeEmpty();
    [Fact] void should_not_copy_feature_examples_into_slice_scaffolding() => _sliceDocument.Modules.Single().Features.Single().Examples.ShouldBeEmpty();
}

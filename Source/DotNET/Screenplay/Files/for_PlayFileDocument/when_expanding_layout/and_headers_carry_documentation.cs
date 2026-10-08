// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Files.for_PlayFileDocument.when_expanding_layout;

public class and_headers_carry_documentation : Specification
{
    ModuleSyntax _module;
    FeatureSyntax _feature;

    void Establish()
    {
        _module = new ScreenplayCompiler().Parse("module M\n  documentation\n    ```markdown\n    Module reasoning.\n    ```\n  feature F\n    documentation\n      ```markdown\n      Feature reasoning.\n      ```\n    slice StateView V\n").Value!.Modules.Single();
        _feature = _module.Features.Single();
    }

    [Fact] void should_keep_documentation_in_the_module_file() => PlayFileDocument.ForModule(_module).Modules.Single().Documentation.ShouldEqual("Module reasoning.");
    [Fact] void should_strip_the_module_wrapper_in_feature_files() => PlayFileDocument.ForFeature(_module, [], _feature).Modules.Single().Documentation.ShouldBeNull();
    [Fact] void should_keep_the_feature_documentation_in_its_file() => PlayFileDocument.ForFeature(_module, [], _feature).Modules.Single().Features.Single().Documentation.ShouldEqual("Feature reasoning.");
    [Fact] void should_strip_the_feature_wrapper_in_slice_files() => PlayFileDocument.ForSlice(_module, [], _feature, _feature.Slices.Single()).Modules.Single().Features.Single().Documentation.ShouldBeNull();
}

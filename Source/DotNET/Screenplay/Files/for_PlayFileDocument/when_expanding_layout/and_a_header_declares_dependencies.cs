// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Files.for_PlayFileDocument.when_expanding_layout;

public class and_a_header_declares_dependencies : Specification
{
    ModuleSyntax _module;
    FeatureSyntax _parent;
    FeatureSyntax _child;

    void Establish()
    {
        _module = new ScreenplayCompiler().Parse("module Payroll\n  depends on Timesheets\n  feature Handover\n    depends on Runs\n    feature Nested\n      depends on Timesheets.Approval\n      slice StateView View\n").Value!.Modules.Single();
        _parent = _module.Features.Single();
        _child = _parent.Features.Single();
    }

    [Fact] void should_keep_module_declarations_only_in_the_module_file() => PlayFileDocument.ForModule(_module).Modules.Single().DependsOn.Count().ShouldEqual(1);
    [Fact] void should_strip_the_module_header_in_feature_files() => PlayFileDocument.ForFeature(_module, [_parent], _child).Modules.Single().DependsOn.ShouldBeEmpty();
    [Fact] void should_strip_ancestor_feature_headers() => PlayFileDocument.ForFeature(_module, [_parent], _child).Modules.Single().Features.Single().DependsOn.ShouldBeEmpty();
    [Fact] void should_keep_the_owning_feature_declarations() => PlayFileDocument.ForFeature(_module, [_parent], _child).Modules.Single().Features.Single().Features.Single().DependsOn.Count().ShouldEqual(1);
    [Fact] void should_strip_the_owning_feature_in_slice_files() => PlayFileDocument.ForSlice(_module, [_parent], _child, _child.Slices.Single()).Modules.Single().Features.Single().Features.Single().DependsOn.ShouldBeEmpty();
}

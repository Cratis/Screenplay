// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Completeness;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Files.for_PlayFolderMerge;

public class when_merging_purpose_references : Specification
{
    CompilationResult<ApplicationSyntax> _result;

    void Because()
    {
        var compiler = new ScreenplayCompiler();
        _result = PlayFolderMerge.Merge([
            compiler.Parse("purpose Billing\n  basis contract\nmodule M\n  purpose Billing\n  feature F\n    purpose Billing", "one.play"),
            compiler.Parse("purpose Claims\n  basis consent\nmodule M\n  purpose Billing\n  feature F\n    purpose Claims\n    slice StateChange S\n      purpose Billing", "two.play")
        ]);
    }

    [Fact] void should_merge_without_findings() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_keep_repeated_module_references_once() => _result.Value!.Modules.Single().Purposes.Count().ShouldEqual(1);
    [Fact] void should_accumulate_feature_references() => _result.Value!.Modules.Single().Features.Single().Purposes.Select(reference => reference.Name).ShouldContainOnly("Billing", "Claims");
    [Fact] void should_use_the_union_for_slice_coverage() => PurposeCoverage.Slices(_result.Value!).Single().Purposes.Select(reference => reference.Name).ShouldContainOnly("Billing", "Claims");
}

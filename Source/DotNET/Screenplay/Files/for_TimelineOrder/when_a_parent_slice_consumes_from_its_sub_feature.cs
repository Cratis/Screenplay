// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Files.for_TimelineOrder;

public class when_a_parent_slice_consumes_from_its_sub_feature : Specification
{
    CompilationResult<Syntax.ApplicationSyntax> _result;
    void Because() => _result = new ScreenplayCompiler().Compile("module M\n  feature F\n    feature Child\n      slice StateChange Write\n        event E\n    slice StateView View\n      projection P\n        from E");
    [Fact] void should_draw_parent_slices_before_sub_features() => _result.Diagnostics.Select(diagnostic => $"{diagnostic.Code}@{diagnostic.Location.Line}").ShouldContainOnly("PLAY0516@8");
    [Fact] void should_explain_that_reordering_cannot_fix_it() => _result.Diagnostics.Single().Message.ShouldContain("cannot be fixed by reordering");
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_validating_read_model_member_types;

public class and_only_parsing_the_document : given.a_member
{
    void Because() => Result = Compiler.Parse(Source, "model.play");

    [Fact] void should_preserve_the_unsupported_member_for_authoring() => Result.Success.ShouldBeTrue();
    [Fact] void should_not_validate_an_isolated_document() => Result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_preserve_the_member_path() => Result.Value!.Modules.Single().Features.Single().Slices.Single().ReadModels!.Single(model => model.Name == "Parent").Properties.Single().Type.Location.Path.ShouldEqual("model.play");
}

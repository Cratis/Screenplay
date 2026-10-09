// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_public_events;

public class a_model_without_public_events : given.a_public_events_model
{
    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind("module M\n  feature F\n    slice Translate T\n      event Imported\n        id String\n");

    [Fact] void should_bind() => _result.Success.ShouldBeTrue();
    [Fact] void should_keep_its_earlier_version() => _result.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V6);
}

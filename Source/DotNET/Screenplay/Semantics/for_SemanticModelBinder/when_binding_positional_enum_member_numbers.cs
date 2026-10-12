// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_positional_enum_member_numbers : given.a_semantic_binder
{
    CompilationResult<SemanticCompilation> _positional;
    CompilationResult<SemanticCompilation> _pinned;

    void Because()
    {
        _positional = Bind("concept Status : Enum\n  draft\n  issued");
        _pinned = Bind("concept Status : Enum\n  draft = -0\n  issued = 1");
    }

    [Fact] void should_bind_both_forms() => (_positional.Success && _pinned.Success).ShouldBeTrue();
    [Fact] void should_keep_the_same_version() => _pinned.Value!.Model.SemanticVersion.ShouldEqual(_positional.Value!.Model.SemanticVersion);
    [Fact] void should_keep_the_same_bytes() => SemanticModelSerializer.Serialize(_pinned.Value!.Model).SequenceEqual(SemanticModelSerializer.Serialize(_positional.Value!.Model)).ShouldBeTrue();
    [Fact] void should_keep_the_same_revision() => _pinned.Value!.Model.Revision.ShouldEqual(_positional.Value!.Model.Revision);
}

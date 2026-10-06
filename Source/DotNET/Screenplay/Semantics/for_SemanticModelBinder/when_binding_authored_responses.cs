// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_authored_responses : given.a_semantic_binder
{
    const string Prefix = "concept Id : Uuid\nmodule M\n  feature F\n    slice StateChange S\n";

    [Theory]
    [InlineData("      command C\n        id Id generated")]
    [InlineData("      command C\n        id Id\n        returns id")]
    [InlineData("      command C\n        id Id\n        returns\n          value = id")]
    void should_bind_v7_authoring(string body)
    {
        var result = Bind(Prefix + body);
        result.Success.ShouldBeTrue();
        result.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V7);
    }

    [Fact]
    void should_admit_the_legacy_tab_separated_property_without_creating_a_response()
    {
        const string Source = "concept id : Uuid\nmodule M\n  feature F\n    slice StateChange S\n      command C\n        id id identifier\n        returns\tid";
        var syntax = new ScreenplayCompiler().Compile(Source);
        syntax.Success.ShouldBeTrue();
        syntax.Value!.Modules.Single().Features.Single().Slices.Single().Commands.Single().Response.ShouldBeNull();
        var result = Bind(Source);
        result.Success.ShouldBeTrue();
        result.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V1);
    }

    [Theory]
    [InlineData("returns String")]
    [InlineData("@returns String")]
    void should_preserve_the_legacy_model_for_deeper_members_after_a_returns_property(string property)
    {
        var nested = Bind(Prefix + "      command C\n        " + property + "\n          value Int");
        var ordinary = Bind(Prefix + "      command C\n        @returns String\n        value Int");
        nested.Success.ShouldBeTrue();
        ordinary.Success.ShouldBeTrue();
        Serialization.SemanticModelSerializer.Serialize(nested.Value!.Model)
            .SequenceEqual(Serialization.SemanticModelSerializer.Serialize(ordinary.Value!.Model)).ShouldBeTrue();
        nested.Value.Model.Revision.ShouldEqual(ordinary.Value.Model.Revision);
        nested.Value.Model.SemanticVersion.ShouldEqual(SemanticVersion.V1);
    }
}

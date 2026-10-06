// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_selecting_v7_only_when_used : given.a_semantic_binder
{
    const string Prefix = "module M\n  feature F\n    slice StateChange S\n";

    [Theory]
    [InlineData(1, "      command C\n        name String")]
    [InlineData(2, "      command C\n        id Uuid identifier\n        produces Created\n          for id\n      event Created")]
    [InlineData(3, "      command C\n        name String\n        validate\n          ```csharp\n          yield break;\n          ```")]
    [InlineData(4, "      event Created\n        name String\n      event Created generation 2\n        name String")]
    [InlineData(5, "      readmodel View\n        id String\n      query ById => View?\n        by id String\n      specification Absent\n        then no readmodel View for \"missing\"")]
    [InlineData(6, "    slice Automation A\n      reaction R\n        when Startup\n          produces Started\n            for \"start\"\n      event Started")]
    void should_keep_unchanged_models_on_their_original_version_and_canonical_bytes(int version, string body)
    {
        var original = Bind(Prefix + body);
        var relocated = Bind(Prefix + body, displayPath: "relocated.play");
        original.Success.ShouldBeTrue();
        relocated.Success.ShouldBeTrue();
        original.Value!.Model.SemanticVersion.ToString().ShouldEqual(version + ".0");
        original.Value.Model.LanguageVersion.ToString().ShouldEqual(version + ".0");
        SemanticModelSerializer.Serialize(relocated.Value!.Model).ShouldEqual(SemanticModelSerializer.Serialize(original.Value.Model));
        relocated.Value.Model.Revision.ShouldEqual(original.Value.Model.Revision);
    }

    [Fact]
    void should_promote_a_v1_destination_when_only_a_response_selects_v7()
    {
        var result = Bind(Prefix + "      command C\n        id String identifier\n        returns id\n        produces Created\n          for id\n          id = id\n      event Created\n        id String");
        result.Success.ShouldBeTrue();
        result.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V7);
        result.Value.Model.Application.Modules.Single().Features.Single().Slices.Single().Commands.Single().Destination.ShouldNotBeNull();
    }

    [Fact]
    void should_refuse_exact_mode_before_response_admission()
    {
        var result = Bind("numbers exact\n" + Prefix + "      command C\n        name String\n        returns name");
        result.Success.ShouldBeFalse();
        result.Diagnostics.Single().Code.ShouldEqual("PLAY0268");
        result.Diagnostics.Single().Message.ShouldContain("Exact numeric source mode");
    }
}

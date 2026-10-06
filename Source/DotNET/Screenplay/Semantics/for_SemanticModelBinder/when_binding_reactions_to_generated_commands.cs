// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_reactions_to_generated_commands : given.a_semantic_binder
{
    const string Prefix = "concept Id : Uuid\nmodule M\n  feature F\n    slice StateChange S\n      command C\n        id Id generated identifier\n        name String\n        note String?\n        returns id\n    slice Automation A\n      reaction R\n        when Startup\n          invokes C\n";

    [Fact]
    void should_require_only_request_input_mappings()
    {
        var result = Bind(Prefix + "            name = \"hello\"");
        result.Success.ShouldBeTrue();
        result.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V7);
    }

    [Fact]
    void should_refuse_mapping_into_a_generated_property()
    {
        var result = Bind(Prefix + "            name = \"hello\"\n            id = \"11111111-1111-1111-1111-111111111111\"");
        result.Success.ShouldBeFalse();
        result.Diagnostics.Any(diagnostic => diagnostic.Code == "PLAY0485").ShouldBeTrue();
    }

    [Fact]
    void should_still_require_required_request_inputs()
    {
        var result = Bind(Prefix);
        result.Success.ShouldBeFalse();
        result.Diagnostics.Any(diagnostic => diagnostic.Code == "PLAY0273" && diagnostic.Message.Contains("required value 'name'", StringComparison.Ordinal)).ShouldBeTrue();
        result.Diagnostics.Any(diagnostic => diagnostic.Message.Contains("required value 'id'", StringComparison.Ordinal)).ShouldBeFalse();
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_protected_reads;

public class without_declared_reads : given.a_protected_read_model
{
    [Fact]
    void should_select_v24_from_the_preamble_alone()
    {
        var result = Bind("command reads protected");
        result.Success.ShouldBeTrue();
        result.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V24);
    }
}

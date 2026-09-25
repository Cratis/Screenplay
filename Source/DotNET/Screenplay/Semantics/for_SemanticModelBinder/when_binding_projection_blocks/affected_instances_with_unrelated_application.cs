// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_projection_blocks;

public class affected_instances_with_unrelated_application : given.a_projection_block_binder
{
    ArgumentException _exception = null!;

    void Because()
    {
        _result = BindProjection("projection Orders => OrderView", "children lines identified by lineNumber\n  from LineAdded key lineNumber\n    parent orderId\n    subtotal = amount");
        var unrelated = Application with { Modules = [] };
        _exception = Assert.Throws<ArgumentException>(() => Projection.GetAffectedInstances(unrelated));
    }

    [Fact] void should_name_the_application_parameter() => _exception.ParamName.ShouldEqual("application");
    [Fact] void should_identify_the_missing_read_model() => _exception.Message.ShouldContain(Projection.ReadModel.ToString());
}

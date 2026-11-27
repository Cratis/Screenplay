// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_structured_specification_values;

public class and_several_list_items_are_invalid : for_SemanticModelBinder.given.a_semantic_binder
{
    const string Source =
        """
        type Line
          sku String
          quantity Int
        module Shop
          feature Orders
            slice StateView OrderLookup
              readmodel OrderView
                id Uuid
                lines Line[]
              query OrderById => OrderView?
                by id Uuid
              specification ReadingAnOrder
                then readmodel OrderView
                  id = "00000000-0000-0000-0000-000000000001"
                  lines
                    item
                      quantity = 1
                    item
                      sku = null
                      quantity = 2
        """;

    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind(Source);

    [Fact] void should_refuse_binding() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_the_missing_member_then_the_invalid_null_in_list_order() =>
        _result.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).Select(diagnostic => diagnostic.Code)
            .ShouldEqual(DiagnosticCodes.MissingStructuredValueMember, DiagnosticCodes.InvalidSpecificationNull);
}

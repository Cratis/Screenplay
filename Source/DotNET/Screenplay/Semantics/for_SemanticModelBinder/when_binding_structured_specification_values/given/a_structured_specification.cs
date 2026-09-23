// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_structured_specification_values.given;

public class a_structured_specification : for_SemanticModelBinder.given.a_semantic_binder
{
    protected const string Source =
        """
        type Detail
          enabled Bool
        type Line
          sku String
          detail Detail
        module Shop
          feature Orders
            slice StateChange Place
              command Place
                id Uuid identifier
                lines Line[]
                produces Placed
                  for id
                  id = id
                  lines = lines
              event Placed
                id Uuid identifier
                lines Line[]
              readmodel OrderView
                id Uuid
                lines Line[]
                tags String[]
                note String?
              query OrderById => OrderView?
                by id Uuid
              specification SeedingAnOrder
                given readmodel OrderView
                  id = "00000000-0000-0000-0000-000000000123"
                  lines = [{"sku":"A-1","detail":{"enabled":true}}]
                  tags = []
                  note = null
                then readmodel OrderView
                  id = "00000000-0000-0000-0000-000000000123"
                  lines = [{"sku":"A-1","detail":{"enabled":true}}]
              specification PlacingAnOrder
                when Place
                  id = "00000000-0000-0000-0000-000000000124"
                  lines = [{"sku":"A-1","detail":{"enabled":true}}]
                then Placed
                  id = "00000000-0000-0000-0000-000000000124"
                  lines = [{"sku":"A-1","detail":{"enabled":true}}]
        """;

    protected CompilationResult<SemanticCompilation> Compile(string source) => Bind(source);
}

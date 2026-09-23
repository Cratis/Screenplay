// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_compiling_structured_values.given;

public class a_structured_specification : Specification
{
    protected const string Source =
        """
        domain Orders
        concept Status : Enum
          open
          closed
        type Line
          sku String
          status Status
        module Shop
          feature Orders
            slice StateChange Place
              command Place
                lines Line[]
                status Status
                produces Placed
                  lines = lines
              event Placed
                lines Line[]
              specification Placing
                when Place
                  lines = [{"sku":"A-1","status":"open"}]
                  status = "open"
                then Placed
                  lines = [{"sku":"A-1","status":"open"}]
        """;

    protected readonly ScreenplayCompiler Compiler = new();
}

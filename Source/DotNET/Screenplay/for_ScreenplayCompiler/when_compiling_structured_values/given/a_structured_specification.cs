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
                  lines
                    item
                      sku = "A-1"
                      status = "open"
                  status = "open"
                then Placed
                  lines
                    item
                      sku = "A-1"
                      status = "open"
        """;

    // The structured 'lines' value under 'when' and 'then', as it sits in the source.
    protected const string Lines = "lines\n            item\n              sku = \"A-1\"\n              status = \"open\"";

    protected readonly ScreenplayCompiler Compiler = new();
}

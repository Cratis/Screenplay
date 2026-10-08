// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_ScopedDiagnostics.given;

public class a_model : Specification
{
    protected Dictionary<string, string> Sources = new(StringComparer.Ordinal)
    {
        ["application.play"] = """
            module Orders
              feature Registration
                slice StateChange Add
                  event Added
                    value UnknownTarget
                slice StateChange Sibling
                  event SiblingEvent
                    value UnknownSibling
            module Reporting
              feature Views
                slice StateChange Consume
                  command Use
                    id String identifier
                    bad UnknownDependent
                    produces Added
                      for id
                      value = id
                  command Unrelated
                    bad UnknownUnrelated
                  readmodel View
                    value String
            module Transitive
              feature Views
                slice StateChange Consume
                  command Further
                    id String identifier
                    bad UnknownTransitive
                    reads View
            module OrdersExtra
              feature Registration
                slice StateChange Add
                  event ExtraEvent
                    value UnknownPrefix
            """
    };
}

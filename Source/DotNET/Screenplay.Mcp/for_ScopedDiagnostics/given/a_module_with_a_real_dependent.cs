// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_ScopedDiagnostics.given;

public class a_module_with_a_real_dependent : Specification
{
    protected Dictionary<string, string> Sources = new(StringComparer.Ordinal)
    {
        ["application.play"] = "module Orders\n  feature F\n    slice StateChange Clean\n      event Added",
        ["consumer.play"] = "module Reporting\n  feature F\n    slice StateChange Good\n      command Consume\n        id String identifier\n        produces Added\n          for id"
    };
}

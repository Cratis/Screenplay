// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_scoping_a_module_with_same_named_unrelated_declarations : given.a_connection
{
    JsonElement _result;

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), "type Orders\n  broken\nconcept Orders : String\n  broken\neventsource Orders\n  stream Special\n    broken\nmodule Orders\n  feature F\n    slice StateChange Clean\n      event Added\nmodule Outside\n  feature F\n    slice StateChange Bad\n      command Use\n        value Orders\n        stream Orders.Special\n        broken");
        Initialize();
    }

    void Because() => _result = Call("diagnostics", new { scope = "Orders" }).GetProperty("result").GetProperty("structuredContent");

    [Fact] void should_keep_the_scoped_verdict_clean() => _result.GetProperty("success").GetBoolean().ShouldBeTrue();
    [Fact] void should_preserve_the_whole_application_failure() => _result.GetProperty("wholeApplicationSuccess").GetBoolean().ShouldBeFalse();
    [Fact] void should_count_only_the_module_hierarchy() => _result.GetProperty("declarationCount").GetInt32().ShouldEqual(4);
    [Fact] void should_exclude_unrelated_dependents() => _result.GetProperty("dependentDeclarationCount").GetInt32().ShouldEqual(0);
    [Fact] void should_report_no_affected_scopes() => _result.GetProperty("affectedScopes").GetArrayLength().ShouldEqual(0);
    [Fact] void should_report_no_scoped_errors() => _result.GetProperty("summary").GetProperty("errors").GetInt32().ShouldEqual(0);
}

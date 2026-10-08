// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_scoping_diagnostics_with_an_unrelated_unresolved_event_consumer : given.a_connection
{
    JsonElement _result;

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), "module M\n  feature F\n    slice StateChange Clean\n      event CleanEvent\n        value String");
        File.WriteAllText(Path.Combine(RootPath, "consumer.play"), """
            module Other
              feature F
                slice StateChange Use
                  command Consume
                    id String identifier
                    broken
                    produces MissingEvent
                      for id
            """);
        Initialize();
    }

    void Because() => _result = Call("diagnostics", new { scope = "M.F.Clean" }).GetProperty("result").GetProperty("structuredContent");

    [Fact] void should_keep_the_scoped_success() => _result.GetProperty("success").GetBoolean().ShouldBeTrue();
    [Fact] void should_preserve_the_whole_application_failure() => _result.GetProperty("wholeApplicationSuccess").GetBoolean().ShouldBeFalse();
    [Fact] void should_not_claim_a_directly_affected_scope() => _result.GetProperty("affectedScopes").GetArrayLength().ShouldEqual(0);
    [Fact] void should_not_include_the_unattributable_error_in_the_page() => _result.GetProperty("page").GetProperty("totalCount").GetInt32().ShouldEqual(0);
    [Fact] void should_report_the_unresolved_event_reference_count() => _result.GetProperty("unresolvedEventConsumers").GetProperty("referenceCount").GetInt32().ShouldEqual(1);
    [Fact] void should_report_the_unresolved_consumer_scope() => _result.GetProperty("unresolvedEventConsumers").GetProperty("scopes")[0].GetString().ShouldEqual("Other.F.Use");
    [Fact] void should_not_count_the_event_consumer_again_as_possibly_affected() => _result.GetProperty("possiblyAffectedReferenceCount").GetInt32().ShouldEqual(0);
}

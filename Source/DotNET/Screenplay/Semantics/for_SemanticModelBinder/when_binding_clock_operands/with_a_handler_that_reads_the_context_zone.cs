// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_clock_operands;

public class with_a_handler_that_reads_the_context_zone : given.a_clock_binder
{
    const string Handler = "        handler\n          implementation\n            hint \"Decide with the calendar zone\"\n";

    string _withZone = string.Empty;
    string _withoutZone = string.Empty;

    void Because()
    {
        var withZone = BindClock(Source("        calendar from $context.timeZone\n        produces Done\n          for id\n" + Handler));
        var withoutZone = BindClock(Source("        produces Done\n          for id\n" + Handler));
        _withZone = Encoding.UTF8.GetString(SemanticTypedContextSerializer.Serialize(withZone.TypedContextDescriptors));
        _withoutZone = Encoding.UTF8.GetString(SemanticTypedContextSerializer.Serialize(withoutZone.TypedContextDescriptors));
    }

    [Fact] void should_describe_the_time_zone_member() => _withZone.ShouldContain("TimeZone");
    [Fact] void should_write_the_next_sidecar_contract_revision() => _withZone.ShouldContain("\"contractRevision\":2");
    [Fact] void should_keep_the_existing_revision_and_bytes_for_a_handler_without_the_zone() => _withoutZone.ShouldContain("\"contractRevision\":1");
    [Fact] void should_not_describe_the_time_zone_member_without_the_calendar() => _withoutZone.Contains("TimeZone", StringComparison.Ordinal).ShouldBeFalse();
}

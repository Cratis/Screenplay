// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_clock_operands;

public class with_a_handler_that_reads_the_context_zone : given.a_clock_binder
{
    const string TimeZoneMember = """
        {"name":"TimeZone","isNullable":false,"isDerived":false,"type":{"kind":"runtime","modelType":null,"shape":null,"runtimeToken":"TimeZone","properties":[]},"source":{"kind":"context-contract","semanticId":null,"path":"TimeZone","constantValue":null,"eventRevision":null}}
        """;

    byte[] _withZone = [];
    byte[] _withoutZone = [];
    byte[] _legacyBytes = [];
    byte[] _revisionTwoBytes = [];

    void Establish()
    {
        using var stream = typeof(when_describing_an_unbound_handler).Assembly.GetManifestResourceStream("Cratis.Screenplay.Semantics.Serialization.Golden.unbound-handler-context-v1.json")!;
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        _legacyBytes = buffer.ToArray();

        // The complete legacy vector stays fixed; revision 2 adds exactly one token and one final member.
        var legacy = Encoding.UTF8.GetString(_legacyBytes);
        _revisionTwoBytes = Encoding.UTF8.GetBytes(legacy
            .Replace("\"contractRevision\":1", "\"contractRevision\":2", StringComparison.Ordinal)
            .Replace("\"CausedBy\",\"Causation\"]", "\"CausedBy\",\"Causation\",\"TimeZone\"]", StringComparison.Ordinal)
            .Replace("]}]}", "," + TimeZoneMember + "]}]}", StringComparison.Ordinal));
    }

    void Because()
    {
        const string source = when_describing_an_unbound_handler.Source;
        var withZone = BindClock(source.Replace("        handler", "        calendar from $context.timeZone\n        handler", StringComparison.Ordinal));
        var withoutZone = BindClock(source);
        _withZone = SemanticTypedContextSerializer.Serialize(withZone.TypedContextDescriptors);
        _withoutZone = SemanticTypedContextSerializer.Serialize(withoutZone.TypedContextDescriptors);
    }

    [Fact] void should_preserve_every_legacy_sidecar_byte() => _withoutZone.SequenceEqual(_legacyBytes).ShouldBeTrue();
    [Fact] void should_write_exactly_the_revision_two_descriptor_and_token() => _withZone.SequenceEqual(_revisionTwoBytes).ShouldBeTrue();
}

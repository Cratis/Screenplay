// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Diagnostics;

using Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_clock_operands.given;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_clock_operands;

public class with_a_declared_context_member : given.a_clock_binder
{
    const string Context = "context\n  billingZone time zone\n  customerZone time zone\n";

    const string BillingZoneMember = """
        {"name":"BillingZone","isNullable":false,"isDerived":false,"type":{"kind":"runtime","modelType":null,"shape":null,"runtimeToken":"TimeZone","properties":[]},"source":{"kind":"application-context","semanticId":null,"path":"billingZone","constantValue":null,"eventRevision":null}}
        """;

    CompilationResult<ClockOperandsBinding> _declared = null!;
    CompilationResult<ClockOperandsBinding> _undeclared = null!;
    CompilationResult<ClockOperandsBinding> _withoutDeclarations = null!;
    CompilationResult<ClockOperandsBinding> _empty = null!;
    byte[] _sidecar = [];
    byte[] _expectedSidecar = [];

    void Establish()
    {
        using var stream = typeof(when_describing_an_unbound_handler).Assembly.GetManifestResourceStream("Cratis.Screenplay.Semantics.Serialization.Golden.unbound-handler-context-v1.json")!;
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);

        // Revision 3 keeps every revision 1 byte and adds the source kind, the token, the registry and one final member.
        _expectedSidecar = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(buffer.ToArray())
            .Replace("\"contractRevision\":1", "\"contractRevision\":3", StringComparison.Ordinal)
            .Replace("\"unavailable\"]", "\"unavailable\",\"application-context\"]", StringComparison.Ordinal)
            .Replace("\"CausedBy\",\"Causation\"]", "\"CausedBy\",\"Causation\",\"TimeZone\"],\"contextMembers\":[{\"name\":\"billingZone\",\"runtimeToken\":\"TimeZone\"}]", StringComparison.Ordinal)
            .Replace("]}]}", "," + BillingZoneMember + "]}]}", StringComparison.Ordinal));
    }

    void Because()
    {
        _declared = BindClock(Source("        calendar from $context.billingZone\n", Context));
        _undeclared = BindClock(Source("        calendar from $context.shopZone\n", Context));
        _withoutDeclarations = BindClock(Source("        calendar from $context.billingZone\n"));
        _empty = BindClock(Source(string.Empty, "context\n"));
        _sidecar = GoldenBytes();
    }

    internal static byte[] GoldenBytes()
    {
        const string source = when_describing_an_unbound_handler.Source;
        var handler = new with_a_declared_context_member().BindClock(Context + source.Replace("        handler", "        calendar from $context.billingZone\n        handler", StringComparison.Ordinal));
        return SemanticTypedContextSerializer.Serialize(handler.TypedContextDescriptors);
    }

    [Fact] void should_bind_the_declared_member() => Command(_declared.Value!).Calendar.ShouldEqual(new SemanticCommandCalendar(null, "billingZone"));
    [Fact] void should_select_the_clock_version() => _declared.Value!.LanguageVersion.Major.ShouldEqual(37u);
    [Fact] void should_accept_an_empty_context_block() => _empty.Success.ShouldBeTrue();

    [Fact]
    void should_refuse_an_undeclared_member_listing_the_declared_members() =>
        _undeclared.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidCalendarContextMember &&
            diagnostic.Message == "Command 'C' reads its calendar from '$context.shopZone'; the trusted members are '$context.timeZone', '$context.billingZone', '$context.customerZone'.");

    [Fact]
    void should_refuse_a_member_when_nothing_is_declared() =>
        _withoutDeclarations.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidCalendarContextMember &&
            diagnostic.Message.EndsWith("the only trusted member is '$context.timeZone'.", StringComparison.Ordinal));

    [Fact] void should_write_the_revision_three_sidecar() => Encoding.UTF8.GetString(_sidecar).ShouldEqual(Encoding.UTF8.GetString(_expectedSidecar));

    [Fact]
    void should_pin_the_packaged_revision_three_vector()
    {
        using var stream = typeof(with_a_declared_context_member).Assembly.GetManifestResourceStream("Cratis.Screenplay.Semantics.Serialization.Golden.unbound-handler-context-v3.json")!;
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        buffer.ToArray().ShouldEqual(_sidecar);
    }
}

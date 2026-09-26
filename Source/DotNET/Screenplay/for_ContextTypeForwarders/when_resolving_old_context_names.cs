// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Contexts;

namespace Cratis.Screenplay.for_ContextTypeForwarders;

public class when_resolving_old_context_names : Specification
{
    static readonly Type[] MovedTypes =
    [
        typeof(Causation), typeof(CausedBy), typeof(Claim), typeof(CommandContext),
        typeof(ContextPayloadTypeMismatch), typeof(Identity), typeof(PolicyContext),
        typeof(QueryContext), typeof(ReducerContext), typeof(RuleContext), typeof(TenantId)
    ];

    [Fact] void should_forward_every_public_context_type() => typeof(ScreenplayCompiler).Assembly.GetForwardedTypes().OrderBy(_ => _.FullName).ShouldEqual(MovedTypes.OrderBy(_ => _.FullName));

    [Fact] void should_resolve_old_assembly_qualified_names()
    {
        foreach (var type in MovedTypes)
        {
            Type.GetType($"{type.FullName}, Cratis.Screenplay").ShouldEqual(type);
            type.Assembly.GetName().Name.ShouldEqual("Cratis.Screenplay.Contexts");
        }
    }

    [Fact] void should_preserve_the_default_and_implicit_operator() => ((string)TenantId.Default).ShouldEqual("00000000-0000-0000-0000-000000000000");

    [Fact] void should_preserve_context_constructors_and_generic_accessors()
    {
        var context = new ReducerContext(null, new Claim("subject", "42"), "42", TenantId.Default, DateTimeOffset.UnixEpoch, 1);
        context.EventAs<Claim>().Value.ShouldEqual("42");
        context.StateAs<string>().ShouldBeNull();
    }
}

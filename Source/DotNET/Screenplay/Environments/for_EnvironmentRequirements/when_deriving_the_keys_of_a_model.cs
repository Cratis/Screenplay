// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Environments.for_EnvironmentRequirements;

public class when_deriving_the_keys_of_a_model : Specification
{
    const string Source =
        """
        persona Owner

        authentication
          provider EntraId
          provider GitHub
          provider OpenId name Partner
          provider OpenId name Supplier

        onboarding
          invite tenant as Owner
        """;

    EnvironmentRequirements _requirements;

    void Because() => _requirements = EnvironmentRequirements.For(new ScreenplayCompiler().Compile(Source).Value!);

    string[] KeysOf(string prefix) => [.. _requirements.Keys.Select(_ => _.Key).Where(_ => _.StartsWith(prefix, StringComparison.Ordinal))];

    [Fact] void should_key_a_provider_without_an_alias_by_its_name() => KeysOf("authentication.providers.EntraId.").ShouldContainOnly("authentication.providers.EntraId.clientId", "authentication.providers.EntraId.clientSecret", "authentication.providers.EntraId.tenantId");
    [Fact] void should_need_nothing_extra_from_github() => KeysOf("authentication.providers.GitHub.").ShouldContainOnly("authentication.providers.GitHub.clientId", "authentication.providers.GitHub.clientSecret");
    [Fact] void should_tell_two_open_id_providers_apart_by_their_name() => KeysOf("authentication.providers.Partner.").ShouldContainOnly("authentication.providers.Partner.clientId", "authentication.providers.Partner.clientSecret", "authentication.providers.Partner.authority");
    [Fact] void should_key_the_other_open_id_provider_by_its_own_name() => KeysOf("authentication.providers.Supplier.").Length.ShouldEqual(3);
    [Fact] void should_not_key_a_provider_by_its_kind_when_it_has_an_alias() => KeysOf("authentication.providers.OpenId.").ShouldBeEmpty();
    [Fact] void should_need_the_mail_settings_for_onboarding() => KeysOf("onboarding.").ShouldContainOnly("onboarding.email.sender", "onboarding.email.server.host", "onboarding.email.server.port", "onboarding.email.server.username", "onboarding.email.server.password");
    [Fact] void should_mark_secrets() => _requirements.Secrets.Select(_ => _.Key).ShouldContainOnly(
        "authentication.providers.EntraId.clientSecret",
        "authentication.providers.GitHub.clientSecret",
        "authentication.providers.Partner.clientSecret",
        "authentication.providers.Supplier.clientSecret",
        "onboarding.email.server.password");
    [Fact] void should_say_where_a_key_comes_from() => _requirements.Keys.Where(_ => _.Source == EnvironmentKeySource.Onboarding).Count().ShouldEqual(5);
}

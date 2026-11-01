// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Environments;

namespace Cratis.Screenplay.Semantics.for_SemanticModelCompiler;

public class when_compiling_a_model_that_needs_its_environment : Specification
{
    const string Source =
        """
        persona Owner

        authentication
          provider EntraId
          provider OpenId name Partner

        onboarding
          invite tenant as Owner
        """;

    CompilationResult<SemanticCompilation> _result;

    void Because()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("App"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("application"), "application", "application.play", Source);
        _result = new SemanticModelCompiler().Compile("App", SemanticDocumentSet.Create([document], catalog));
    }

    [Fact] void should_compile() => _result.Success.ShouldBeTrue();
    [Fact] void should_list_the_keys_without_parsing_the_source_again() => _result.Value!.Environment.Keys.Select(_ => _.Key).ShouldContain("authentication.providers.Partner.clientId");
    [Fact] void should_tell_two_providers_of_one_kind_apart_by_name() => _result.Value!.Environment.Keys.Select(_ => _.Key).ShouldNotContain("authentication.providers.OpenId.clientId");
    [Fact] void should_mark_the_secrets() => _result.Value!.Environment.Secrets.Select(_ => _.Key).ShouldContainOnly("authentication.providers.EntraId.clientSecret", "authentication.providers.Partner.clientSecret", "onboarding.email.server.password");
    [Fact] void should_leave_the_executable_model_alone() => _result.Value!.Environment.Keys.All(_ => _.Source is EnvironmentKeySource.AuthenticationProvider or EnvironmentKeySource.Onboarding).ShouldBeTrue();
}

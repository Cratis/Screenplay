// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_document_with_authentication : given.a_compiler
{
    const string Source =
        """
        authentication
          provider AzureAd
            type oidc
            authority "https://login.microsoftonline.com/common/v2.0"
            clientId $secrets.azureAdClientId
            clientSecret $secrets.azureAdClientSecret
          provider GitHub
            type oauth
            clientId $secrets.githubClientId
            clientSecret $secrets.githubClientSecret
        """;

    CompilationResult<ApplicationSyntax> _result;
    AuthenticationSyntax _authentication;

    void Because()
    {
        _result = _compiler.Compile(Source);
        _authentication = _result.Value!.Authentication!;
    }

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_parse_both_providers() => _authentication.Providers.Select(_ => _.Name).ShouldContainOnly("AzureAd", "GitHub");
    [Fact] void should_parse_the_settings_of_the_first_provider() => FirstProvider.Settings.Select(_ => _.Name).ShouldContainOnly("type", "authority", "clientId", "clientSecret");
    [Fact] void should_parse_the_type_as_a_path() => ((PathExpressionSyntax)Setting("type").Value).Path.ShouldEqual("oidc");
    [Fact] void should_parse_the_authority_as_a_literal() => ((LiteralExpressionSyntax)Setting("authority").Value).Value.ShouldEqual("https://login.microsoftonline.com/common/v2.0");
    [Fact] void should_keep_the_client_id_secret_symbolic() => Setting("clientId").Value.ShouldBeOfExactType<SecretExpressionSyntax>();
    [Fact] void should_parse_the_client_id_secret_name() => ((SecretExpressionSyntax)Setting("clientId").Value).Name.ShouldEqual("azureAdClientId");

    AuthenticationProviderSyntax FirstProvider => _authentication.Providers.First();

    AuthenticationSettingSyntax Setting(string name) => FirstProvider.Settings.Single(_ => _.Name == name);
}

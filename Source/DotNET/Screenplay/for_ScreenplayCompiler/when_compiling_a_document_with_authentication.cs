// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_document_with_authentication : given.a_compiler
{
    const string Source =
        """
        authentication
          provider EntraId
          provider GitHub
          provider OpenId name Partner
          provider OpenId name Supplier
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
    [Fact] void should_parse_every_provider() => _authentication.Providers.Count().ShouldEqual(4);
    [Fact] void should_keep_the_kind_of_each_provider() => _authentication.Providers.Select(_ => _.Name).ShouldContainOnly("EntraId", "GitHub", "OpenId", "OpenId");
    [Fact] void should_leave_an_unnamed_provider_without_an_alias() => _authentication.Providers.First().Alias.ShouldBeNull();
    [Fact] void should_identify_an_unnamed_provider_by_its_kind() => _authentication.Providers.First().Identity.ShouldEqual("EntraId");

    // Two OpenId providers are two different identity providers, and the name is what tells them apart.
    [Fact] void should_keep_both_generic_providers() => _authentication.Providers.Count(_ => _.Name == "OpenId").ShouldEqual(2);
    [Fact] void should_identify_a_named_provider_by_its_name() =>
        _authentication.Providers.Where(_ => _.Name == "OpenId").Select(_ => _.Identity).ShouldContainOnly("Partner", "Supplier");
}

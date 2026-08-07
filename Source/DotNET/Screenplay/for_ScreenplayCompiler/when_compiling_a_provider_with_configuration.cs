// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_provider_with_configuration : given.a_compiler
{
    const string Source =
        """
        authentication
          provider EntraId
            authority "https://login.microsoftonline.com/common/v2.0"
            clientId "the-client"
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    // The document says something the language deliberately does not express, and dropping it silently
    // would leave a reader believing the configuration is part of the declaration.
    [Fact] void should_not_succeed() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_every_configuration_line() => _result.Diagnostics.Count().ShouldEqual(2);
    [Fact] void should_report_them_as_configuration_that_does_not_belong() =>
        _result.Diagnostics.All(_ => _.Code == DiagnosticCodes.ProviderWithConfiguration).ShouldBeTrue();
    [Fact] void should_still_declare_the_provider() => _result.Value!.Authentication!.Providers.Single().Name.ShouldEqual("EntraId");
}

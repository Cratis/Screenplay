// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_specification_with_an_unnamed_error : given.a_compiler
{
    const string UnnamedSource =
        """
        specification WhenExchangingAndMagicLinkIsNotActive
          when ExchangeToken
          then error
        """;

    const string NamedSource =
        """
        specification WhenExchangingAndTokenIsMalformed
          when ExchangeToken
          then error "The token is malformed"
        """;

    CompilationResult<SpecificationSyntax> _unnamed;
    CompilationResult<SpecificationSyntax> _named;

    void Because()
    {
        _unnamed = _compiler.CompileSpecification(UnnamedSource);
        _named = _compiler.CompileSpecification(NamedSource);
    }

    [Fact] void should_succeed() => _unnamed.Success.ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _unnamed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_state_the_rejection() => _unnamed.Value!.ThenErrors.Count().ShouldEqual(1);
    [Fact] void should_not_name_a_reason() => _unnamed.Value!.ThenErrors.Single().Name.ShouldBeNull();
    [Fact] void should_still_name_a_reason_when_one_is_given() => _named.Value!.ThenErrors.Single().Name.ShouldEqual("The token is malformed");
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_specification_with_an_unnamed_error : given.a_compiler
{
    const string Source =
        """
        module Identity
          feature Tokens
            slice StateChange ExchangeToken
              command ExchangeToken
                token String

              specification WhenExchangingAndMagicLinkIsNotActive
                when ExchangeToken
                then error

              specification WhenExchangingAndTokenIsExpired
                when ExchangeToken
                then error "Token has expired"
                then error ""
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_leave_an_unnamed_reason_unnamed() => Errors("WhenExchangingAndMagicLinkIsNotActive").Single().Name.ShouldBeNull();
    [Fact] void should_keep_a_named_reason() => Errors("WhenExchangingAndTokenIsExpired").First().Name.ShouldEqual("Token has expired");
    [Fact] void should_keep_an_empty_reason_distinct_from_an_unnamed_one() => Errors("WhenExchangingAndTokenIsExpired").Last().Name.ShouldEqual(string.Empty);
    [Fact] void should_allow_both_forms_in_one_specification() => Errors("WhenExchangingAndTokenIsExpired").Count().ShouldEqual(2);

    IEnumerable<SpecificationErrorSyntax> Errors(string name) =>
        _result.Value!.Modules.Single().Features.Single().Slices.Single()
            .Specifications.Single(_ => _.Name == name).ThenErrors;
}

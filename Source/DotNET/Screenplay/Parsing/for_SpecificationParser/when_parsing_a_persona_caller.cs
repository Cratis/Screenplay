// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Parsing.for_SpecificationParser;

public class when_parsing_a_persona_caller : Specification
{
    CompilationResult<SpecificationSyntax> _result;

    void Because() => _result = new ScreenplayCompiler().CompileSpecification("specification Allowed\n  given caller as Accountant");

    [Fact] void should_parse_the_persona_form() => _result.Success.ShouldBeTrue();
    [Fact] void should_keep_the_persona_name() => _result.Value!.GivenCallerPersona!.Name.ShouldEqual("Accountant");
    [Fact] void should_accept_the_existing_unicode_identifier_alphabet() => new ScreenplayCompiler().CompileSpecification("specification X\n  given caller as Pärson").Value!.GivenCallerPersona!.Name.ShouldEqual("Pärson");
    [Fact] void should_not_look_like_an_unauthenticated_explicit_caller() => _result.Value!.GivenCaller.ShouldBeNull();
    [Fact] void should_reject_a_body() => Error("given caller as Accountant\n    role \"Other\"").ShouldEqual(DiagnosticCodes.InvalidSpecificationCallerPersona);
    [Fact] void should_reject_a_malformed_name() => Error("given caller as").ShouldEqual(DiagnosticCodes.InvalidSpecificationCallerPersona);
    [Fact] void should_keep_the_zero_or_one_caller_rule() => Error("given caller as Accountant\n  given caller").ShouldEqual(DiagnosticCodes.DuplicateSpecificationCallerOrDenied);

    static string Error(string body) => new ScreenplayCompiler().CompileSpecification($"specification Invalid\n  {body}").Diagnostics.Single().Code;
}

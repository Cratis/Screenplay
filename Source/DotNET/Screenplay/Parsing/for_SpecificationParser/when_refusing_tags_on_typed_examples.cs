// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_SpecificationParser;

public class when_refusing_tags_on_typed_examples : Specification
{
    const string Source = "module M\n  feature F\n    slice StateChange S\n      event E\n        @tag String\n      example Tagged : E\n        ";
    string[] _tagCodes;
    string[] _noTagsCodes;
    CompilationResult<ApplicationSyntax> _payload;

    void Because()
    {
        var compiler = new ScreenplayCompiler();
        _tagCodes = compiler.Parse(Source + "tag audit").Diagnostics.Select(diagnostic => diagnostic.Code).ToArray();
        _noTagsCodes = compiler.Parse(Source + "no tags").Diagnostics.Select(diagnostic => diagnostic.Code).ToArray();
        _payload = compiler.Parse(Source + "tag = \"payload\"");
    }

    [Fact] void should_refuse_tag_with_the_tag_expectation_diagnostic() => _tagCodes.ShouldEqual(["PLAY1144"]);
    [Fact] void should_refuse_no_tags_with_the_tag_expectation_diagnostic() => _noTagsCodes.ShouldEqual(["PLAY1144"]);
    [Fact] void should_still_accept_a_payload_property_named_tag() => _payload.Diagnostics.ShouldBeEmpty();
    [Fact] void should_keep_the_payload_mapping() => _payload.Value!.Modules.Single().Features.Single().Slices.Single().Examples.Single().Values.Single().Property.ShouldEqual("tag");
}

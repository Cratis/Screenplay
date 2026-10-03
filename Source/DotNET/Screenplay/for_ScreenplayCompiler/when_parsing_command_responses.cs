// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_parsing_command_responses : given.a_compiler
{
    const string Prefix = "concept Id : Uuid\nconcept lowerCaseConcept : String\nconcept slug : String\nmodule M\n  feature F\n    slice StateChange S\n";

    [Theory]
    [InlineData("returns String", false)]
    [InlineData("returns lowerCaseConcept", false)]
    [InlineData("@returns String", false)]
    [InlineData("returns slug\n        slug slug", true)]
    [InlineData("slug slug\n        returns slug", true)]
    [InlineData("@returns String\n        returns returns", true)]
    [InlineData("returns @slug\n        slug slug", true)]
    void should_disambiguate_using_only_properties_in_the_same_command(string body, bool response)
    {
        var parsed = _compiler.Parse(Prefix + "      command C\n        " + body);
        parsed.Success.ShouldBeTrue();
        var command = Command(parsed.Value!);
        (command.Response is not null).ShouldEqual(response);
        var printed = new ScreenplayPrinter().Print(parsed.Value!);
        SyntaxJson.StructurallyEqual(parsed.Value!, _compiler.Parse(printed).Value!).ShouldBeTrue();
        if (command.Properties.Any(property => property.Name == "returns")) printed.ShouldContain("@returns");
    }

    [Fact]
    void should_keep_generated_contextual() => Command(_compiler.Parse(Prefix + "      command C\n        generated String").Value!).Properties.Single().IsGenerated.ShouldBeFalse();

    [Fact]
    void should_round_trip_record_responses_and_fixtures()
    {
        const string Source = Prefix + """
              command C
                id Id generated identifier
                receipt Id generated
                note String optional
                returns
                  receipt Id = receipt
                  note = note
              specification Accepts
                when C
                  for "11111111-1111-1111-1111-111111111111"
                  generated receipt = "22222222-2222-2222-2222-222222222222"
                  generated = "ordinary input"
                then returns
                  receipt = "22222222-2222-2222-2222-222222222222"
                  note = null
        """;
        var parsed = _compiler.Parse(Source);
        parsed.Success.ShouldBeTrue();
        var command = Command(parsed.Value!);
        command.Properties.Count(property => property.IsGenerated).ShouldEqual(2);
        var specification = parsed.Value!.Modules.Single().Features.Single().Slices.Single().Specifications.Single();
        specification.When!.GeneratedValues.Count().ShouldEqual(1);
        specification.When.Values.Single().Property.ShouldEqual("generated");
        specification.ThenReturns.ShouldBeOfExactType<RecordSpecificationReturnSyntax>();
        var decoded = (ApplicationSyntax)SyntaxJson.Deserialize(SyntaxJson.Serialize(parsed.Value!));
        SyntaxJson.StructurallyEqual(parsed.Value!, decoded).ShouldBeTrue();
        var printed = new ScreenplayPrinter().Print(decoded);
        SyntaxJson.StructurallyEqual(decoded, _compiler.Parse(printed).Value!).ShouldBeTrue();
    }

    [Fact]
    void should_preserve_a_one_field_record() => Command(_compiler.Parse(Prefix + "      command C\n        name String\n        returns\n          value = name").Value!).Response.ShouldBeOfExactType<RecordCommandResponseSyntax>();

    [Theory]
    [InlineData("generated generated")]
    [InlineData("identifier generated")]
    [InlineData("generated optional")]
    [InlineData("generated identifier generated")]
    [InlineData("optional optional generated")]
    [InlineData("optional generated optional")]
    [InlineData("identifier identifier generated")]
    void should_reject_repeated_or_reversed_generated_modifiers(string modifiers) => _compiler.Parse(Prefix + "      command C\n        id Id " + modifiers).Diagnostics.Any(diagnostic => diagnostic.Code == "PLAY0484").ShouldBeTrue();

    [Theory]
    [InlineData("\"one\" \"two\"")]
    [InlineData("id")]
    [InlineData("{\"id\": 1} trailing")]
    [InlineData("[1] [2]")]
    void should_reject_nonconcrete_or_partially_consumed_return_values(string value) => _compiler.Parse(Prefix + "      specification Invalid\n        when C\n        then returns " + value).Success.ShouldBeFalse();

    static CommandSyntax Command(ApplicationSyntax application) => application.Modules.Single().Features.Single().Slices.Single().Commands.Single();
}

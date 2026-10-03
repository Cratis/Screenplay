// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Files;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_validating_command_responses : given.a_compiler
{
    const string Prefix = "concept Id : Uuid\nconcept Status : Enum\n  accepted\ntype Payload\n  names String[]\n  note String optional\nmodule M\n  feature F\n    slice StateChange S\n";

    [Theory]
    [InlineData("Uuid generated")]
    [InlineData("String generated")]
    [InlineData("Id optional generated")]
    [InlineData("Id[] generated")]
    [InlineData("Payload generated")]
    void should_require_generated_values_to_be_required_uuid_concepts(string type) =>
        _compiler.Compile(Prefix + "      command C\n        value " + type).Diagnostics.Any(diagnostic => diagnostic.Code == "PLAY0483").ShouldBeTrue();

    [Theory]
    [InlineData("returns", "PLAY0486")]
    [InlineData("name String\n        returns name\n        returns name", "PLAY0486")]
    [InlineData("returns when accepted", "PLAY0486")]
    [InlineData("returns @unknown", "PLAY0487")]
    [InlineData("name String\n        name String\n        returns @name", "PLAY0487")]
    [InlineData("name String\n        returns\n          value = name\n          value = name", "PLAY0488")]
    [InlineData("id Id\n        returns\n          value Uuid = id", "PLAY0489")]
    [InlineData("name String[]\n        returns name", "PLAY0489")]
    [InlineData("returns id\n          nested String\n        id Id", "PLAY0486")]
    [InlineData("returns @id\n          nested String\n        id Id", "PLAY0486")]
    [InlineData("name String\n        returns\n          value = name\n            nested String", "PLAY0486")]
    void should_diagnose_invalid_response_contracts(string body, string code) =>
        _compiler.Compile(Prefix + "      command C\n        " + body).Diagnostics.Any(diagnostic => diagnostic.Code == code).ShouldBeTrue();

    [Theory]
    [InlineData("String", "\"text\"", true)]
    [InlineData("String", "42", false)]
    [InlineData("Bool", "true", true)]
    [InlineData("Bool", "\"true\"", false)]
    [InlineData("Int", "42", true)]
    [InlineData("Int", "1.5", false)]
    [InlineData("Decimal", "1.5", true)]
    [InlineData("Decimal", "\"1.5\"", false)]
    [InlineData("Date", "\"2026-10-02\"", true)]
    [InlineData("Date", "\"invalid\"", false)]
    [InlineData("Date", "\"2024-02-29\"", true)]
    [InlineData("Date", "\"2023-02-29\"", false)]
    [InlineData("Date", "\"0000-01-01\"", false)]
    [InlineData("Date", "\"2026-13-01\"", false)]
    [InlineData("Date", "\"2026-10-02T12:00:00Z\"", false)]
    [InlineData("DateTime", "\"2026-10-02T12:00:00Z\"", true)]
    [InlineData("DateTime", "\"invalid\"", false)]
    [InlineData("DateTime", "\"2026-10-02T12:00:00+14:00\"", true)]
    [InlineData("DateTime", "\"2026-10-02T12:00:00+14:01\"", false)]
    [InlineData("DateTime", "\"2026-10-02T12:00:00+15:00\"", false)]
    [InlineData("DateTime", "\"2026-10-02T12:00:00+01:60\"", false)]
    [InlineData("Id", "\"11111111-1111-1111-1111-111111111111\"", true)]
    [InlineData("Id", "\"invalid\"", false)]
    [InlineData("Id", "\"11111111111111111111111111111111\"", true)]
    [InlineData("Id", "\"{11111111-1111-1111-1111-111111111111}\"", true)]
    [InlineData("Id", "\"(11111111-1111-1111-1111-111111111111)\"", true)]
    [InlineData("Id", "\"{0x11111111,0x1111,0x1111,{0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11}}\"", false)]
    [InlineData("String optional", "null", true)]
    [InlineData("String", "null", false)]
    [InlineData("Status", "\"accepted\"", true)]
    [InlineData("Status", "\"other\"", false)]
    [InlineData("Payload", "{\"names\": [\"one\", \"two\"], \"note\": null}", true)]
    [InlineData("Payload", "{\"names\": [false]}", false)]
    [InlineData("Payload", "{\"names\": \"one\"}", false)]
    [InlineData("Payload", "{\"unknown\": true}", false)]
    [InlineData("Payload", "\"not an object\"", false)]
    void should_check_concrete_return_values_against_known_types(string type, string value, bool valid) =>
        _compiler.Compile(Prefix + $"      command C\n        result {type}\n        returns result\n      specification Accepts\n        when C\n        then returns {value}")
            .Diagnostics.Any(diagnostic => diagnostic.Code == "PLAY0491").ShouldEqual(!valid);

    [Theory]
    [InlineData("generated unknown = \"11111111-1111-1111-1111-111111111111\"")]
    [InlineData("generated id = \"11111111-1111-1111-1111-111111111111\"")]
    [InlineData("generated receipt = \"invalid\"")]
    [InlineData("generated receipt = name")]
    [InlineData("generated receipt = \"one\" \"two\"")]
    [InlineData("generated receipt = \"11111111-1111-1111-1111-111111111111\"\n          generated receipt = \"11111111-1111-1111-1111-111111111111\"")]
    void should_reject_invalid_generated_fixtures(string fixture) =>
        _compiler.Compile(Prefix + "      command C\n        id Id generated identifier\n        receipt Id generated\n      specification Invalid\n        when C\n          " + fixture)
            .Diagnostics.Any(diagnostic => diagnostic.Code == "PLAY0490").ShouldBeTrue();

    [Theory]
    [InlineData("then error")]
    [InlineData("then denied")]
    void should_reject_returns_on_unsuccessful_outcomes(string outcome) =>
        _compiler.Compile(Prefix + "      command C\n        name String\n        returns name\n      specification Invalid\n        when C\n        then returns \"text\"\n        " + outcome)
            .Diagnostics.Any(diagnostic => diagnostic.Code == "PLAY0491").ShouldBeTrue();

    [Fact]
    void should_reject_generated_inputs_through_specifications_forms_executions_and_invocations()
    {
        var result = _compiler.Compile(Prefix + "      command C\n        receipt Id generated\n      specification Invalid\n        when C\n          receipt = \"11111111-1111-1111-1111-111111111111\"\n      screen Entry\n        on submit\n          execute C\n            with receipt from $form.receipt\n      reaction R\n        when Recorded\n          invokes C\n            receipt = \"ignored\"\n  form Entry for C\n    field receipt");
        result.Diagnostics.Count(diagnostic => diagnostic.Code == "PLAY0485").ShouldEqual(4);
    }

    [Fact]
    void should_preserve_per_command_interpretation_in_reordered_imported_documents()
    {
        var (_, result) = PlayApplicationAssembly.Compile(_compiler, ["application.play"], new InMemoryPlayDocumentSource(new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["application.play"] = "import \"commands.play\"\nimport \"types.play\"",
            ["commands.play"] = "module M\n  feature F\n    slice StateChange S\n      command C\n        returns lowerCaseConcept\n      command D\n        returns slug\n        slug slug",
            ["types.play"] = "concept lowerCaseConcept : String\nconcept slug : String"
        }));
        result.Success.ShouldBeTrue();
        var commands = result.Value!.Modules.Single().Features.Single().Slices.Single().Commands.ToArray();
        commands[0].Response.ShouldBeNull();
        commands[0].Properties.Single().Name.ShouldEqual("returns");
        commands[1].Response.ShouldBeOfExactType<ScalarCommandResponseSyntax>();
    }

    [Fact]
    void should_preserve_comments_on_responses_and_generated_fixtures()
    {
        var parsed = _compiler.Parse(Prefix + "      command C\n        receipt Id generated // allocation\n        // response contract\n        returns\n          value = receipt // source\n          // closing response\n      specification Accepts\n        when C\n          // fixture\n          generated receipt = \"11111111-1111-1111-1111-111111111111\"\n        // expectation\n        then returns\n          value = \"11111111-1111-1111-1111-111111111111\" // expected\n          // closing expectation");
        var printed = new ScreenplayPrinter().Print(parsed.Value!);
        foreach (var comment in new[] { "allocation", "response contract", "source", "fixture", "expectation", "expected" }) printed.ShouldContain("// " + comment);
        printed.ShouldContain("value = receipt // source\n          // closing response");
        printed.ShouldContain("value = \"11111111-1111-1111-1111-111111111111\" // expected\n          // closing expectation");
        SyntaxJson.StructurallyEqual(parsed.Value!, _compiler.Parse(printed).Value!).ShouldBeTrue();
    }

    [Fact]
    void should_default_missing_additive_json_members_and_reject_unknown_members()
    {
        var property = (PropertySyntax)SyntaxJson.Deserialize(System.Text.Json.JsonSerializer.SerializeToElement(new { kind = "PropertySyntax", name = "name", type = new { kind = "TypeRefSyntax", name = "String", isCollection = false, isOptional = false } }));
        property.IsGenerated.ShouldBeFalse();
        var command = (CommandSyntax)SyntaxJson.Deserialize(System.Text.Json.JsonSerializer.SerializeToElement(new { kind = "CommandSyntax", name = "C" }));
        command.Response.ShouldBeNull();
        var action = (SpecificationCommandSyntax)SyntaxJson.Deserialize(System.Text.Json.JsonSerializer.SerializeToElement(new { kind = "SpecificationCommandSyntax", commandType = "C" }));
        action.GeneratedValues.ShouldBeEmpty();
        var specification = (SpecificationSyntax)SyntaxJson.Deserialize(System.Text.Json.JsonSerializer.SerializeToElement(new { kind = "SpecificationSyntax", name = "Accepts", thenEventsInAnyOrder = false }));
        specification.ThenReturns.ShouldBeNull();
        Catch.Exception(() => SyntaxJson.Deserialize(System.Text.Json.JsonSerializer.SerializeToElement(new { kind = "RecordCommandResponseSyntax", fields = Array.Empty<object>(), unexpected = true }))).ShouldBeOfExactType<InvalidSyntaxJson>();
        SyntaxSchema.For("CommandSyntax").GetProperty("properties").GetProperty("response").GetProperty("anyOf")[0].GetProperty("oneOf").GetArrayLength().ShouldEqual(2);
        SyntaxSchema.For("RecordCommandResponseSyntax").GetProperty("additionalProperties").GetBoolean().ShouldBeFalse();
    }
}

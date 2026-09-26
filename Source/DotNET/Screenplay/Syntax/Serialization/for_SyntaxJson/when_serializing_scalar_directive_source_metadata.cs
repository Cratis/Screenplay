// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Syntax.Serialization.for_SyntaxJson;

public class when_serializing_scalar_directive_source_metadata : Specification
{
    readonly UiProfileSyntax _profile = new("Desktop", ["web"], "expanded", ["core"], SourceLocation.Start, "Aurora", "Main")
    {
        DirectiveLocations = new Dictionary<string, SourceLocation> { ["target platform"] = new(2, 3), ["package:0"] = new(5, 5) }
    };

    [Fact] void should_preserve_the_profile_json_contract() => SyntaxJson.Serialize(_profile).GetRawText().ShouldEqual(
        "{\"kind\":\"UiProfileSyntax\",\"defaultSizeClass\":\"expanded\",\"layout\":\"Main\",\"name\":\"Desktop\",\"packages\":[\"core\"],\"platforms\":[\"web\"],\"theme\":\"Aurora\"}");

    [Fact] void should_preserve_the_theme_json_contract() => SyntaxJson.Serialize(new ThemeSyntax("Aurora", ["core"], SourceLocation.Start)
    {
        DirectiveLocations = new Dictionary<string, SourceLocation> { ["compatible:0"] = new(2, 3) }
    }).GetRawText().ShouldEqual("{\"kind\":\"ThemeSyntax\",\"compatibleWith\":[\"core\"],\"name\":\"Aurora\"}");

    [Fact] void should_preserve_the_persona_json_contract() => SyntaxJson.Serialize(new PersonaSyntax("Clerk", null, ["Member"], SourceLocation.Start)
    {
        DirectiveLocations = new Dictionary<string, SourceLocation> { ["policy:0"] = new(2, 3) }
    }).GetRawText().ShouldEqual("{\"kind\":\"PersonaSyntax\",\"description\":null,\"name\":\"Clerk\",\"policies\":[\"Member\"]}");

    [Fact] void should_preserve_the_concept_json_contract() => SyntaxJson.Serialize(new ConceptSyntax("State", "Enum", [], ["active"], SourceLocation.Start)
    {
        DirectiveLocations = new Dictionary<string, SourceLocation> { ["value:0"] = new(2, 3) }
    }).GetRawText().ShouldEqual("{\"kind\":\"ConceptSyntax\",\"attributes\":[],\"file\":null,\"name\":\"State\",\"type\":\"Enum\",\"validations\":[],\"values\":[\"active\"]}");

    [Fact] void should_preserve_the_contribution_json_contract() => SyntaxJson.Serialize(new ContributionSyntax("Navigation", null, "Shop", 1, SourceLocation.Start)
    {
        DirectiveLocations = new Dictionary<string, SourceLocation> { ["label"] = new(2, 3) }
    }).GetRawText().ShouldEqual("{\"kind\":\"ContributionSyntax\",\"contributionPoint\":\"Navigation\",\"label\":\"Shop\",\"navigate\":null,\"order\":1}");

    [Fact] void should_preserve_the_action_json_contract() => SyntaxJson.Serialize(new ScreenActionSyntax("Open", "Open", null, SourceLocation.Start)
    {
        DirectiveLocations = new Dictionary<string, SourceLocation> { ["label"] = new(2, 3) }
    }).GetRawText().ShouldEqual("{\"kind\":\"ScreenActionSyntax\",\"command\":\"Open\",\"label\":\"Open\",\"navigate\":null}");

    [Fact] void should_not_advertise_directive_locations_in_the_schema() => SyntaxSchema.For(nameof(UiProfileSyntax)).GetProperty("properties").TryGetProperty("directiveLocations", out _).ShouldBeFalse();

    [Fact] void should_compare_the_same_as_a_profile_without_authored_positions() => SyntaxJson.StructurallyEqual(_profile, _profile with { DirectiveLocations = new Dictionary<string, SourceLocation>() }).ShouldBeTrue();

    [Fact] void should_still_compare_value_changes() => SyntaxJson.StructurallyEqual(_profile, _profile with { Theme = "Other" }).ShouldBeFalse();

    [Fact]
    void should_keep_the_specification_subdirective_json_contract()
    {
        var caller = new SpecificationCallerSyntax(true, ["Clerk"], [], SourceLocation.Start)
        {
            DirectiveLocations = new Dictionary<string, SourceLocation> { ["authenticated"] = new(2, 5), ["role:0"] = new(3, 5) }
        };
        AssertSourceMetadataDoesNotChangeTypedJson(caller);
    }

    [Fact]
    void should_keep_the_capture_subdirective_json_contract()
    {
        var capture = new ScreenplayCompiler().CompileCapture("capture Import\n  map\n    split name by \",\"\n      first\n").Value!;
        AssertSourceMetadataDoesNotChangeTypedJson(capture with
        {
            DirectiveLocations = new Dictionary<string, SourceLocation> { ["map"] = new(2, 3) }
        });
        AssertSourceMetadataDoesNotChangeTypedJson(capture.Map.Single());
    }

    [Fact]
    void should_keep_the_projection_subdirective_json_contract()
    {
        var projection = new ScreenplayCompiler().CompileProjection("projection Orders\n  automap\n  every\n    exclude children\n").Value!;
        AssertSourceMetadataDoesNotChangeTypedJson(projection);
        AssertSourceMetadataDoesNotChangeTypedJson(projection.Blocks.Single());
    }

    [Fact]
    void should_keep_the_command_subdirective_json_contract()
    {
        var command = new ScreenplayCompiler().Compile("module Shop\n  feature Orders\n    slice StateChange Order\n      command Place\n        validate\n          require id == 1\n            severity warning\n            message \"Denied\"\n        produces when id == 1\n          OrderPlaced\n").Value!
            .Modules.Single().Features.Single().Slices.Single().Commands.Single();
        AssertSourceMetadataDoesNotChangeTypedJson(command.Validations.Single());
        AssertSourceMetadataDoesNotChangeTypedJson(((DeclarativeValidateSyntax)command.Validations.Single()).Requirements!.Single());
        AssertSourceMetadataDoesNotChangeTypedJson(command.Produces.Single());
    }

    static void AssertSourceMetadataDoesNotChangeTypedJson(SyntaxNode node)
    {
        var relocated = node with { DirectiveLocations = new Dictionary<string, SourceLocation> { ["other"] = new(100, 4) } };
        SyntaxJson.Serialize(node).GetRawText().ShouldEqual(SyntaxJson.Serialize(relocated).GetRawText());
        SyntaxJson.StructurallyEqual(node, relocated).ShouldBeTrue();
        SyntaxSchema.For(node.GetType().Name).GetProperty("properties").TryGetProperty("directiveLocations", out _).ShouldBeFalse();
    }

    [Fact]
    void should_keep_the_existing_typed_contract_for_sublanguage_directives()
    {
        var compiler = new ScreenplayCompiler();
        var capture = compiler.CompileCapture("capture Legacy\n  key id\n  append OrderCaptured\n").Value!;
        var projection = compiler.CompileProjection("projection Orders\n  sequence orders\n  from OrderPlaced\n").Value!;
        var specification = compiler.CompileSpecification("specification Appending\n  when append OrderPlaced\n  then events in any order\n  then OrderPlaced\n").Value!;
        var concurrency = compiler.Compile("module Shop\n  feature Orders\n    slice StateChange Place\n      command Place\n        concurrency\n          eventSource\n").Value!
            .Modules.Single().Features.Single().Slices.Single().Commands.Single().Concurrency!;
        foreach (var (node, key) in new (SyntaxNode, string)[]
        {
            (capture, "key"), (projection, "sequence"), (specification, "then events in any order"), (concurrency, "eventSource")
        })
        {
            var relocated = node with { DirectiveLocations = new Dictionary<string, SourceLocation> { [key] = new(100, 4) } };
            SyntaxJson.Serialize(relocated).GetRawText().ShouldEqual(SyntaxJson.Serialize(node).GetRawText());
            SyntaxJson.StructurallyEqual(node, relocated).ShouldBeTrue();
            SyntaxSchema.For(node.GetType().Name).GetProperty("properties").TryGetProperty("directiveLocations", out _).ShouldBeFalse();
        }
    }
}

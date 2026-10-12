// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Semantics.for_ExecutableSemanticModel;

public class when_validating_concept_compliance : Specification
{
    static readonly SemanticConcept _personal = new(Id(2), "Protected", SemanticPrimitiveType.Text, [], []) { Compliance = new(new(), null) };

    [Fact] void should_refuse_an_empty_classification() => Refuse(_personal with { Compliance = new(null, null) });
    [Fact] void should_refuse_an_enum_classification() => Refuse(_personal with { Values = ["one", "two"] });
    [Fact] void should_refuse_an_unknown_special_category() => Refuse(_personal with { Compliance = new(new("unknown"), null) });
    [Fact] void should_refuse_a_secret_scope_next_to_personal() => Refuse(_personal with { Compliance = new(new(), new("subject")) });
    [Fact] void should_refuse_an_unknown_secret_scope() => Refuse(_personal with { Compliance = new(null, new("unknown")) });
    [Fact] void should_refuse_v13_without_markers() => Refuse(_personal with { Compliance = null });

    [Theory]
    [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)] [InlineData(5)] [InlineData(6)]
    [InlineData(7)] [InlineData(8)] [InlineData(9)] [InlineData(10)] [InlineData(11)] [InlineData(12)]
    void should_refuse_compliance_in_older_models(uint version)
    {
        Catch.Exception(() => ExecutableSemanticModel.Create(new(version, 0), new(version, 0), Application(_personal))).ShouldBeOfExactType<InvalidSemanticContract>();
        var json = Json().Replace("\"schemaVersion\":13", $"\"schemaVersion\":{version}", StringComparison.Ordinal).Replace("\"13.0\"", $"\"{version}.0\"", StringComparison.Ordinal);
        ReadRefusal(json, "compliance");
    }

    [Theory]
    [InlineData("{\"personal\":{\"special\":\"unknown\"}}", "special category")]
    [InlineData("{\"personal\":{},\"secret\":{\"scope\":\"subject\"}}", "scope next to personal")]
    [InlineData("{\"secret\":{\"scope\":\"unknown\"}}", "secret scope")]
    [InlineData("{}", "compliance")]
    [InlineData("{\"personal\":{\"criminal\":false}}", "criminal")]
    [InlineData("{\"personal\":{\"reason\":\"note\"}}", "reason")]
    void should_refuse_invalid_compliance_json(string compliance, string message) => ReadRefusal(Json().Replace("{\"personal\":{}}", compliance, StringComparison.Ordinal), message);

    [Fact]
    void should_refuse_json_compliance_on_an_enum() => ReadRefusal(Json().Replace("\"values\":[]", "\"values\":[\"one\",\"two\"]", StringComparison.Ordinal), "Enum concept");

    [Theory]
    [InlineData("source")] [InlineData("scalar")] [InlineData("composite")]
    void should_refuse_compliance_on_event_source_and_stream_identities(string role)
    {
        var application = IdentityApplication(role);
        Catch.Exception(() => Create(application)).ShouldBeOfExactType<InvalidSemanticContract>();
        var unmarked = application with { Concepts = [_personal with { Compliance = null }] };
        var bytes = SemanticModelSerializer.Serialize(ExecutableSemanticModel.Create(LanguageVersion.V8, SemanticVersion.V8, unmarked));
        var json = Encoding.UTF8.GetString(bytes).Replace("\"schemaVersion\":8", "\"schemaVersion\":13", StringComparison.Ordinal).Replace("\"8.0\"", "\"13.0\"", StringComparison.Ordinal).Replace("\"validations\":[]", "\"validations\":[],\"compliance\":{\"personal\":{}}", StringComparison.Ordinal);
        ReadRefusal(json, "identity");
    }

    [Theory]
    [InlineData("racialOrEthnicOrigin")] [InlineData("politicalOpinions")] [InlineData("religiousOrPhilosophicalBeliefs")]
    [InlineData("tradeUnionMembership")] [InlineData("genetic")] [InlineData("biometric")]
    [InlineData("health")] [InlineData("sexLifeOrSexualOrientation")]
    void should_round_trip_every_special_category(string category)
    {
        var model = Create(Application(_personal with { Compliance = new(new(category, true), null) }));
        SemanticModelSerializer.Deserialize(SemanticModelSerializer.Serialize(model)).Revision.ShouldEqual(model.Revision);
    }

    [Theory]
    [InlineData("root", false)] [InlineData("children", false)] [InlineData("nested", false)]
    [InlineData("root", true)] [InlineData("children", true)] [InlineData("nested", true)]
    void should_refuse_compliance_on_capture_append_identities(string level, bool secret)
    {
        var application = CaptureIdentityApplication(level);
        var compliance = secret ? new SemanticConceptCompliance(null, new()) : _personal.Compliance;
        application = application with { Concepts = [_personal with { Compliance = compliance }] };
        var error = Catch.Exception(() => Create(application));
        error.ShouldBeOfExactType<InvalidSemanticContract>();
        error.Message.ShouldContain("identity");
        var unmarked = application with { Concepts = [_personal with { Compliance = null }] };
        var bytes = SemanticModelSerializer.Serialize(ExecutableSemanticModel.Create(LanguageVersion.V6, SemanticVersion.V6, unmarked));
        var json = Encoding.UTF8.GetString(bytes).Replace("\"schemaVersion\":6", "\"schemaVersion\":13", StringComparison.Ordinal).Replace("\"6.0\"", "\"13.0\"", StringComparison.Ordinal)
            .Replace("\"validations\":[]", $"\"validations\":[],\"compliance\":{{\"{(secret ? "secret" : "personal")}\":{{}}}}", StringComparison.Ordinal);
        ReadRefusal(json, "identity");
    }

    static SemanticApplication CaptureIdentityApplication(string level)
    {
        var contract = new SemanticEventContract(Id(6), EventContractId.CreateLegacy(ApplicationIdentity.Create("Compliance"), "Imported"), EventContractRevision.Initial, "Imported", []);
        var append = new SemanticCaptureAppend(contract.Id, SemanticTypeReference.ForConcept(_personal.Id), null, []);
        var capture = new SemanticCapture(Id(7), "Import", "id", [], level == "root" ? [append] : [])
        {
            Children = level == "children" ? [new("items", "id", [], [append])] : [],
            Nested = level == "nested" ? [new("item", [], [append])] : []
        };
        var slice = new SemanticSlice(Id(5), "Import", SemanticSliceKind.Translate, [contract], [], [], [], [], []) { Captures = [capture] };

        return Application(_personal) with { Modules = [new(Id(3), "External", [new(Id(4), "Import", [], [slice])])] };
    }

    static SemanticApplication IdentityApplication(string role)
    {
        var type = SemanticTypeReference.ForConcept(_personal.Id);
        var stream = new SemanticEventStream(Id(4), "Details", "Details")
        {
            StreamIdType = role == "scalar" ? type : null,
            StreamIdParts = role == "composite" ? [new("protected", type), new("other", SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text))] : []
        };

        return Application(_personal) with
        {
            EventSources = [new(Id(3), "Records", "Records", role == "source" ? [] : [stream]) { IdentifierType = role == "source" ? type : null }]
        };
    }

    static void ReadRefusal(string json, string message)
    {
        var error = Catch.Exception(() => SemanticModelSerializer.Deserialize(Encoding.UTF8.GetBytes(json)));
        error.ShouldBeOfExactType<InvalidSemanticContract>();
        error.Message.ShouldContain(message);
        error.Message.ShouldNotContain("computed revision");
    }

    static void Refuse(SemanticConcept concept) => Catch.Exception(() => Create(Application(concept))).ShouldBeOfExactType<InvalidSemanticContract>();
    static string Json() => Encoding.UTF8.GetString(SemanticModelSerializer.Serialize(Create(Application(_personal))));
    static ExecutableSemanticModel Create(SemanticApplication application) => ExecutableSemanticModel.Create(LanguageVersion.V13, SemanticVersion.V13, application);
    static SemanticApplication Application(SemanticConcept concept) => new(Id(1), "Compliance", [concept], [], []);
    static SemanticId Id(int value) => SemanticId.Parse($"sem1:{value:x64}");
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_round_tripping_the_v37_golden : Specification
{
    byte[] _bytes;
    string _text;
    ExecutableSemanticModel _model;

    void Because()
    {
        golden_vector_regeneration.RegenerateWhenRequested();
        _model = canonical_serialization_golden_vectors.CreateSemanticModelV37();
        _bytes = SemanticModelSerializer.Serialize(_model);
        _text = Encoding.UTF8.GetString(_bytes);
    }

    [Fact] void should_pin_the_new_golden() => _bytes.ShouldEqual(canonical_serialization_golden_vectors.EsmV37Bytes);
    [Fact] void should_round_trip_strictly() => SemanticModelSerializer.Serialize(SemanticModelSerializer.Deserialize(_bytes)).ShouldEqual(_bytes);
    [Fact] void should_select_v37() => _model.SemanticVersion.ShouldEqual(SemanticVersion.V37);
    [Fact] void should_carry_a_revision() => _model.Revision.ShouldNotBeNull();
    [Fact] void should_leave_earlier_readers_unextended() => EsmSchemaV36Support.Supports(LanguageVersion.V37, SemanticVersion.V37).ShouldBeFalse();
    [Fact] void should_write_the_configurable_application_zone() => _text.ShouldContain("\"timeZone\":{\"id\":\"Europe/Oslo\",\"configurable\":true}");
    [Fact] void should_write_the_declared_context_member() => _text.ShouldContain("\"contextMembers\":[{\"name\":\"billingZone\",\"type\":\"timeZone\"}]");
    [Fact] void should_write_a_fixed_command_calendar() => _text.ShouldContain("\"calendar\":{\"timeZone\":\"America/St_Johns\"}");
    [Fact] void should_write_the_context_calendar() => _text.ShouldContain("\"calendar\":{\"contextMember\":\"timeZone\"}");
    [Fact] void should_write_the_declared_member_calendar() => _text.ShouldContain("\"calendar\":{\"contextMember\":\"billingZone\"}");
    [Fact] void should_write_a_day_offset() => _text.ShouldContain("\"clock\":{\"kind\":\"today\",\"offset\":{\"amount\":-7,\"unit\":\"day\"}}");
    [Fact] void should_write_an_hour_offset() => _text.ShouldContain("\"offset\":{\"amount\":2,\"unit\":\"hour\"}");
    [Fact] void should_write_a_minute_offset() => _text.ShouldContain("\"offset\":{\"amount\":-30,\"unit\":\"minute\"}");
    [Fact] void should_write_date_of_a_property() => _text.ShouldContain("\"dateOf\":true");
    [Fact] void should_write_the_given_time_zone() => _text.ShouldContain("\"givenTimeZone\":\"Europe/Oslo\"");

    [Fact]
    void should_refuse_the_clock_below_v37()
    {
        var error = Catch.Exception(() => SemanticModelCanonicalJson.SerializeWithoutRevision(LanguageVersion.V36, SemanticVersion.V36, _model.Application));
        error.ShouldBeOfExactType<InvalidSemanticContract>();
    }

    [Fact]
    void should_refuse_the_shape_in_every_earlier_reader()
    {
        foreach (var version in EsmSchemaV36Support.SemanticVersions)
        {
            var json = _text.Replace("\"schemaVersion\":37", $"\"schemaVersion\":{version.Major}", StringComparison.Ordinal).Replace("\"37.0\"", $"\"{version}\"", StringComparison.Ordinal);
            var error = Catch.Exception(() => SemanticModelSerializer.Deserialize(Encoding.UTF8.GetBytes(json)));
            error.ShouldBeOfExactType<InvalidSemanticContract>();
            error.Message.Contains("revision", StringComparison.OrdinalIgnoreCase).ShouldBeFalse();
        }
    }
}

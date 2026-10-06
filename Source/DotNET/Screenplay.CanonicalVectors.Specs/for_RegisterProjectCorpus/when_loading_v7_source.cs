// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_RegisterProjectCorpus;

public class when_loading_v7_source : given.a_v7_corpus
{
    [Fact] void should_pin_the_name() => Corpus.Name.ShouldEqual("register-project/v7");
    [Fact] void should_pin_the_revision() => Corpus.SemanticRevision.ToString().ShouldEqual("rev1:502c66ea7ce0deaebfa155915fb9e21266e9f732a8b81bac66700e04a7c151c6");

    [Fact]
    void should_compile_every_form_to_identical_canonical_bytes_and_revision()
    {
        Corpus.SourceForms.Select(form => form.Name).ShouldEqual("single", "folder", "reordered", "relocated");
        foreach (var form in Corpus.SourceForms)
        {
            var model = Compile(Corpus, form).Model;
            model.LanguageVersion.ShouldEqual(LanguageVersion.V7);
            model.SemanticVersion.ShouldEqual(SemanticVersion.V7);
            model.Revision.ShouldEqual(Corpus.SemanticRevision);
            SemanticModelSerializer.Serialize(model).SequenceEqual(Corpus.EsmBytes).ShouldBeTrue();
        }
    }

    [Fact]
    void should_bind_two_distinct_generated_uuid_concepts_and_an_ordered_record_response()
    {
        var model = Compile(Corpus, Corpus.SourceForms[0]).Model;
        var slice = model.Application.Modules.Single().Features.Single().Slices.Single();
        var command = slice.Commands.Single();
        var generated = command.Properties.Where(property => property.IsGenerated).ToArray();
        generated.Length.ShouldEqual(2);
        generated.Select(property => property.Type.Target).Distinct().Count().ShouldEqual(2);
        generated.All(property => property.Type.Kind == SemanticTypeReferenceKind.Concept).ShouldBeTrue();
        command.Properties.Single(property => property.IsIdentifier).IsGenerated.ShouldBeTrue();
        var response = (SemanticRecordCommandResponse)command.Response!;
        response.Fields.Select(field => field.Name).ShouldEqual("projectId", "receipt", "name");
        var accepted = slice.Specifications.Single(specification => specification.Name == "RegisteringAProject");
        accepted.When!.Values.Select(value => value.TargetProperty).ShouldEqual(command.Properties.Single(property => property.Name == "name").Id);
        accepted.When.GeneratedValues.Length.ShouldEqual(2);
        accepted.When.EventSource.ShouldBeNull();
        ((SemanticTextValue)accepted.When.GeneratedValues.Single(value => value.TargetProperty == command.Properties.Single(property => property.Name == "receipt").Id).Value).Value
            .ShouldEqual("22222222-2222-2222-2222-2222222222aa");
        slice.Specifications.Single(specification => specification.Name == "MissingAllocation").When!.GeneratedValues.ShouldBeEmpty();
    }

    [Fact] void should_round_trip_the_strict_reader() => SemanticModelSerializer.Serialize(SemanticModelSerializer.Deserialize(Corpus.EsmBytes.AsSpan())).SequenceEqual(Corpus.EsmBytes).ShouldBeTrue();
}

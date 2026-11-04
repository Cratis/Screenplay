// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_PossibleEventsCorpus;

public class when_loading_v35_source : Specification
{
    ExecutableSemanticModel[] _models;

    void Because()
    {
        _models = [.. PossibleEventsCorpus.SourceForms.Select(Compile)];
        if (Environment.GetEnvironmentVariable("SCREENPLAY_REGENERATE_POSSIBLE_EVENTS_CORPUS") == "1")
        {
            var root = Path.Combine(Root(), "Source/DotNET/Screenplay.CanonicalCorpus/Corpus/PossibleEvents/v35/expected");
            Directory.CreateDirectory(root);
            File.WriteAllBytes(Path.Combine(root, "esm-v35.json"), SemanticModelSerializer.Serialize(_models[0]));
            File.WriteAllText(Path.Combine(root, "semantic-revision.txt"), $"{_models[0].Revision}\n");
            Assert.Fail("Possible events corpus regenerated; review, rebuild and rerun without SCREENPLAY_REGENERATE_POSSIBLE_EVENTS_CORPUS.");
        }
    }

    [Fact] void should_select_v35_in_all_four_forms() => _models.Length.ShouldEqual(4);
    [Fact] void should_use_the_admitted_version() => _models.All(model => model.SemanticVersion == SemanticVersion.V35).ShouldBeTrue();
    [Fact] void should_write_the_result_contract() => System.Text.Encoding.UTF8.GetString(SemanticModelSerializer.Serialize(_models[0])).ShouldContain("\"resultVersion\":2,\"possibleEvents\":[");

    [Fact]
    void should_pin_every_model_and_its_round_trip()
    {
        var vector = PossibleEventsCorpus.V35;
        foreach (var model in _models)
        {
            model.Revision.ShouldEqual(vector.SemanticRevision);
            SemanticModelSerializer.Serialize(model).SequenceEqual(vector.EsmBytes).ShouldBeTrue();
        }
        SemanticModelSerializer.Serialize(SemanticModelSerializer.Deserialize(vector.EsmBytes.AsSpan())).SequenceEqual(vector.EsmBytes).ShouldBeTrue();
    }

    static ExecutableSemanticModel Compile(CanonicalCorpusSourceForm form)
    {
        var catalog = SemanticIdentityCatalogSerializer.Deserialize(form.IdentityCatalogBytes.AsSpan());
        var documents = form.Documents.Select(document => SemanticSourceDocument.Create(catalog.ResolveDocument(document.StableKey), document.StableKey, document.DisplayPath, document.Text));
        var result = new SemanticModelCompiler().Compile("PossibleEvents", SemanticDocumentSet.Create([.. documents], catalog));
        Assert.True(result.Success, string.Join('\n', result.Diagnostics.Select(diagnostic => diagnostic.Message)));
        return result.Value.Model;
    }

    static string Root([CallerFilePath] string path = "")
    {
        var directory = Directory.GetParent(path);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "Documentation"))) directory = directory.Parent;
        return directory.FullName;
    }
}

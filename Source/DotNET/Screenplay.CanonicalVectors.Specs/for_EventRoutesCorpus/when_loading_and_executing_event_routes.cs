// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_EventRoutesCorpus;

public class when_loading_and_executing_event_routes : Specification
{
    [Theory]
    [InlineData("scalar")]
    [InlineData("specifications")]
    [InlineData("composites")]
    void should_pin_bytes_catalogs_revisions_and_outcomes_across_four_forms(string key)
    {
        if (Environment.GetEnvironmentVariable("SCREENPLAY_REGENERATE_EVENT_ROUTES_CORPUS") == "1") Regenerate(key);
        var corpus = key switch { "scalar" => EventRoutesCorpus.Scalar, "specifications" => EventRoutesCorpus.Specifications, _ => EventRoutesCorpus.Composites };
        corpus.SourceForms.Length.ShouldEqual(4);
        foreach (var form in corpus.SourceForms)
        {
            var compilation = Compile(form);
            var model = compilation.Model;
            model.LanguageVersion.ShouldEqual(EventRoutesVersion.Language);
            model.SemanticVersion.ShouldEqual(EventRoutesVersion.Semantic);
            SemanticModelSerializer.Serialize(model).SequenceEqual(corpus.EsmBytes).ShouldBeTrue();
            model.Revision.ShouldEqual(corpus.SemanticRevision);
            var catalog = SemanticIdentityCatalogSerializer.Deserialize(form.IdentityCatalogBytes.AsSpan());
            SemanticIdentityCatalogSerializer.Serialize(catalog).SequenceEqual(form.IdentityCatalogBytes).ShouldBeTrue();
            catalog.Semantics.Any(entry => entry.Address.Kind == SemanticKind.EventSource).ShouldBeTrue();
            catalog.Semantics.Any(entry => entry.Address.Kind == SemanticKind.EventStream).ShouldBeTrue();
            var read = SemanticModelSerializer.Deserialize(corpus.EsmBytes.AsSpan());
            SemanticModelSerializer.Serialize(read).SequenceEqual(corpus.EsmBytes).ShouldBeTrue();
            var slice = model.Application.Modules.Single().Features.Single().Slices.Single();
            slice.Specifications.Select(spec => spec.Id).OrderBy(id => id.ToString(), StringComparer.Ordinal).SequenceEqual(corpus.SpecificationExpectations.Select(expected => expected.Specification)).ShouldBeTrue();
            var plan = SemanticExecutionPlan.Compile(read).Plan!;
            foreach (var expected in corpus.SpecificationExpectations)
            {
                var specification = slice.Specifications.Single(spec => spec.Id == expected.Specification);
                specification.Name.ShouldEqual(expected.Name);
                var run = new SemanticSpecificationRunner().Run(plan, expected.Specification);
                Assert.True(run.Passed == expected.Passed, $"{key}/{form.Name}/{expected.Name}: {string.Join(';', run.Failures)}");
                run.Execution.Kind.ShouldEqual(expected.Outcome);
                run.Execution.World.Facts.Length.ShouldEqual(expected.WorldFactCount!.Value);
                if (run.Execution is SemanticRejected rejected)
                {
                    rejected.Category.ShouldEqual(expected.RejectionCategory!.Value);
                    rejected.Details.ShouldEqual(expected.RejectionMessage);
                }
                var facts = (run.Execution as SemanticAccepted)?.Facts ?? [];
                facts.Select(fact => Route(fact.Route)).SequenceEqual(expected.Routes).ShouldBeTrue();
                if (expected.Passed) run.Failures.ShouldBeEmpty();
                if (expected.Name == "HistoryWithoutProducer") run.Execution.World.Facts[0].Route!.StreamKind.ShouldEqual("All");
            }
        }
    }

    internal static SemanticCompilation Compile(CanonicalCorpusSourceForm form)
    {
        var catalog = SemanticIdentityCatalogSerializer.Deserialize(form.IdentityCatalogBytes.AsSpan());
        var documents = form.Documents.Select(document => SemanticSourceDocument.Create(catalog.ResolveDocument(document.StableKey), document.StableKey, document.DisplayPath, document.Text));
        var result = new SemanticModelCompiler().Compile("EventRoutes", SemanticDocumentSet.Create([.. documents], catalog));
        Assert.True(result.Success, string.Join('\n', result.Diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}")));

        return result.Value!;
    }

    internal static string Route(SemanticEventRoute? route)
    {
        if (route is null) return "null";

        return route.StreamId is null
            ? JsonSerializer.Serialize(new { sourceKind = route.SourceKind, streamKind = route.StreamKind })
            : JsonSerializer.Serialize(new { sourceKind = route.SourceKind, streamKind = route.StreamKind, streamId = route.StreamId });
    }

    static void Regenerate(string key)
    {
        var root = Path.Combine(Root(), "Source/DotNET/Screenplay.CanonicalCorpus/Corpus/EventRoutes", key);
        var declarations = File.ReadAllBytes(Path.Combine(root, "source/declarations.play")).ToImmutableArray();
        var module = File.ReadAllBytes(Path.Combine(root, "source/module.play")).ToImmutableArray();
        var seed = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("EventRoutes"));
        var folder = new CanonicalCorpusSourceForm
        {
            Name = "folder",
            IdentityCatalogBytes = [.. SemanticIdentityCatalogSerializer.Serialize(seed)],
            Documents =
            [
                new() { StableKey = "declarations", DisplayPath = "declarations.play", Bytes = declarations },
                new() { StableKey = "module", DisplayPath = "M/module.play", Bytes = module }
            ]
        };
        var single = folder with { Name = "single", Documents = [new() { StableKey = "application", DisplayPath = "application.play", Bytes = [.. declarations, .. module] }] };
        var expected = Path.Combine(root, "expected");
        Directory.CreateDirectory(expected);
        foreach (var form in new[] { single, folder })
        {
            var compilation = Compile(form);
            var assignments = new List<SemanticIdentityAssignment>();
            foreach (var source in compilation.Model.Application.EventSources)
            {
                var address = SemanticAddress.ForEventSource(seed.Application, source.Name);
                assignments.Add(new(address, source.Id, SemanticIdentityOrigin.Persisted));
                assignments.AddRange(source.Streams.Select(stream => new SemanticIdentityAssignment(SemanticAddress.ForEventStream(address, stream.Name), stream.Id, SemanticIdentityOrigin.Persisted)));
            }
            var catalog = SemanticIdentityCatalog.Create(seed.Application, [.. form.Documents.Select(document => new DocumentIdentityAssignment(document.StableKey, seed.ResolveDocument(document.StableKey), SemanticIdentityOrigin.Persisted))], [.. assignments], []);
            var bytes = SemanticIdentityCatalogSerializer.Serialize(catalog);
            File.WriteAllBytes(Path.Combine(expected, $"identity-catalog-{form.Name}.json"), bytes);
            var pinned = Compile(form with { IdentityCatalogBytes = [.. bytes] });
            SemanticModelSerializer.Serialize(pinned.Model).SequenceEqual(SemanticModelSerializer.Serialize(compilation.Model)).ShouldBeTrue();
            File.WriteAllBytes(Path.Combine(expected, "esm-event-routes.json"), SemanticModelSerializer.Serialize(pinned.Model));
            File.WriteAllText(Path.Combine(expected, "semantic-revision.txt"), $"{pinned.Model.Revision}\n", new UTF8Encoding(false));
        }
        Assert.Fail("Event-routes corpus regenerated; review, rebuild and rerun without SCREENPLAY_REGENERATE_EVENT_ROUTES_CORPUS.");
    }

    static string Root([CallerFilePath] string path = "")
    {
        var directory = Directory.GetParent(path);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "Documentation"))) directory = directory.Parent;

        return directory!.FullName;
    }
}

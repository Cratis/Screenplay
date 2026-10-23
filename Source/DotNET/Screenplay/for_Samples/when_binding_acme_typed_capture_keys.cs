// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.for_Documentation.given;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.for_Samples;

public class when_binding_acme_typed_capture_keys : Specification
{
    ExecutableSemanticModel _model;

    void Because()
    {
        var root = Path.Combine(Directory.GetParent(DocumentationExamples.Root())!.FullName, "Samples/AcmeCommerce");
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Acme Commerce Shop"));
        var documents = Directory.GetFiles(root, "*.play", SearchOption.AllDirectories).Select(path =>
        {
            var key = Path.GetRelativePath(root, path).Replace('\\', '/');
            return SemanticSourceDocument.Create(catalog.ResolveDocument(key.Replace('/', '-')), key.Replace('/', '-'), key, File.ReadAllText(path));
        });
        var result = new SemanticModelCompiler().Compile("Acme Commerce Shop", SemanticDocumentSet.Create([.. documents], catalog));
        Assert.True(result.Success, string.Join('\n', result.Diagnostics.Select(diagnostic => diagnostic.Message)));
        _model = result.Value!.Model;
    }

    [Fact] void should_select_v32() => _model.SemanticVersion.ShouldEqual(SemanticVersion.V32);

    [Theory]
    [InlineData("OrderReceived")]
    [InlineData("MyOrderUpdated")]
    void should_use_order_id_as_the_captured_identity(string name)
    {
        var orderId = SemanticTypeReference.ForConcept(_model.Application.Concepts.Single(concept => concept.Name == "OrderId").Id);
        var slices = Slices(_model.Application.Modules.SelectMany(module => module.Features));
        var events = slices.SelectMany(slice => slice.Events).Where(value => value.Name == name).Select(value => value.Id).ToHashSet();
        var appends = slices.SelectMany(slice => slice.Captures).SelectMany(capture => capture.Appends).Where(append => events.Contains(append.EventContract)).ToArray();
        appends.ShouldNotBeEmpty();
        appends.All(append => append.EventSourceType == orderId).ShouldBeTrue();
    }

    static SemanticSlice[] Slices(IEnumerable<SemanticFeature> features) =>
        [.. features.SelectMany(feature => feature.Slices.Concat(Slices(feature.Features)))];
}

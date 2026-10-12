// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Screenplay.Semantics.Serialization.given;

public static partial class canonical_serialization_golden_vectors
{
    public static byte[] EsmV14Bytes => ReadResource("Cratis.Screenplay.Semantics.Serialization.Golden.full-esm-v14.json");

    public static ExecutableSemanticModel CreateSemanticModelV14()
    {
        var application = CreateSemanticModelV13().Application;
        var module = application.Modules[0];
        var feature = module.Features.Single(value => value.Id == Id(9300));
        var text = SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text);
        var required = new SemanticProperty(Id(10411), "status", text, false);
        var optional = new SemanticProperty(Id(10412), "category", SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text, isOptional: true), false);
        var readModel = new SemanticReadModel(Id(10410), "FilterableEntity", [new(Id(10413), "entityId", text, true), required, optional]);
        var query = new SemanticKeyedQuery(Id(10400), "FilteredEntities", null, readModel.Id, null, SemanticQueryCardinality.Many, SemanticQueryDelivery.Snapshot)
        {
            Filters = [new(Id(10401), required.Name, required.Type, required.Id), new(Id(10402), optional.Name, optional.Type, optional.Id)]
        };
        var specification = new SemanticSpecification(
            Id(10403),
            "OnlyTheRequiredFilter",
            [],
            [],
            null,
            [],
            [],
            [new(query.Id, SemanticValue.Null, []) { Filters = [new(Id(10401), SemanticValue.Text("example"))] }],
            []);
        var filtered = new SemanticSlice(Id(10420), "FilteredEntities", SemanticSliceKind.StateView, [], [], [readModel], [], [query], [specification]);
        application = application with
        {
            Modules = application.Modules.SetItem(0, module with
            {
                Features = [.. module.Features.Select(value => value.Id == feature.Id ? value with { Slices = value.Slices.Add(filtered) } : value)]
            })
        };

        return ExecutableSemanticModel.Create(LanguageVersion.V14, SemanticVersion.V14, application);
    }
}
#endif

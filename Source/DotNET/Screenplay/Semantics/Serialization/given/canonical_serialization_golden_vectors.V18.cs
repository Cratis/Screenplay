// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Screenplay.Semantics.Serialization.given;

public static partial class canonical_serialization_golden_vectors
{
    public static byte[] EsmV18Bytes => ReadResource("Cratis.Screenplay.Semantics.Serialization.Golden.full-esm-v18.json");

    public static ExecutableSemanticModel CreateSemanticModelV18()
    {
        var application = CreateSemanticModelV17().Application;
        var concept = new SemanticConcept(Id(10700), "InvoiceNumber", SemanticPrimitiveType.Text, [],
            [new(default, SemanticValidationRuleKind.Minimum, SemanticValue.Number(2), "Invoice number is too short")]);
        var type = SemanticTypeReference.ForConcept(concept.Id);
        var property = new SemanticProperty(Id(10711), "number", type, true);
        var readModel = new SemanticReadModel(Id(10710), "InvoiceLookup", [property]);
        var query = new SemanticKeyedQuery(Id(10720), "InvoiceByNumber", new(Id(10721), "number", type), readModel.Id, property.Id, SemanticQueryCardinality.ZeroOrOne, SemanticQueryDelivery.Snapshot);
        var specification = new SemanticSpecification(Id(10730), "MalformedInvoiceNumber", [], [], null, [], [],
            [new(query.Id, SemanticValue.Text("A"), [])], [new(null, "Invoice number is too short")]);
        var bare = specification with { Id = Id(10731), Name = "MalformedInvoiceNumberWithoutMessage", ThenErrors = [new(null, null)] };
        var slice = new SemanticSlice(Id(10740), "InvoiceLookup", SemanticSliceKind.StateView, [], [], [readModel], [], [query], [specification, bare]);
        var module = application.Modules[0];
        application = application with
        {
            Concepts = application.Concepts.Add(concept),
            Modules = application.Modules.SetItem(0, module with { Features = module.Features.Add(new(Id(10750), "QueryValidation", [], [slice])) })
        };
        return ExecutableSemanticModel.Create(LanguageVersion.V18, SemanticVersion.V18, application);
    }
}
#endif

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_projection_blocks.given;

// One application declaring what the projection-block specs project from; each spec supplies only the projection body.
public class a_projection_block_binder : for_SemanticModelBinder.given.a_semantic_binder
{
    const string Declarations =
        """
        concept OrderId : Uuid
        concept CustomerId : Uuid
        type OrderLine
          lineNumber Int
          quantity Int
          subtotal Decimal?
          discontinued Bool?
        type Shipping
          carrier String
          note String?
        type Quantity
          amount Int
          basis String
        type OrderLineKey
          orderId OrderId
          lineNumber Int
        module Orders
          feature Ordering
            slice StateChange Events
              event OrderPlaced
                orderId OrderId
                customerId CustomerId
                label String
                quantity Quantity
              event LineAdded
                orderId OrderId
                lineNumber Int
                amount Decimal
              event LineRemoved
                orderId OrderId
                lineNumber Int
              event OrderShipped
                carrier String
              event ShippingCleared
                orderId OrderId
              event CustomerRegistered
                customerName String
              event CustomerClosed
                customerId CustomerId
              event OrderCancelled
                orderId OrderId
            slice StateView Lookup
              readmodel OrderView
                orderId OrderId
                customerId CustomerId?
                label String?
                amount Int?
                basis String?
                total Decimal?
                events Int?
                lastSeen DateTime?
                customerName String?
                lines OrderLine[]
                shipping Shipping?
                quantity Quantity?
              query OrderById => OrderView?
                by orderId OrderId
              readmodel LineView
                key OrderLineKey
                amount Decimal?
              query LineByKey => LineView?
                by key OrderLineKey
        """;

    protected CompilationResult<SemanticCompilation> _result;

    protected SemanticApplication Application => _result.Value!.Model.Application;

    protected IEnumerable<SemanticSlice> Slices => Application.Modules.Single().Features.Single().Slices;

    protected SemanticProjection Projection => Slices.SelectMany(_ => _.Projections).Single();

    protected SemanticProjectionScope Scope => Projection.Scope!;

    protected IEnumerable<Diagnostic> Errors => _result.Diagnostics.Where(_ => _.Severity == DiagnosticSeverity.Error);

    // The body is indented under the projection line, so a spec writes it at column zero.
    protected CompilationResult<SemanticCompilation> BindProjection(string header, string body) =>
        Bind($"{Declarations}\n      {header}\n{string.Join('\n', body.Split('\n').Select(_ => $"        {_}"))}\n");

    protected SemanticId EventId(string name) => Slices.SelectMany(_ => _.Events).Single(_ => _.Name == name).Id;

    protected SemanticId EventProperty(string eventName, string name) =>
        Slices.SelectMany(_ => _.Events).Single(_ => _.Name == eventName).Properties.Single(_ => _.Name == name).Id;

    protected SemanticId ReadModelProperty(string readModel, string name) =>
        Slices.SelectMany(_ => _.ReadModels).Single(_ => _.Name == readModel).Properties.Single(_ => _.Name == name).Id;

    protected SemanticId TypeProperty(string type, string name) =>
        Application.Types.Single(_ => _.Name == type).Properties.Single(_ => _.Name == name).Id;
}

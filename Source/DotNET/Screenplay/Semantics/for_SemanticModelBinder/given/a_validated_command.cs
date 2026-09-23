// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.given;

public class a_validated_command : a_semantic_binder
{
    protected CompilationResult<SemanticCompilation> _result;

    protected SemanticCommand Command => _result.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single().Commands.Single();

    protected SemanticValidationRule Rule => Command.Validations.Single();

    protected Diagnostic Diagnostic => _result.Diagnostics.Single();

    protected SemanticId PropertyId(string name) => Command.Properties.Single(_ => _.Name == name).Id;

    protected CompilationResult<SemanticCompilation> BindRules(params string[] rules) => Bind(
        $$"""
        concept Status : Enum
          open
          closed
        concept Reference : String
        module Orders
          feature Ordering
            slice StateChange PlaceOrder
              command PlaceOrder
                orderId Uuid identifier
                name String
                reference Reference
                quantity Int
                amount Decimal
                status Status
                express Bool
                dueDate Date
                weights Decimal[]
                tags String[]
                validate
        {{string.Join(Environment.NewLine, rules.Select(_ => $"          {_}"))}}
        """);
}

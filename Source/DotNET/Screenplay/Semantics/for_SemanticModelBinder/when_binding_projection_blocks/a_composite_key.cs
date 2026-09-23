// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_projection_blocks;

// Chronicle lowers a composite key to $composite(Type, part=expression, ...) and admits only simple part expressions - templates
// are rejected (ProjectionDefinitionSyntaxVisitor.cs:255-257, ProjectionValidator.cs:383-465).
public class a_composite_key : given.a_projection_block_binder
{
    CompilationResult<SemanticCompilation> _template;

    void Because()
    {
        _result = BindProjection("projection Lines => LineView", "from LineAdded\n  key OrderLineKey\n    orderId = orderId\n    lineNumber = lineNumber\n  amount = amount");
        _template = BindProjection("projection Lines => LineView", "from LineAdded\n  key OrderLineKey\n    orderId = `${orderId}`\n    lineNumber = lineNumber\n  amount = amount");
    }

    [Fact] void should_bind() => _result.Success.ShouldBeTrue();
    [Fact] void should_key_on_the_composite_type() => ((SemanticProjectionCompositeKey)Scope.From.Single().Key).Type.ShouldEqual(Application.Types.Single(_ => _.Name == "OrderLineKey").Id);
    [Fact] void should_bind_every_part() => ((SemanticProjectionCompositeKey)Scope.From.Single().Key).Parts.Length.ShouldEqual(2);
    [Fact] void should_reject_a_template_part() => _template.Diagnostics.Any(_ => _.Code == DiagnosticCodes.UnsupportedSemanticSyntax && _.Message.Contains("Template expressions")).ShouldBeTrue();
}

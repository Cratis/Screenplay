// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Traversal of syntax-only response contracts and expectations.
/// </summary>
public abstract partial class ScreenplaySyntaxWalker
{
    /// <summary>
    /// Visits a command response by its shape.
    /// </summary>
    /// <param name="syntax">The response.</param>
    public virtual void VisitCommandResponse(CommandResponseSyntax syntax)
    {
        switch (syntax)
        {
            case ScalarCommandResponseSyntax scalar: VisitScalarCommandResponse(scalar); break;
            case RecordCommandResponseSyntax record: VisitRecordCommandResponse(record); break;
            default: VisitNode(syntax); break;
        }
    }

    /// <summary>
    /// Visits a scalar response and its source.
    /// </summary>
    /// <param name="syntax">The response.</param>
    public virtual void VisitScalarCommandResponse(ScalarCommandResponseSyntax syntax)
    {
        VisitNode(syntax);
        VisitPropertyResponseSource(syntax.Source);
    }

    /// <summary>
    /// Visits a record response and its fields in authored order.
    /// </summary>
    /// <param name="syntax">The response.</param>
    public virtual void VisitRecordCommandResponse(RecordCommandResponseSyntax syntax)
    {
        VisitNode(syntax);
        foreach (var field in syntax.Fields) VisitResponseField(field);
    }

    /// <summary>
    /// Visits a response field, explicit type and source.
    /// </summary>
    /// <param name="syntax">The field.</param>
    public virtual void VisitResponseField(ResponseFieldSyntax syntax)
    {
        VisitNode(syntax);
        if (syntax.Type is not null) VisitTypeRef(syntax.Type);
        VisitPropertyResponseSource(syntax.Source);
    }

    /// <summary>
    /// Visits a direct response source.
    /// </summary>
    /// <param name="syntax">The source.</param>
    public virtual void VisitPropertyResponseSource(PropertyResponseSourceSyntax syntax) => VisitNode(syntax);

    /// <summary>
    /// Visits a return expectation by its shape.
    /// </summary>
    /// <param name="syntax">The expectation.</param>
    public virtual void VisitSpecificationReturn(SpecificationReturnSyntax syntax)
    {
        switch (syntax)
        {
            case ScalarSpecificationReturnSyntax scalar: VisitScalarSpecificationReturn(scalar); break;
            case RecordSpecificationReturnSyntax record: VisitRecordSpecificationReturn(record); break;
            default: VisitNode(syntax); break;
        }
    }

    /// <summary>
    /// Visits a scalar expectation and concrete value.
    /// </summary>
    /// <param name="syntax">The expectation.</param>
    public virtual void VisitScalarSpecificationReturn(ScalarSpecificationReturnSyntax syntax)
    {
        VisitNode(syntax);
        VisitExpression(syntax.Value);
    }

    /// <summary>
    /// Visits a record expectation and its field assertions.
    /// </summary>
    /// <param name="syntax">The expectation.</param>
    public virtual void VisitRecordSpecificationReturn(RecordSpecificationReturnSyntax syntax)
    {
        VisitNode(syntax);
        foreach (var field in syntax.Fields) VisitPropertyMapping(field);
    }
}

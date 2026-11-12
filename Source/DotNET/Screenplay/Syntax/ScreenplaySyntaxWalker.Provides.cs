// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax;

public abstract partial class ScreenplaySyntaxWalker
{
    /// <summary>
    /// Visits a provided context value, its type and its source.
    /// </summary>
    /// <param name="syntax">The provided value to visit.</param>
    public virtual void VisitProvide(ProvideSyntax syntax)
    {
        VisitNode(syntax);
        VisitTypeRef(syntax.Type);
        VisitProvideSource(syntax.Source);
    }

    /// <summary>
    /// Dispatches a provided value's source, leaving unknown forms to the fallback.
    /// </summary>
    /// <param name="syntax">The source to visit.</param>
    public virtual void VisitProvideSource(ProvideSourceSyntax syntax)
    {
        switch (syntax)
        {
            case QueryProvideSourceSyntax query:
                VisitNode(query);
                foreach (var by in query.By) VisitExpression(by);
                break;
            case ValueProvideSourceSyntax value:
                VisitNode(value);
                VisitExpression(value.Value);
                break;
            case CodeProvideSourceSyntax code:
                VisitNode(code);
                VisitCodeBlock(code.Code);
                break;
            case FileProvideSourceSyntax file:
                VisitNode(file);
                VisitFileReference(file.File);
                break;
            default:
                VisitNode(syntax);
                break;
        }
    }
}

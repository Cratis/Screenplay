// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax;

public partial class ScreenplaySyntaxWalker
{
    /// <summary>
    /// Visits identity authoring metadata and its details.
    /// </summary>
    /// <param name="syntax">The metadata to visit.</param>
    public virtual void VisitIdentity(IdentitySyntax syntax)
    {
        VisitNode(syntax);
        foreach (var detail in syntax.Details) VisitIdentityDetail(detail);
    }

    /// <summary>
    /// Visits a caller detail, its type and its source.
    /// </summary>
    /// <param name="syntax">The detail to visit.</param>
    public virtual void VisitIdentityDetail(IdentityDetailSyntax syntax)
    {
        VisitNode(syntax);
        VisitTypeRef(syntax.Type);
        VisitIdentitySource(syntax.Source);
    }

    /// <summary>
    /// Dispatches a detail source, leaving unknown forms to the fallback.
    /// </summary>
    /// <param name="syntax">The source to visit.</param>
    public virtual void VisitIdentitySource(IdentitySourceSyntax syntax)
    {
        switch (syntax)
        {
            case ClaimIdentitySourceSyntax claim: VisitClaimIdentitySource(claim); break;
            case QueryIdentitySourceSyntax query: VisitQueryIdentitySource(query); break;
            case CodeIdentitySourceSyntax code: VisitCodeIdentitySource(code); break;
            case FileIdentitySourceSyntax file: VisitFileIdentitySource(file); break;
            default: VisitNode(syntax); break;
        }
    }

    /// <summary>
    /// Visits a claim source.
    /// </summary>
    /// <param name="syntax">The source to visit.</param>
    public virtual void VisitClaimIdentitySource(ClaimIdentitySourceSyntax syntax) => VisitNode(syntax);

    /// <summary>
    /// Visits a query source and its key expression.
    /// </summary>
    /// <param name="syntax">The source to visit.</param>
    public virtual void VisitQueryIdentitySource(QueryIdentitySourceSyntax syntax)
    {
        VisitNode(syntax);
        VisitExpression(syntax.By);
    }

    /// <summary>
    /// Visits an inline source and its code.
    /// </summary>
    /// <param name="syntax">The source to visit.</param>
    public virtual void VisitCodeIdentitySource(CodeIdentitySourceSyntax syntax)
    {
        VisitNode(syntax);
        VisitCodeBlock(syntax.Code);
    }

    /// <summary>
    /// Visits a file source and its reference.
    /// </summary>
    /// <param name="syntax">The source to visit.</param>
    public virtual void VisitFileIdentitySource(FileIdentitySourceSyntax syntax)
    {
        VisitNode(syntax);
        VisitFileReference(syntax.File);
    }
}

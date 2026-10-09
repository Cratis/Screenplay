// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax;

public abstract partial class ScreenplaySyntaxWalker
{
    /// <summary>
    /// Visits a processing purpose and its transfers.
    /// </summary>
    /// <param name="syntax">The declaration.</param>
    public virtual void VisitPurpose(PurposeSyntax syntax)
    {
        VisitNode(syntax);
        foreach (var transfer in syntax.Transfers) VisitPurposeTransfer(transfer);
    }

    /// <summary>
    /// Visits a processing purpose reference.
    /// </summary>
    /// <param name="syntax">The reference.</param>
    public virtual void VisitPurposeReference(PurposeReferenceSyntax syntax) => VisitNode(syntax);

    /// <summary>
    /// Visits a declared transfer and safeguard.
    /// </summary>
    /// <param name="syntax">The transfer.</param>
    public virtual void VisitPurposeTransfer(PurposeTransferSyntax syntax) => VisitNode(syntax);
}

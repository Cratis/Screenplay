// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.Captures;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Traversal of captures - their source, value mappings, appends and nested structures.
/// </summary>
public abstract partial class ScreenplaySyntaxWalker
{
    /// <summary>
    /// Visits a <see cref="CaptureSyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="CaptureSyntax"/> to visit.</param>
    /// <remarks>
    /// This is one of the four roots - a capture compiled on its own with
    /// <see cref="IScreenplayCompiler.CompileCapture(string)"/> starts here.
    /// </remarks>
    public virtual void VisitCapture(CaptureSyntax syntax)
    {
        VisitNode(syntax);

        if (syntax.Source is not null)
        {
            VisitCaptureSource(syntax.Source);
        }

        foreach (var operation in syntax.Map)
        {
            VisitCaptureMapOperation(operation);
        }

        foreach (var append in syntax.Appends)
        {
            VisitCaptureAppend(append);
        }

        foreach (var children in syntax.Children)
        {
            VisitCaptureChildren(children);
        }

        foreach (var nested in syntax.Nested)
        {
            VisitCaptureNested(nested);
        }
    }

    /// <summary>
    /// Visits a <see cref="CaptureSourceSyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="CaptureSourceSyntax"/> to visit.</param>
    public virtual void VisitCaptureSource(CaptureSourceSyntax syntax)
    {
        VisitNode(syntax);

        foreach (var setting in syntax.Settings)
        {
            VisitCaptureSourceSetting(setting);
        }
    }

    /// <summary>
    /// Visits a <see cref="CaptureSourceSettingSyntax"/> node.
    /// </summary>
    /// <param name="syntax">The <see cref="CaptureSourceSettingSyntax"/> to visit.</param>
    public virtual void VisitCaptureSourceSetting(CaptureSourceSettingSyntax syntax) => VisitNode(syntax);

    /// <summary>
    /// Visits a <see cref="CaptureMapOperationSyntax"/> node by dispatching to the method for its kind.
    /// </summary>
    /// <param name="syntax">The <see cref="CaptureMapOperationSyntax"/> to visit.</param>
    /// <remarks>
    /// An operation kind this walker does not know is visited as a node and not descended into, so a kind
    /// added to the language later cannot break an existing walker.
    /// </remarks>
    public virtual void VisitCaptureMapOperation(CaptureMapOperationSyntax syntax)
    {
        switch (syntax)
        {
            case CaptureMapEntrySyntax entry:
                VisitCaptureMapEntry(entry);
                break;
            case CaptureSplitSyntax split:
                VisitCaptureSplit(split);
                break;
            default:
                VisitNode(syntax);
                break;
        }
    }

    /// <summary>
    /// Visits a <see cref="CaptureMapEntrySyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="CaptureMapEntrySyntax"/> to visit.</param>
    public virtual void VisitCaptureMapEntry(CaptureMapEntrySyntax syntax)
    {
        VisitNode(syntax);
        VisitExpression(syntax.Source);

        foreach (var translation in syntax.Translations)
        {
            VisitCaptureTranslation(translation);
        }
    }

    /// <summary>
    /// Visits a <see cref="CaptureTranslationSyntax"/> node.
    /// </summary>
    /// <param name="syntax">The <see cref="CaptureTranslationSyntax"/> to visit.</param>
    public virtual void VisitCaptureTranslation(CaptureTranslationSyntax syntax) => VisitNode(syntax);

    /// <summary>
    /// Visits a <see cref="CaptureSplitSyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="CaptureSplitSyntax"/> to visit.</param>
    public virtual void VisitCaptureSplit(CaptureSplitSyntax syntax)
    {
        VisitNode(syntax);
        VisitExpression(syntax.Source);
    }

    /// <summary>
    /// Visits a <see cref="CaptureAppendSyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="CaptureAppendSyntax"/> to visit.</param>
    public virtual void VisitCaptureAppend(CaptureAppendSyntax syntax)
    {
        VisitNode(syntax);

        if (syntax.When is not null)
        {
            VisitCaptureWhen(syntax.When);
        }

        foreach (var mapping in syntax.Mappings)
        {
            VisitPropertyMapping(mapping);
        }

        foreach (var tag in syntax.Tags ?? [])
        {
            VisitTag(tag);
        }
    }

    /// <summary>
    /// Visits a <see cref="CaptureWhenSyntax"/> node.
    /// </summary>
    /// <param name="syntax">The <see cref="CaptureWhenSyntax"/> to visit.</param>
    public virtual void VisitCaptureWhen(CaptureWhenSyntax syntax) => VisitNode(syntax);

    /// <summary>
    /// Visits a <see cref="CaptureChildrenSyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="CaptureChildrenSyntax"/> to visit.</param>
    public virtual void VisitCaptureChildren(CaptureChildrenSyntax syntax)
    {
        VisitNode(syntax);

        foreach (var operation in syntax.Map)
        {
            VisitCaptureMapOperation(operation);
        }

        foreach (var append in syntax.Appends)
        {
            VisitCaptureAppend(append);
        }
    }

    /// <summary>
    /// Visits a <see cref="CaptureNestedSyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="CaptureNestedSyntax"/> to visit.</param>
    public virtual void VisitCaptureNested(CaptureNestedSyntax syntax)
    {
        VisitNode(syntax);

        foreach (var operation in syntax.Map)
        {
            VisitCaptureMapOperation(operation);
        }

        foreach (var append in syntax.Appends)
        {
            VisitCaptureAppend(append);
        }
    }
}

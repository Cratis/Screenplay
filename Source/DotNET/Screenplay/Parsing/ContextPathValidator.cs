// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Withdraws the parser's unknown <c>$context.&lt;root&gt;</c> warning for a root that a <c>provide</c> in scope names, once the whole
/// application is assembled and every level's provided values are known.
/// </summary>
internal sealed class ContextPathValidator(ParserContext context) : ScreenplaySyntaxWalker
{
    readonly Stack<IReadOnlySet<string>> _scopes = new();

    /// <summary>
    /// Gets the roots a <c>$context.</c> path may start with besides <see cref="ContextExpressionSyntax.KnownRoots"/>.
    /// </summary>
    internal static IEnumerable<string> ReservedRoots => ["audiences"];

    /// <inheritdoc/>
    public override void VisitApplication(ApplicationSyntax syntax)
    {
        _scopes.Push(Names(syntax.Provides));
        base.VisitApplication(syntax);
        _scopes.Pop();
    }

    /// <inheritdoc/>
    public override void VisitModule(ModuleSyntax syntax)
    {
        _scopes.Push(Names(syntax.Provides));
        base.VisitModule(syntax);
        _scopes.Pop();
    }

    /// <inheritdoc/>
    public override void VisitFeature(FeatureSyntax syntax)
    {
        _scopes.Push(Names(syntax.Provides));
        base.VisitFeature(syntax);
        _scopes.Pop();
    }

    /// <inheritdoc/>
    public override void VisitSlice(SliceSyntax syntax)
    {
        _scopes.Push(Names(syntax.Provides));
        base.VisitSlice(syntax);
        _scopes.Pop();
    }

    /// <inheritdoc/>
    public override void VisitContextExpression(ContextExpressionSyntax syntax)
    {
        base.VisitContextExpression(syntax);
        if (ContextExpressionSyntax.KnownRoots.Contains(syntax.Root))
        {
            return;
        }

        // The parser warned about every root it did not know; a root a provide in scope names is withdrawn here, once every level is known.
        if (ReservedRoots.Contains(syntax.Root) || _scopes.Any(scope => scope.Contains(syntax.Root)))
        {
            context.Withdraw(DiagnosticCodes.UnknownContextPath, syntax.Location, $"'{syntax.Path}'");
        }
    }

    static HashSet<string> Names(IEnumerable<ProvideSyntax> provides) => [.. provides.Select(provide => provide.Name)];
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler.given;

/// <summary>
/// A document with an application shell and two modules, to which a navigation scenario is appended.
/// </summary>
public class a_navigation_document : a_compiler
{
    /// <summary>
    /// The layout and modules every navigation scenario builds on.
    /// </summary>
    public const string Foundation =
        """
        layout AppShell
          category application
          topbar
          navigation contributes Navigation
          content
          side

        module Sales
          feature Orders
            feature Returns
          feature Customers

        module Support
          feature Tickets

        module Empty
        """;

    protected CompilationResult<ApplicationSyntax> _result;

    protected IEnumerable<Diagnostic> Codes(string code) => _result.Diagnostics.Where(diagnostic => diagnostic.Code == code);

    /// <summary>
    /// Compiles the foundation with extra navigation lines.
    /// </summary>
    /// <param name="shell">Lines added to the layout, indented as written.</param>
    /// <param name="sales">Lines added right under <c>module Sales</c>, indented as written.</param>
    /// <param name="support">Lines added right under <c>module Support</c>, indented as written.</param>
    protected void CompileWith(string shell, string sales = "", string support = "")
    {
        var source = Foundation;
        if (shell.Length > 0)
        {
            source = source.Replace("  side\n", "  side\n" + shell.TrimEnd() + "\n", StringComparison.Ordinal);
        }

        if (sales.Length > 0)
        {
            source = source.Replace("module Sales\n", "module Sales\n" + sales.TrimEnd() + "\n", StringComparison.Ordinal);
        }

        if (support.Length > 0)
        {
            source = source.Replace("module Support\n", "module Support\n" + support.TrimEnd() + "\n", StringComparison.Ordinal);
        }

        _result = _compiler.Compile(source);
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler.given;

/// <summary>
/// A document with State Change, State View and Automation slices and a screen with a toolbar, to which button lines are added.
/// </summary>
public class a_toolbar_document : a_compiler
{
    protected CompilationResult<ApplicationSyntax> _result;

    protected IEnumerable<Diagnostic> Codes(string code) => _result.Diagnostics.Where(diagnostic => diagnostic.Code == code);

    protected ScreenToolbarSyntax Toolbar => _result.Value!.Modules.Single().Features.Single(feature => feature.Name == "Orders").Slices.Single(slice => slice.Name == "OrderList")
        .Screens.Single().Directives.OfType<ScreenToolbarSyntax>().Single();

    /// <summary>
    /// Compiles the foundation with the given lines inside the toolbar, each indented by eight spaces more than the toolbar.
    /// </summary>
    /// <param name="lines">The toolbar body lines, without indentation.</param>
    protected void CompileWith(params string[] lines)
    {
        var body = string.Concat(lines.Select(line => "            " + line + "\n"));
        _result = _compiler.Compile(
            "module Shop\n" +
            "  feature Orders\n" +
            "    slice StateChange PlaceOrder\n" +
            "      command PlaceOrder\n" +
            "        orderId Uuid identifier\n" +
            "        produces OrderPlaced\n" +
            "          orderId = orderId\n" +
            "      event OrderPlaced\n" +
            "        orderId Uuid\n" +
            "    slice StateView OrderList\n" +
            "      readmodel OrderRow\n" +
            "        orderId Uuid\n" +
            "      screen List\n" +
            "        toolbar actions\n" + body +
            "  feature Billing\n" +
            "    slice StateChange PlaceOrder\n" +
            "      command PlaceInvoiceOrder\n" +
            "        orderId Uuid identifier\n" +
            "        produces InvoiceOrderPlaced\n" +
            "          orderId = orderId\n" +
            "      event InvoiceOrderPlaced\n" +
            "        orderId Uuid\n" +
            "    slice Automation Chase\n" +
            "      reaction Chaser\n" +
            "        when InvoiceOrderPlaced\n");
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

/// <summary>
/// A button that names a slice is a hook, and the messages say so; a retired form's message names the command and the
/// step that rewrites it the way the migration reports it.
/// </summary>
public class when_wording_hook_and_migration_messages : given.a_placement_document
{
    const string Hook = "      button x title \"X\" ";

    static string Message(string source, string code) =>
        new ScreenplayCompiler().Compile(source).Diagnostics.Single(diagnostic => diagnostic.Code == code).Message;

    [Fact] void should_call_a_button_that_names_a_slice_a_hook() => Message(Source(feature: Hook + "slice SetProductPrice for item opens in slot ghost\n"), DiagnosticCodes.HookSlotNotDeclared)
        .ShouldEqual("Hook 'x' opens in slot 'ghost', which the template offering it does not declare");

    [Fact] void should_name_the_migration_step_of_a_retired_form() => Message("module M\n  feature F\n    toolbar actions\n      button go title \"Go\"\n", DiagnosticCodes.RemovedFeatureToolbar)
        .ShouldEqual("'toolbar actions' in a feature is written 'outlet actions extends' since Screenplay 6 - an item list a template offers is an outlet; run 'screenplay migrate --from 5 --to 6' (the slots-and-outlets step) to rewrite the model");
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_CommandHandlerValidator;

public class when_validating_composed_command_parts : Specification
{
    CommandSyntax _pending;
    CommandSyntax _attached;

    void Establish()
    {
        var application = new ScreenplayCompiler().Parse("module M\n  feature F\n    slice StateChange S\n      command C\n        handler\n          implementation\n      command C\n        handler\n          file C.cs").Value!;
        var commands = application.Modules.Single().Features.Single().Slices.Single().Commands.ToArray();
        _pending = commands[0];
        _attached = commands[1];
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    void should_report_the_second_handler(bool attachedFirst, bool bothPending)
    {
        var context = ParserContext.ForDiagnostics();
        CommandSyntax[] commands = attachedFirst ? [_attached, _pending] : [_pending, bothPending ? _pending : _attached];
        CommandHandlerValidator.Validate(commands, context);
        context.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.PendingIntentWithOtherHandler);
        context.Diagnostics.Single().Location.ShouldEqual(commands[1].Handler!.Location);
    }

    [Fact]
    void should_not_confuse_different_commands()
    {
        var context = ParserContext.ForDiagnostics();
        CommandHandlerValidator.Validate([_pending, _attached with { Name = "Other" }], context);
        context.Diagnostics.ShouldBeEmpty();
    }
}

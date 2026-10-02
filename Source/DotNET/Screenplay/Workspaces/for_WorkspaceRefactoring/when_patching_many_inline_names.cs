// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring;

public class when_patching_many_inline_names
{
    [Theory]
    [InlineData(100)]
    [InlineData(200)]
    void should_preserve_exact_trivia_and_coalesce_shared_header_patches(int count)
    {
        var source = "// retained application comment\r\nmodule Projects\r\n  feature Recording\r\n    slice StateChange Record\r\n      command Record\r\n        recordId Uuid identifier\r\n        name String\r\n" +
            string.Join("\r\n", Enumerable.Range(0, count).Select(index => $"        produces event Recorded{index} // retained header\r\n          name String = name // retained mapping")) + "\r\n";
        var application = new ScreenplayCompiler().Parse(source).Value!;
        var module = application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        var command = slice.Commands.Single();
        var intended = application with
        {
            Modules = [module with
            {
                Features = [feature with
                {
                    Slices = [slice with
                    {
                        Commands = [command with
                        {
                            Produces = [.. command.Produces.Select(production => production with
                            {
                                Event = production.Event.Replace("Recorded", "Finished", StringComparison.Ordinal),
                                InlineEvent = production.InlineEvent! with { Name = production.InlineEvent!.Name.Replace("Recorded", "Finished", StringComparison.Ordinal) }
                            })]
                        }]
                    }]
                }]
            }]
        };
        var document = WorkspaceDocument.Create("events", PortablePlayPath.Parse("events.play"), Encoding.UTF8.GetBytes(source));
        var printed = WorkspaceTriviaPrinter.Print(document, intended);
        printed.Text.ShouldEqual(source.Replace("Recorded", "Finished", StringComparison.Ordinal));
    }
}

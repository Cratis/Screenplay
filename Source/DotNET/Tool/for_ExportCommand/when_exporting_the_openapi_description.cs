// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Api;

namespace Cratis.Screenplay.Tool.for_ExportCommand;

public class when_exporting_the_openapi_description : Tool.given.a_renderable_model
{
    int _exitCode;

    void Because() => _exitCode = ToolCommands.Run(["export", Root, "--format", "openapi"], Output, Error);

    [Fact] void should_succeed() => _exitCode.ShouldEqual(0);
    [Fact] void should_write_the_library_document() => Output.ToString().ShouldEqual(Encoding.UTF8.GetString(OpenApiDocument.Write(ApiDescription.For(LibraryModel(Root, Source)))));
    [Fact] void should_describe_the_command_route() => Output.ToString().ShouldContain("\"/api/projects/registration/register-project\"");
}

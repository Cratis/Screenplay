// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Files;

namespace Cratis.Screenplay.for_PlayFileCompiler.given;

public class a_play_file_compiler : Specification
{
    protected IPlayFiles _playFiles;
    protected PlayFileCompiler _compiler;

    void Establish()
    {
        _playFiles = Substitute.For<IPlayFiles>();
        _compiler = new(_playFiles, new ScreenplayCompiler());
    }
}

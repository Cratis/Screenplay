// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Files.for_PlayFileWriter;

public class when_expanding_interleaved_slice_members : Specification
{
    const string Source =
        """
        module Sales
          feature Orders
            slice StateChange Place
              event Placed
              specification Placing
                file Specs/Placing.cs
              command Place
                produces Placed
              constraint OnlyOne
                unique event Placed
        """;

    DirectoryInfo _root;
    string _sliceFile;
    ApplicationCompilation<ApplicationSyntax> _merged;

    void Establish() => _root = Directory.CreateTempSubdirectory("playorder");

    void Because()
    {
        var application = new ScreenplayCompiler().Compile(Source).Value!;
        var writer = new PlayFileWriter();
        _sliceFile = writer.Expand(application).Single(file => file.RelativePath.EndsWith("Place.play", StringComparison.Ordinal)).Content;
        writer.WriteTo(application, _root.FullName);
        _merged = new PlayFileCompiler().CompileFolder(_root.FullName);
    }

    [Fact] void should_preserve_order_in_the_slice_file() => InOrder(_sliceFile).ShouldBeTrue();
    [Fact] void should_merge_without_errors() => _merged.Result.Success.ShouldBeTrue();
    [Fact] void should_preserve_order_after_folder_merge() => InOrder(new ScreenplayPrinter().Print(_merged.Result.Value!)).ShouldBeTrue();

    void Destroy() => _root.Delete(true);

    static bool InOrder(string printed)
    {
        var positions = new[] { "event Placed", "specification Placing", "command Place", "constraint OnlyOne" }
            .Select(declaration => printed.IndexOf(declaration, StringComparison.Ordinal)).ToArray();
        return positions.All(position => position >= 0) && positions.SequenceEqual(positions.Order());
    }
}

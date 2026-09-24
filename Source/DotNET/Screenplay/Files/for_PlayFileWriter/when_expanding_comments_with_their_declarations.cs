// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Files.for_PlayFileWriter;

public class when_expanding_comments_with_their_declarations : Specification
{
    const string Source =
        """
        // application annotation
        domain Sales
        // @public module
        module Sales // module annotation
          // @public feature
          feature Orders
            // @public slice
            slice StateChange Place
              // event annotation
              event Placed // trailing event
              // command annotation
              command Place
                produces Placed
        """;

    DirectoryInfo _root;
    Dictionary<string, string> _files;
    ApplicationCompilation<ApplicationSyntax> _merged;

    void Establish() => _root = Directory.CreateTempSubdirectory("playcomments");

    void Because()
    {
        var application = new ScreenplayCompiler().Parse(Source).Value!;
        var writer = new PlayFileWriter();
        _files = writer.Expand(application).ToDictionary(file => file.RelativePath, file => file.Content);
        writer.WriteTo(application, _root.FullName);
        _merged = new PlayFileCompiler().CompileFolder(_root.FullName);
    }

    [Fact] void should_keep_application_annotation_at_root() => _files[PlayFileWriter.RootFileName].ShouldContain("// application annotation");
    [Fact] void should_keep_module_annotation_only_in_module_file() => _files[Path.Combine("Sales", "Sales.play")].ShouldContain("// @public module");
    [Fact] void should_keep_feature_annotation_only_in_feature_file() => _files[Path.Combine("Sales", "Orders", "Orders.play")].ShouldContain("// @public feature");
    [Fact] void should_keep_slice_and_member_annotations_in_slice_file() => _files[Path.Combine("Sales", "Orders", "Place", "Place.play")].ShouldContain("// trailing event");
    [Fact] void should_merge_with_annotations() => new ScreenplayPrinter().Print(_merged.Result.Value!).ShouldContain("// @public slice");
    [Fact] void should_merge_module_comments_even_when_its_file_sorts_last() => new ScreenplayPrinter().Print(_merged.Result.Value!).ShouldContain("// @public module");

    void Destroy() => _root.Delete(true);
}

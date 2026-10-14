// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Validation.for_ValidationGateRoot;

public class when_resolving_files_and_subfolders : for_ScreenplayValidationGate.given.a_gate_file
{
    string _subfolder;
    string _file;
    string _fromFile;
    string _fromFolder;

    void Establish()
    {
        File.WriteAllText(GatePath, "{}");
        _subfolder = Path.Combine(Root, "feature");
        Directory.CreateDirectory(_subfolder);
        _file = Path.Combine(_subfolder, "slice.play");
        File.WriteAllText(_file, "module M");
    }

    void Because()
    {
        _fromFile = ValidationGateRoot.Resolve(_file);
        _fromFolder = ValidationGateRoot.Resolve(_subfolder);
    }

    [Fact] void should_start_a_file_at_its_folder() => _fromFile.ShouldEqual(Root);
    [Fact] void should_find_the_owning_gate_above_a_subfolder() => _fromFolder.ShouldEqual(Root);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Files;

/// <summary>
/// The exception that is thrown when expanding an application into a folder structure would put two
/// declarations in the same file.
/// </summary>
/// <param name="path">The path two declarations both claim.</param>
/// <remarks>
/// A folder structure names a module, feature or slice by its folder, so two of them sharing a name at the
/// same level - or differing only in casing, which a case insensitive file system cannot tell apart - have
/// nowhere separate to live. Saying so is the only honest answer: writing them anyway would silently lose one.
/// </remarks>
public class AmbiguousPlayFilePath(string path)
    : Exception($"'{path}' is claimed by more than one declaration - modules, features and slices at the same level need names a file system can tell apart");

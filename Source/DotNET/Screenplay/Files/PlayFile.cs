// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Files;

/// <summary>
/// Represents a discovered <c>.play</c> file.
/// </summary>
/// <param name="Path">The full path of the file.</param>
/// <param name="RelativePath">The path of the file relative to the searched root.</param>
public record PlayFile(string Path, string RelativePath);

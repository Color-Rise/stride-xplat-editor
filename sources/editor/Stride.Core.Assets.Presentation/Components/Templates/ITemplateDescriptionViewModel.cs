// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Templates;
using Stride.Core.IO;

namespace Stride.Core.Assets.Presentation.Components.Templates;

public interface ITemplateDescriptionViewModel
{
    Guid Id { get; }

    string Name { get; }

    string? Description { get; }

    string? FullDescription { get; }

    string? DefaultOutputName { get; }

    UFile? Icon { get; }

    List<UFile> Screenshots { get; }

    TemplateDescription? Template { get; }
}

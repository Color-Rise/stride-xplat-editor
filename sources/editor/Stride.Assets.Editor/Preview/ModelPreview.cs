// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Models;
using Stride.Editor.Annotations;
using Stride.Editor.Preview;
using Stride.Engine;
using Stride.Rendering;

namespace Stride.Assets.Editor.Preview;

/// <summary>
/// An implementation of the <see cref="AssetPreview"/> that can preview models.
/// </summary>
[AssetPreview<ModelAsset>]
public class ModelPreview : PreviewFromEntity<ModelAsset>
{
    /// <inheritdoc/>
    protected override PreviewEntity CreatePreviewEntity()
    {
        var modelLocation = AssetItem.Location;
        // load the created material and the model from the data base
        var model = LoadAsset<Model>(modelLocation);

        // create the entity, create and set the model component
        var entity = new Entity { Name = "Preview Entity of model: " + modelLocation };
        entity.Add(new ModelComponent { Model = model });

        var previewEntity = new PreviewEntity(entity);

        previewEntity.Disposed += () => UnloadAsset(model);

        return previewEntity;
    }
}

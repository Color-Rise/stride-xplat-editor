// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.GameEditor;
using Stride.Assets.Editor.GameEditor.Services;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Assets;
using Stride.Core.Mathematics;
using Stride.Core.Settings;
using Stride.Engine;
using Stride.Engine.Processors;

namespace Stride.Assets.Editor.SceneEditor;

[DataContract(nameof(SceneSettingsData))]
public sealed class SceneSettingsData
{
    // Camera view parameters
    public Vector3 CamPosition = EditorGameCameraService.DefaultPosition;
    public Vector2 CamPitchYaw = new(EditorGameCameraService.DefaultPitch, EditorGameCameraService.DefaultYaw);

    // Camera projection parameters
    public CameraProjectionMode CamProjection = CameraProjectionMode.Perspective;
    public float CamVerticalFieldOfView = CameraComponent.DefaultVerticalFieldOfView;
    public float CamOrthographicSize = CameraComponent.DefaultOrthographicSize;
    public float CamNearClipPlane = CameraComponent.DefaultNearClipPlane;
    public float CamFarClipPlane = CameraComponent.DefaultFarClipPlane;
    public float CamAspectRatio = CameraComponent.DefaultAspectRatio;
    public float CamMoveSpeed = EditorGameCameraService.DefaultMoveSpeed;

    // Snapping
    public bool TranslationSnapActive = false;
    public float TranslationSnapValue = 1.0f;
    public bool RotationSnapActive = false;
    public float RotationSnapValue = 22.5f;
    public bool ScaleSnapActive = false;
    public float ScaleSnapValue = 1.1f;
    public float SceneUnit = 1.0f;

    // Gizmos
    public bool SelectionMaskVisible = true;
    public bool GridVisible = true;
    public Color3 GridColor = (Color3)new Color(180, 180, 180);
    public float GridOpacity = 0.35f;
    public int GridAxisIndex = (int)ViewportGridAxis.Y;
    public bool CameraPreviewVisible = true;
    public RenderMode RenderMode = RenderMode.SingleStream;
    public string ActiveStream = string.Empty;
    public double TransformationGizmoSize = 1.0;
    public double ComponentGizmoSize = 1.0;
    public bool FixedSizeGizmos = false;
    public List<string> HiddenGizmos = new();
    public List<Guid> VisibleNavigationGroups = new();
    public bool LightProbeWireframe = false;
    public int LightProbeBounces = 1;

    // Scene
    /// <summary>
    /// Indicates whether the scene will be automatically loaded when opened in the editor.
    /// </summary>
    /// <remarks>
    /// This settings only make sense in the context of a scene hierarchy. A single scene is always loaded.
    /// </remarks>
    public bool SceneLoaded = false;

    /// <summary>
    /// Creates a new instance of the default scene settings.
    /// </summary>
    [NotNull]
    public static SceneSettingsData CreateDefault()
    {
        return new SceneSettingsData
        {
            HiddenGizmos = new List<string>
            {
                DisplayAttribute.GetDisplayName(typeof(TransformComponent)),
                DisplayAttribute.GetDisplayName(typeof(PhysicsComponent)),
                DisplayAttribute.GetDisplayName(typeof(Stride.Physics.PhysicsConstraintComponent)),
            }
        };
    }
}

public static class PackageSceneSettings
{
    public static SettingsKey<Dictionary<AssetId, SceneSettingsData>> SceneSettings = new("Package/SceneSettings",
        PackageUserSettings.SettingsContainer, () => new Dictionary<AssetId, SceneSettingsData>());
}

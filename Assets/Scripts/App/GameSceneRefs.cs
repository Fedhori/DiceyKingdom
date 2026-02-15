using System;
using UnityEngine;

[Serializable]
/// <summary>
/// Core class that defines game scene refs responsibilities.
/// </summary>
public sealed class GameSceneRefs
{
    [SerializeField] Transform sceneRoot;

    public Transform SceneRoot => sceneRoot;
}


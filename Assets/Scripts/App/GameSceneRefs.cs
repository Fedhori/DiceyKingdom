using System;
using UnityEngine;

[Serializable]
public sealed class GameSceneRefs
{
    [SerializeField] Transform sceneRoot;

    public Transform SceneRoot => sceneRoot;
}

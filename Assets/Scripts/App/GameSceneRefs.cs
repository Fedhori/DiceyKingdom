using System;
using UnityEngine;

namespace Game.App
{
    [Serializable]



    public sealed class GameSceneRefs
    {
        [SerializeField] Transform sceneRoot;

        public Transform SceneRoot => sceneRoot;
    }

}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Extreal.Core.SceneTransition
{
    /// <summary>
    /// Class for a scene
    /// </summary>
    /// <typeparam name="TScene">Enum for scene names</typeparam>
    /// <typeparam name="TUnityScene">Enum for Unity scene names</typeparam>
    [Serializable]
    public class Scene<TScene, TUnityScene>
        where TScene : struct
        where TUnityScene : struct
    {
        [SerializeField] private TScene sceneName;
        [SerializeField] private List<TUnityScene> unitySceneNames;

        public TScene SceneName => sceneName;
        public List<TUnityScene> UnitySceneNames => unitySceneNames;
    }
}

using System;
using System.Collections.Generic;

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
        public TScene sceneName;
        public List<TUnityScene> unitySceneNames = new List<TUnityScene>();
    }
}

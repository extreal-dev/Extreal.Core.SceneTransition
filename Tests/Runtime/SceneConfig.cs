using System.Collections.Generic;
using UnityEngine;

namespace Extreal.Core.SceneTransition.Test
{
    [CreateAssetMenu(
        menuName = "Config/" + nameof(SceneConfig),
        fileName = nameof(SceneConfig))]
    public class SceneConfig : ScriptableObject, ISceneConfig<SceneName, UnitySceneName>
    {
        [SerializeField] private List<UnitySceneName> _commonUnitySceneNames;
        [SerializeField] private List<Scene<SceneName, UnitySceneName>> _scenes;

        public List<UnitySceneName> CommonUnitySceneNames => this._commonUnitySceneNames;
        public List<Scene<SceneName, UnitySceneName>> Scenes => _scenes;
    }
}

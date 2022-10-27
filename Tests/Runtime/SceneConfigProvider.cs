using UnityEngine;

namespace Extreal.Core.SceneTransition.Test
{
    public class SceneConfigProvider : MonoBehaviour
    {
        [SerializeField] private SceneConfig sceneConfig;
        [SerializeField] private SceneConfig emptySceneConfig;

        public SceneConfig SceneConfig => sceneConfig;
        public SceneConfig EmptySceneConfig => emptySceneConfig;
    }
}

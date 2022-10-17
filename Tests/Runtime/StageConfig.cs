using System.Collections.Generic;
using UnityEngine;

namespace Extreal.Core.StageNavigation.Test
{
    [CreateAssetMenu(
        menuName = "Config/" + nameof(StageConfig),
        fileName = nameof(StageConfig))]
    public class StageConfig : ScriptableObject, IStageConfig<StageName, SceneName>
    {
        [SerializeField] private List<SceneName> commonUnitySceneNames;
        [SerializeField] private List<Stage<StageName, SceneName>> scenes;

        public List<SceneName> CommonScenes => commonUnitySceneNames;
        public List<Stage<StageName, SceneName>> Stages => scenes;
    }
}

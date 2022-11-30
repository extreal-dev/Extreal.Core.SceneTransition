using System.Collections.Generic;
using UnityEngine;

namespace Extreal.Core.StageNavigation
{
    public class StageConfigBase<TStage, TScene> : ScriptableObject, IStageConfig<TStage, TScene>
        where TStage : struct
        where TScene : struct
    {
        [SerializeField] private List<TScene> commonScenes;
        [SerializeField] private List<Stage<TStage, TScene>> stages;

        public List<TScene> CommonScenes => commonScenes;
        public List<Stage<TStage, TScene>> Stages => stages;
    }
}

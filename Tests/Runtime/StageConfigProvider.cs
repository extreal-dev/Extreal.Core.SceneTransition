using UnityEngine;

namespace Extreal.Core.StageNavigation.Test
{
    public class StageConfigProvider : MonoBehaviour
    {
        [SerializeField] private StageConfig stageConfig;
        [SerializeField] private StageConfig emptyStageConfig;

        public StageConfig StageConfig => stageConfig;
        public StageConfig EmptyStageConfig => emptyStageConfig;
    }
}

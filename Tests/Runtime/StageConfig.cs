using UnityEngine;

namespace Extreal.Core.StageNavigation.Test
{
    [CreateAssetMenu(
        menuName = "Config/" + nameof(StageConfig),
        fileName = nameof(StageConfig))]
    public class StageConfig : StageConfigBase<StageName, SceneName> { }
}

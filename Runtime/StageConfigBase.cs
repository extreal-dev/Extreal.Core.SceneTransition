using System.Collections.Generic;
using UnityEngine;

namespace Extreal.Core.StageNavigation
{
    /// <summary>
    /// Base class for the stage configuration.
    /// </summary>
    /// <typeparam name="TStage">Enum for stage names.</typeparam>
    /// <typeparam name="TScene">Enum for scene names.</typeparam>
    public class StageConfigBase<TStage, TScene> : ScriptableObject, IStageConfig<TStage, TScene>
        where TStage : struct
        where TScene : struct
    {
        [SerializeField] private List<TScene> commonScenes;
        [SerializeField] private List<Stage<TStage, TScene>> stages;

        /// <inheritdoc/>
        public List<TScene> CommonScenes => commonScenes;

        /// <inheritdoc/>
        public List<Stage<TStage, TScene>> Stages => stages;
    }
}

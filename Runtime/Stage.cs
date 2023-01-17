using System;
using System.Collections.Generic;
using UnityEngine;

namespace Extreal.Core.StageNavigation
{
    /// <summary>
    /// Class for a stage.
    /// </summary>
    /// <typeparam name="TStage">Enum for stage names.</typeparam>
    /// <typeparam name="TScene">Enum for scene names.</typeparam>
    [Serializable]
    public class Stage<TStage, TScene>
        where TStage : struct
        where TScene : struct
    {
        [SerializeField] private TStage stageName;
        [SerializeField] private List<TScene> sceneNames;

        /// <summary>
        /// Stage name to be set.
        /// </summary>
        public TStage StageName => stageName;

        /// <summary>
        /// Stage names included in this stage.
        /// </summary>
        public List<TScene> SceneNames => sceneNames;
    }
}

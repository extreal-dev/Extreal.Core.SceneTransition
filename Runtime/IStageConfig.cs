using System.Collections.Generic;

namespace Extreal.Core.StageNavigation
{
    /// <summary>
    /// Interface for holding stage configuration
    /// </summary>
    /// <typeparam name="TStage">Enum for stage names</typeparam>
    /// <typeparam name="TScene">Enum for scene names</typeparam>
    public interface IStageConfig<TStage, TScene>
        where TStage : struct
        where TScene : struct
    {
        /// <summary>
        /// Common scene names for all stages
        /// </summary>
        /// <value>Common scene names for all stages</value>
        List<TScene> CommonScenes { get; }

        /// <summary>
        /// Stage configuration
        /// </summary>
        /// <value>Stage configuration</value>
        List<Stage<TStage, TScene>> Stages { get; }
    }
}

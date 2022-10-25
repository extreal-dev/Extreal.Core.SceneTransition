using System.Collections.Generic;

namespace Extreal.Core.SceneTransition
{
    /// <summary>
    /// Interface for holding scene configuration
    /// </summary>
    /// <typeparam name="TScene">Enum for scene names</typeparam>
    /// <typeparam name="TUnityScene">Enum for Unity scene names</typeparam>
    public interface ISceneConfig<TScene, TUnityScene>
        where TScene : struct
        where TUnityScene : struct
    {
        /// <summary>
        /// Common Unity scene names for all scenes
        /// </summary>
        /// <value>Common Unity scene names for all scenes</value>
        List<TUnityScene> CommonUnitySceneNames { get; }

        /// <summary>
        /// Scene configuration
        /// </summary>
        /// <value>Scene configuration</value>
        List<Scene<TScene, TUnityScene>> Scenes { get; }
    }
}

using System;
using System.Collections.Generic;

namespace Extreal.Core.SceneTransition
{
    /// <summary>
    /// Interface for implementation used for scene transition
    /// </summary>
    /// <typeparam name="TPage">Enum defining page names</typeparam>
    /// <typeparam name="TScene">Enum defining scene names</typeparam>
    public interface ISceneTransitionConfiguration<TPage, TScene>
        where TPage : struct
        where TScene : struct
    {
        /// <summary>
        /// Uses to load pages as permanent
        /// </summary>
        /// <value>Page names that permanently exist</value>
        List<TPage> PermanentNames { get; }

        /// <summary>
        /// Uses to load pages tied to scene name
        /// </summary>
        /// <value></value>
        List<SceneConfiguration<TPage, TScene>> Scenes { get; }
    }

    /// <summary>
    /// Class that defines which scene contains which pages
    /// </summary>
    /// <typeparam name="TPage">Enum defining page names</typeparam>
    /// <typeparam name="TScene">Enum defining scene names</typeparam>
    [Serializable]
    public class SceneConfiguration<TPage, TScene>
        where TPage : struct
        where TScene : struct
    {
        public TScene _sceneName;
        public List<TPage> _pageNames = new List<TPage>();
    }
}

using System;
using System.Collections.Generic;

namespace Extreal.Core.SceneTransition
{
    public interface ISceneTransitionConfiguration<TPage, TScene>
        where TPage : struct
        where TScene : struct
    {
        List<TPage> PermanentNames { get; }
        List<SceneConfiguration<TPage, TScene>> Scenes { get; }
    }

    [Serializable]
    public class SceneConfiguration<TPage, TScene>
        where TPage : struct
        where TScene : struct
    {
        public TScene _sceneName;
        public List<TPage> _pageNames = new List<TPage>();
    }
}

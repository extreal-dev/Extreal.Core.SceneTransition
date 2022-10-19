using System.Collections.Generic;
using UnityEngine;

namespace Extreal.Core.SceneTransition
{
    [CreateAssetMenu(
        menuName = "Extreal/Configuration/" + nameof(SceneTransitionConfiguration),
        fileName = nameof(SceneTransitionConfiguration))]
    public class SceneTransitionConfiguration : ScriptableObject
    {
        public List<string> _permanentNames = new List<string>();
        public List<PageConfiguration> _pages = new List<PageConfiguration>();
    }

    public class PageConfiguration
    {
        public string _pageName;
        public List<string> _sceneNames = new List<string>();
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace Extreal.Core.SceneTransition.Test
{
    [CreateAssetMenu(
        menuName = "Configuration/" + nameof(SceneTransitionConfiguration),
        fileName = nameof(SceneTransitionConfiguration))]
    public class SceneTransitionConfiguration : ScriptableObject, ISceneTransitionConfiguration<PageName, SceneName>
    {
        [SerializeField] private List<PageName> _permanentNames;
        [SerializeField] private List<SceneConfiguration<PageName, SceneName>> _scenes;

        public List<PageName> PermanentNames => _permanentNames;
        public List<SceneConfiguration<PageName, SceneName>> Scenes => _scenes;
    }
}

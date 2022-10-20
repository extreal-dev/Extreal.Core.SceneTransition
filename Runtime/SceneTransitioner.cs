using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace Extreal.Core.SceneTransition
{
    public class SceneTransitioner<TPage, TScene> : ISceneTransitioner<TScene>
        where TPage : struct
        where TScene : struct
    {
        public event Action<TScene> OnSceneChanged;

        private Dictionary<TScene, TPage[]> _sceneMap = new Dictionary<TScene, TPage[]>();
        private Stack<TScene> _sceneHistory = new Stack<TScene>();
        private List<TPage> _loadedPages = new List<TPage>();
        private bool _initialTransition = true;
        private TScene _currentScene;

        public SceneTransitioner(ISceneTransitionConfiguration<TPage, TScene> configuration)
        {
            foreach (var scene in configuration.Scenes)
            {
                _sceneMap[scene._sceneName] = scene._pageNames.ToArray();
            }

            foreach (var permanentName in configuration.PermanentNames)
            {
                _ = SceneManager.LoadSceneAsync(permanentName.ToString());
            }
        }

        public async UniTask ReplaceAsync(TScene scene)
        {
            if (_initialTransition)
            {
                _initialTransition = false;
            }

            await UnloadScenesAsync(scene);
            await LoadScenesAsync(scene);

            _currentScene = scene;
            OnSceneChanged?.Invoke(_currentScene);
        }

        public async UniTask PushAsync(TScene scene)
        {
            if (!_initialTransition)
            {
                _sceneHistory.Push(_currentScene);
            }
            else
            {
                _initialTransition = false;
            }

            await UnloadScenesAsync(scene);
            await LoadScenesAsync(scene);

            _currentScene = scene;
            OnSceneChanged?.Invoke(_currentScene);
        }

        public async UniTask PopAsync()
        {
            if (_sceneHistory.Count == 0)
            {
                return;
            }

            _currentScene = _sceneHistory.Pop();
            await UnloadScenesAsync(_currentScene);
            await LoadScenesAsync(_currentScene);

            OnSceneChanged?.Invoke(_currentScene);
        }

        public void Reset()
        {
            _sceneHistory.Clear();
        }

        private async UniTask UnloadScenesAsync(TScene scene)
        {
            var asyncOps = new List<UnityEngine.AsyncOperation>();
            for (var i = _loadedPages.Count - 1; i >= 0; i--)
            {
                var pageEnum = _loadedPages[i];
                if (!_sceneMap[scene].Contains(pageEnum))
                {
                    var asyncOp = SceneManager.UnloadSceneAsync(pageEnum.ToString());
                    asyncOps.Add(asyncOp);
                    _loadedPages.RemoveAt(i);
                }
            }

            await UniTask.WaitUntil(() => IsDoneAllOperation(asyncOps));
        }

        private async UniTask LoadScenesAsync(TScene scene)
        {
            var asyncOps = new List<UnityEngine.AsyncOperation>();
            foreach (var pageEnum in _sceneMap[scene])
            {
                if (!_loadedPages.Contains(pageEnum))
                {
                    var asyncOp = SceneManager.LoadSceneAsync(pageEnum.ToString(), LoadSceneMode.Additive);
                    asyncOps.Add(asyncOp);
                    _loadedPages.Add(pageEnum);
                }
            }

            await UniTask.WaitUntil(() => IsDoneAllOperation(asyncOps));
        }

        private bool IsDoneAllOperation(List<UnityEngine.AsyncOperation> asyncOps)
        {
            var isDone = true;
            for (var i = asyncOps.Count - 1; i >= 0; i--)
            {
                if (asyncOps[i].isDone)
                {
                    asyncOps.RemoveAt(i);
                }
                else
                {
                    isDone = false;
                }
            }
            return isDone;
        }
    }
}

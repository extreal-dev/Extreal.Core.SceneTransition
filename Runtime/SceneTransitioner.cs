using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace Extreal.Core.SceneTransition
{
    public class SceneTransitioner<TScene, TPage> : ISceneTransitioner<TPage>
        where TScene : struct
        where TPage : struct
    {
        public event Action<TPage> OnPageChanged;

        private Dictionary<TPage, TScene[]> _pageMap = new Dictionary<TPage, TScene[]>();
        private Stack<TPage> _pageHistory = new Stack<TPage>();
        private List<TScene> _loadedScenes = new List<TScene>();

        public SceneTransitioner(SceneTransitionConfiguration configuration)
        {
            foreach (var page in configuration._pages)
            {
                if (EnumTryParseAndIsDefined<TPage>(page._pageName, out var pageEnum))
                {
                    var pageScenes = new List<TScene>();
                    foreach (var sceneName in page._sceneNames)
                    {
                        if (EnumTryParseAndIsDefined<TScene>(sceneName, out var sceneEnum))
                        {
                            pageScenes.Add(sceneEnum);
                        }
                    }
                    _pageMap[pageEnum] = pageScenes.ToArray();
                }
            }

            foreach (var permanentName in configuration._permanentNames)
            {
                if (EnumTryParseAndIsDefined<TScene>(permanentName, out var _))
                {
                    _ = SceneManager.LoadSceneAsync(permanentName);
                }
            }
        }

        public async UniTask ReplaceAsync(TPage page)
        {
            if (_pageHistory.Count > 0)
            {
                _pageHistory.Pop();
            }
            _pageHistory.Push(page);

            await UnloadScenesAsync(page);
            await LoadScenesAsync(page);

            OnPageChanged?.Invoke(page);
        }

        public async UniTask PushAsync(TPage page)
        {
            _pageHistory.Push(page);

            await UnloadScenesAsync(page);
            await LoadScenesAsync(page);

            OnPageChanged?.Invoke(page);
        }

        public async UniTask PopAsync()
        {
            if (_pageHistory.Count == 0)
            {
                return;
            }

            var page = _pageHistory.Pop();
            await UnloadScenesAsync(page);
            await LoadScenesAsync(page);

            OnPageChanged?.Invoke(page);
        }

        public void Reset()
        {
            _pageHistory.Clear();
        }

        private async UniTask UnloadScenesAsync(TPage page)
        {
            var syncOps = new List<UnityEngine.AsyncOperation>();
            for (var i = _loadedScenes.Count - 1; i >= 0; i--)
            {
                var sceneEnum = _loadedScenes[i];
                if (!_pageMap[page].Contains(sceneEnum))
                {
                    var syncOp = SceneManager.UnloadSceneAsync(sceneEnum.ToString());
                    syncOps.Add(syncOp);
                    _loadedScenes.RemoveAt(i);
                }
            }

            await UniTask.WaitUntil(() =>
            {
                var isDone = true;
                for (var i = syncOps.Count - 1; i >= 0; i--)
                {
                    if (syncOps[i].isDone)
                    {
                        syncOps.RemoveAt(i);
                    }
                    else
                    {
                        isDone = false;
                    }
                }
                return isDone;
            });
        }

        private async UniTask LoadScenesAsync(TPage page)
        {
            var asyncOps = new List<UnityEngine.AsyncOperation>();
            foreach (var sceneEnum in _pageMap[page])
            {
                if (!_loadedScenes.Contains(sceneEnum))
                {
                    var asyncOp = SceneManager.LoadSceneAsync(sceneEnum.ToString(), LoadSceneMode.Additive);
                    asyncOps.Add(asyncOp);
                    _loadedScenes.Add(sceneEnum);
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

        private bool EnumTryParseAndIsDefined<TEnum>(string value, out TEnum result) where TEnum : struct
        {
            return Enum.TryParse(value, out result) && Enum.IsDefined(typeof(TEnum), result);
        }
    }
}

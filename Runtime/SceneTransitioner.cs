using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Cysharp.Threading.Tasks;
using Extreal.Core.Logging;
using UnityEngine.SceneManagement;

namespace Extreal.Core.SceneTransition
{
    /// <summary>
    /// Class used to transition scenes
    /// </summary>
    /// <typeparam name="TScene">Enum for scene names</typeparam>
    /// <typeparam name="TUnityScene">Enum for Unity scene names</typeparam>
    public class SceneTransitioner<TScene, TUnityScene> : ISceneTransitioner<TScene>
        where TScene : struct
        where TUnityScene : struct
    {
        private static readonly ELogger LOGGER
            = LoggingManager.GetLogger(nameof(SceneTransitioner<TScene, TUnityScene>));
        
        /// <summary>
        /// Invokes when scene is changed
        /// </summary>
        public event Action<TScene> OnSceneTransitioned;

        private Dictionary<TScene, TUnityScene[]> _sceneMap = new Dictionary<TScene, TUnityScene[]>();
        private Stack<TScene> _sceneHistory = new Stack<TScene>();
        private List<TUnityScene> _loadedUnityScenes = new List<TUnityScene>();
        private bool _initialTransition = true;
        private TScene _currentScene;

        /// <summary>
        /// Creates a new SceneTransitioner with given configuration
        /// </summary>
        /// <param name="config">Scene configuration</param>
        public SceneTransitioner(ISceneConfig<TScene, TUnityScene> config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }
            if (isEmpty(config.Scenes))
            {
                throw new ArgumentException("scene config requires at least one scene");
            }
            
            foreach (var scene in config.Scenes)
            {
                _sceneMap[scene._sceneName] = scene._unitySceneNames.ToArray();
            }

            foreach (var permanentName in config.CommonUnitySceneNames)
            {
                _ = SceneManager.LoadSceneAsync(permanentName.ToString(), LoadSceneMode.Additive);
            }
        }

        private bool isEmpty(List<Scene<TScene, TUnityScene>> scenes)
        {
            return scenes == null || scenes.Count == 0;
        }

        /// <summary>
        /// Transitions scene without leaving scene transition history
        /// </summary>
        /// <param name="scene">Scene Name to transition to</param>
        /// <returns>UniTask of this method</returns>
        public async UniTask ReplaceAsync(TScene scene)
        {
            if (_initialTransition)
            {
                _initialTransition = false;
            }

            await UnloadScenesAsync(scene);
            await LoadScenesAsync(scene);

            _currentScene = scene;
            Debug(Operation.Replace, _currentScene);
            this.OnSceneTransitioned?.Invoke(_currentScene);
        }
        
        private enum Operation
        {
            Replace,
            Push,
            Pop,
            Reset,
        }

        private void Debug(Operation operation, TScene scene)
        {
            if (LOGGER.IsDebug())
            {
                LOGGER.LogDebug($"{operation}: {scene}");
            }
        }

        private void Debug(Operation operation)
        {
            if (LOGGER.IsDebug())
            {
                LOGGER.LogDebug(operation.ToString());
            }
        }

        /// <summary>
        /// Transitions scene with leaving scene transition history
        /// </summary>
        /// <param name="scene">Scene Name to transition to</param>
        /// <returns>UniTask of this method</returns>
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
            Debug(Operation.Push, _currentScene);
            this.OnSceneTransitioned?.Invoke(_currentScene);
        }

        /// <summary>
        /// Transitions back according to scene transition history
        /// </summary>
        /// <returns>UniTask of this method</returns>
        public async UniTask PopAsync()
        {
            if (_sceneHistory.Count == 0)
            {
                throw new InvalidOperationException("there is no scene transition history");
            }

            _currentScene = _sceneHistory.Pop();
            await UnloadScenesAsync(_currentScene);
            await LoadScenesAsync(_currentScene);
            
            Debug(Operation.Pop, _currentScene);
            this.OnSceneTransitioned?.Invoke(_currentScene);
        }

        /// <summary>
        /// Resets scene transition history
        /// </summary>
        public void Reset()
        {
            _sceneHistory.Clear();
            Debug(Operation.Reset);
        }

        private async UniTask UnloadScenesAsync(TScene scene)
        {
            var asyncOps = new List<UnityEngine.AsyncOperation>();
            for (var i = this._loadedUnityScenes.Count - 1; i >= 0; i--)
            {
                var pageEnum = this._loadedUnityScenes[i];
                if (!_sceneMap[scene].Contains(pageEnum))
                {
                    var asyncOp = SceneManager.UnloadSceneAsync(pageEnum.ToString());
                    asyncOps.Add(asyncOp);
                    this._loadedUnityScenes.RemoveAt(i);
                }
            }

            await UniTask.WaitUntil(() => IsDoneAllOperation(asyncOps));
        }

        private async UniTask LoadScenesAsync(TScene scene)
        {
            var asyncOps = new List<UnityEngine.AsyncOperation>();
            foreach (var pageEnum in _sceneMap[scene])
            {
                if (!this._loadedUnityScenes.Contains(pageEnum))
                {
                    var asyncOp = SceneManager.LoadSceneAsync(pageEnum.ToString(), LoadSceneMode.Additive);
                    asyncOps.Add(asyncOp);
                    this._loadedUnityScenes.Add(pageEnum);
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

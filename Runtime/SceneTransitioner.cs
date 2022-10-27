using System;
using System.Collections.Generic;
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
        private static readonly ELogger Logger
            = LoggingManager.GetLogger(nameof(SceneTransitioner<TScene, TUnityScene>));

        /// <summary>
        /// Invokes when scene is changed
        /// </summary>
        public event Action<TScene> OnSceneTransitioned;

        private readonly Dictionary<TScene, TUnityScene[]> sceneMap = new Dictionary<TScene, TUnityScene[]>();
        private readonly Stack<TScene> sceneHistory = new Stack<TScene>();
        private readonly List<TUnityScene> loadedUnityScenes = new List<TUnityScene>();
        private bool initialTransition = true;
        private TScene currentScene;

        /// <summary>
        /// Creates a new SceneTransitioner with given configuration
        /// </summary>
        /// <exception cref="ArgumentNullException">If config is null</exception>
        /// <exception cref="ArgumentException">If config contains no scenes</exception>
        /// <param name="config">Scene configuration</param>
        public SceneTransitioner(ISceneConfig<TScene, TUnityScene> config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }
            if (IsEmpty(config.Scenes))
            {
                throw new ArgumentException("scene config requires at least one scene");
            }

            foreach (var scene in config.Scenes)
            {
                sceneMap[scene.SceneName] = scene.UnitySceneNames.ToArray();
            }

            foreach (var permanentName in config.CommonUnitySceneNames)
            {
                _ = SceneManager.LoadSceneAsync(permanentName.ToString(), LoadSceneMode.Additive);
            }
        }

        /// <summary>
        /// Transitions scene without leaving scene transition history
        /// </summary>
        /// <param name="scene">Scene Name to transition to</param>
        /// <returns>UniTask of this method</returns>
        public async UniTask ReplaceAsync(TScene scene)
        {
            if (initialTransition)
            {
                initialTransition = false;
            }

            await UnloadScenesAsync(scene);
            await LoadScenesAsync(scene);

            currentScene = scene;
            Debug(Operation.Replace, currentScene);
            OnSceneTransitioned?.Invoke(currentScene);
        }

        /// <summary>
        /// Transitions scene with leaving scene transition history
        /// </summary>
        /// <param name="scene">Scene Name to transition to</param>
        /// <returns>UniTask of this method</returns>
        public async UniTask PushAsync(TScene scene)
        {
            if (!initialTransition)
            {
                sceneHistory.Push(currentScene);
            }
            else
            {
                initialTransition = false;
            }

            await UnloadScenesAsync(scene);
            await LoadScenesAsync(scene);

            currentScene = scene;
            Debug(Operation.Push, currentScene);
            OnSceneTransitioned?.Invoke(currentScene);
        }

        /// <summary>
        /// Transitions back according to scene transition history
        /// </summary>
        /// <returns>UniTask of this method</returns>
        public async UniTask PopAsync()
        {
            if (sceneHistory.Count == 0)
            {
                throw new InvalidOperationException("there is no scene transition history");
            }

            currentScene = sceneHistory.Pop();
            await UnloadScenesAsync(currentScene);
            await LoadScenesAsync(currentScene);

            Debug(Operation.Pop, currentScene);
            OnSceneTransitioned?.Invoke(currentScene);
        }

        /// <summary>
        /// Resets scene transition history
        /// </summary>
        public void Reset()
        {
            sceneHistory.Clear();
            Debug(Operation.Reset);
        }

        private async UniTask UnloadScenesAsync(TScene scene)
        {
            var asyncOps = new List<UnityEngine.AsyncOperation>();
            for (var i = loadedUnityScenes.Count - 1; i >= 0; i--)
            {
                var pageEnum = loadedUnityScenes[i];
                if (!sceneMap[scene].Contains(pageEnum))
                {
                    var asyncOp = SceneManager.UnloadSceneAsync(pageEnum.ToString());
                    asyncOps.Add(asyncOp);
                    loadedUnityScenes.RemoveAt(i);
                }
            }

            await UniTask.WaitUntil(() => IsDoneAllOperation(asyncOps));
        }

        private async UniTask LoadScenesAsync(TScene scene)
        {
            var asyncOps = new List<UnityEngine.AsyncOperation>();
            foreach (var pageEnum in sceneMap[scene])
            {
                if (!loadedUnityScenes.Contains(pageEnum))
                {
                    var asyncOp = SceneManager.LoadSceneAsync(pageEnum.ToString(), LoadSceneMode.Additive);
                    asyncOps.Add(asyncOp);
                    loadedUnityScenes.Add(pageEnum);
                }
            }

            await UniTask.WaitUntil(() => IsDoneAllOperation(asyncOps));
        }

        private static bool IsEmpty(List<Scene<TScene, TUnityScene>> scenes)
            => scenes == null || scenes.Count == 0;

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

        private static void Debug(Operation operation, TScene scene)
        {
            if (Logger.IsDebug())
            {
                Logger.LogDebug($"{operation}: {scene}");
            }
        }

        private static void Debug(Operation operation)
        {
            if (Logger.IsDebug())
            {
                Logger.LogDebug(operation.ToString());
            }
        }

        private enum Operation
        {
            Replace,
            Push,
            Pop,
            Reset,
        }
    }
}

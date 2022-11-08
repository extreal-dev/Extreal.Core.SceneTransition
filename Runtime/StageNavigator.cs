using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Extreal.Core.Logging;
using UnityEngine.SceneManagement;

namespace Extreal.Core.StageNavigation
{
    /// <summary>
    /// Class used to transition stages
    /// </summary>
    /// <typeparam name="TStage">Enum for stage names</typeparam>
    /// <typeparam name="TScene">Enum for scene names</typeparam>
    public class StageNavigator<TStage, TScene> : IStageNavigator<TStage>
        where TStage : struct
        where TScene : struct
    {
        private static readonly ELogger Logger
            = LoggingManager.GetLogger(nameof(StageNavigator<TStage, TScene>));

        /// <inheritdoc/>
        public event Action<TStage> OnStageTransitioning;

        /// <inheritdoc/>
        public event Action<TStage> OnStageTransitioned;

        private readonly Dictionary<TStage, TScene[]> stageMap = new Dictionary<TStage, TScene[]>();
        private readonly Stack<TStage> stageHistory = new Stack<TStage>();
        private readonly List<TScene> loadedScenes = new List<TScene>();
        private bool initialTransition = true;
        private TStage currentStage;

        /// <summary>
        /// Creates a new StageNavigator with given configuration
        /// </summary>
        /// <exception cref="ArgumentNullException">If config is null</exception>
        /// <exception cref="ArgumentException">If config contains no stages</exception>
        /// <param name="config">Stage configuration</param>
        public StageNavigator(IStageConfig<TStage, TScene> config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }
            if (IsEmpty(config.Stages))
            {
                throw new ArgumentException("stage config requires at least one stage");
            }

            foreach (var stage in config.Stages)
            {
                stageMap[stage.StageName] = stage.SceneNames.ToArray();
            }

            foreach (var commonScene in config.CommonScenes)
            {
                _ = SceneManager.LoadSceneAsync(commonScene.ToString(), LoadSceneMode.Additive);
            }
        }

        /// <inheritdoc/>
        public async UniTask ReplaceAsync(TStage stage)
        {
            OnStageTransitioning?.Invoke(stage);
            if (initialTransition)
            {
                initialTransition = false;
            }

            await UnloadScenesAsync(stage);
            await LoadScenesAsync(stage);

            currentStage = stage;
            Debug(Operation.Replace, currentStage);
            OnStageTransitioned?.Invoke(currentStage);
        }

        /// <inheritdoc/>
        public async UniTask PushAsync(TStage stage)
        {
            OnStageTransitioning?.Invoke(stage);
            if (!initialTransition)
            {
                stageHistory.Push(currentStage);
            }
            else
            {
                initialTransition = false;
            }

            await UnloadScenesAsync(stage);
            await LoadScenesAsync(stage);

            currentStage = stage;
            Debug(Operation.Push, currentStage);
            OnStageTransitioned?.Invoke(currentStage);
        }

        /// <inheritdoc/>
        public async UniTask PopAsync()
        {
            if (stageHistory.Count == 0)
            {
                throw new InvalidOperationException("there is no stage transition history");
            }

            currentStage = stageHistory.Pop();
            OnStageTransitioning?.Invoke(currentStage);
            await UnloadScenesAsync(currentStage);
            await LoadScenesAsync(currentStage);

            Debug(Operation.Pop, currentStage);
            OnStageTransitioned?.Invoke(currentStage);
        }

        /// <inheritdoc/>
        public void Reset()
        {
            stageHistory.Clear();
            Debug(Operation.Reset);
        }

        private async UniTask UnloadScenesAsync(TStage stage)
        {
            var asyncOps = new List<UnityEngine.AsyncOperation>();
            for (var i = loadedScenes.Count - 1; i >= 0; i--)
            {
                var loadedScene = loadedScenes[i];
                if (!stageMap[stage].Contains(loadedScene))
                {
                    var asyncOp = SceneManager.UnloadSceneAsync(loadedScene.ToString());
                    asyncOps.Add(asyncOp);
                    loadedScenes.RemoveAt(i);
                }
            }

            await UniTask.WaitUntil(() => IsDoneAllOperations(asyncOps));
        }

        private async UniTask LoadScenesAsync(TStage stage)
        {
            var asyncOps = new List<UnityEngine.AsyncOperation>();
            foreach (var scene in stageMap[stage])
            {
                if (!loadedScenes.Contains(scene))
                {
                    var asyncOp = SceneManager.LoadSceneAsync(scene.ToString(), LoadSceneMode.Additive);
                    asyncOps.Add(asyncOp);
                    loadedScenes.Add(scene);
                }
            }

            await UniTask.WaitUntil(() => IsDoneAllOperations(asyncOps));
        }

        private static bool IsEmpty(List<Stage<TStage, TScene>> stages)
            => stages == null || stages.Count == 0;

        private static bool IsDoneAllOperations(List<UnityEngine.AsyncOperation> asyncOps)
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

        private static void Debug(Operation operation, TStage stage)
        {
            if (Logger.IsDebug())
            {
                Logger.LogDebug($"{operation}: {stage}");
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

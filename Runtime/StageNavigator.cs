using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Extreal.Core.Common.System;
using Extreal.Core.Logging;
using UniRx;
using UnityEngine.SceneManagement;

namespace Extreal.Core.StageNavigation
{
    /// <summary>
    /// Class used to transition stages.
    /// </summary>
    /// <typeparam name="TStage">Enum for stage names.</typeparam>
    /// <typeparam name="TScene">Enum for scene names.</typeparam>
    public class StageNavigator<TStage, TScene> : DisposableBase
        where TStage : struct
        where TScene : struct
    {
        private static readonly ELogger Logger
            = LoggingManager.GetLogger(nameof(StageNavigator<TStage, TScene>));

        /// <summary>
        /// <para>Invokes just before a stage transitioning.</para>
        /// Arg: Stage Name to transition to.
        /// </summary>
        public IObservable<TStage> OnStageTransitioning => onStageTransitioning;
        private readonly Subject<TStage> onStageTransitioning = new Subject<TStage>();

        /// <summary>
        /// <para>Invokes immediately after a stage transitioned.</para>
        /// Arg: Stage Name to transition to.
        /// </summary>
        public IObservable<TStage> OnStageTransitioned => onStageTransitioned;
        private readonly Subject<TStage> onStageTransitioned = new Subject<TStage>();

        private readonly Dictionary<TStage, TScene[]> stageMap = new Dictionary<TStage, TScene[]>();
        private readonly List<TScene> loadedScenes = new List<TScene>();

        /// <summary>
        /// Creates a new StageNavigator with given configuration.
        /// </summary>
        /// <exception cref="ArgumentNullException">If config is null.</exception>
        /// <exception cref="ArgumentException">If config contains no stages.</exception>
        /// <param name="config">Stage configuration.</param>
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
        protected override void ReleaseManagedResources()
        {
            onStageTransitioning.Dispose();
            onStageTransitioned.Dispose();
        }

        /// <summary>
        /// Transitions to the stage.
        /// </summary>
        /// <param name="stage">Stage Name to transition to.</param>
        /// <returns>UniTask of this method.</returns>
        public async UniTask ReplaceAsync(TStage stage)
        {
            Logger.LogDebug($"Transitions to '{stage}'");

            onStageTransitioning.OnNext(stage);

            await UnloadScenesAsync(stage);
            await LoadScenesAsync(stage);

            onStageTransitioned.OnNext(stage);
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
    }
}

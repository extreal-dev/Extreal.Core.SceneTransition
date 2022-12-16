using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using Extreal.Core.Logging;
using NUnit.Framework;
using UniRx;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Extreal.Core.StageNavigation.Test
{
    public class StageNavigatorTest
    {
        private StageNavigator<StageName, SceneName> stageNavigator;

        private StageName transitioningStageName;
        private StageName transitionedStageName;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeCracker", "CC0033")]
        private readonly CompositeDisposable disposables = new CompositeDisposable();

        [UnitySetUp]
        public IEnumerator InitializeAsync() => UniTask.ToCoroutine(async () =>
        {
            LoggingManager.Initialize(logLevel: LogLevel.Debug);

            await SceneManager.LoadSceneAsync("Main");

            var provider = Object.FindObjectOfType<StageConfigProvider>();
            stageNavigator = new StageNavigator<StageName, SceneName>(provider.StageConfig);

            _ = stageNavigator.OnStageTransitioning
                .Subscribe(stage => transitioningStageName = stage)
                .AddTo(disposables);

            _ = stageNavigator.OnStageTransitioned
                .Subscribe(stage => transitionedStageName = stage)
                .AddTo(disposables);

            transitioningStageName = default;
            transitionedStageName = default;
        });

        [UnityTearDown]
        public IEnumerator DisposeAsync() => UniTask.ToCoroutine(async () =>
        {
            disposables.Clear();
            stageNavigator.Dispose();
            await UniTask.Yield();
        });

        [OneTimeTearDown]
        public void OneTimeTearDown()
            => disposables.Dispose();

        [Test]
        public void InvalidConfig()
        {
            void TestConfigIsNull() => _ = new StageNavigator<StageName, SceneName>(null);
            Assert.That(TestConfigIsNull,
                Throws.TypeOf<ArgumentNullException>().With.Message.Contains("Parameter name: config"));

            void TestScenesIsNull()
            {
                var config = ScriptableObject.CreateInstance<StageConfig>();
                _ = new StageNavigator<StageName, SceneName>(config);
            }
            Assert.That(TestScenesIsNull,
                Throws.TypeOf<ArgumentException>()
                    .With.Message.EqualTo("stage config requires at least one stage"));

            void TestScenesIsEmpty()
            {
                var provider = Object.FindObjectOfType<StageConfigProvider>();
                _ = new StageNavigator<StageName, SceneName>(provider.EmptyStageConfig);
            }
            Assert.That(TestScenesIsEmpty,
                Throws.TypeOf<ArgumentException>()
                    .With.Message.EqualTo("stage config requires at least one stage"));
        }

        [UnityTest]
        public IEnumerator CommonScenes() => UniTask.ToCoroutine(async () =>
        {
            Assert.IsTrue(SceneManager.GetSceneByName(SceneName.CommonScene.ToString()).IsValid());

            // Transition to FirstStage without leaving history
            await stageNavigator.ReplaceAsync(StageName.FirstStage);
            Assert.IsTrue(SceneManager.GetSceneByName(SceneName.CommonScene.ToString()).IsValid());
        });

        [UnityTest]
        public IEnumerator Replace() => UniTask.ToCoroutine(async () =>
        {
            // Transition to FirstStage without leaving history
            await stageNavigator.ReplaceAsync(StageName.FirstStage);
            Assert.AreEqual(StageName.FirstStage, transitioningStageName);
            Assert.AreEqual(StageName.FirstStage, transitionedStageName);
            Assert.AreEqual(4, SceneManager.sceneCount);
            Assert.IsTrue(SceneManager.GetSceneByName(SceneName.FirstSpace.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(SceneName.FirstScreen.ToString()).IsValid());

            // Transition to SecondStage without leaving history
            await stageNavigator.ReplaceAsync(StageName.SecondStage);
            Assert.AreEqual(StageName.SecondStage, transitioningStageName);
            Assert.AreEqual(StageName.SecondStage, transitionedStageName);
            Assert.AreEqual(4, SceneManager.sceneCount);
            Assert.IsFalse(SceneManager.GetSceneByName(SceneName.FirstSpace.ToString()).IsValid());
            Assert.IsFalse(SceneManager.GetSceneByName(SceneName.FirstScreen.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(SceneName.SecondSpace.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(SceneName.SecondThirdScreen.ToString()).IsValid());

            // Transition to ThirdStage without leaving history
            await stageNavigator.ReplaceAsync(StageName.ThirdStage);
            Assert.AreEqual(StageName.ThirdStage, transitioningStageName);
            Assert.AreEqual(StageName.ThirdStage, transitionedStageName);
            Assert.AreEqual(5, SceneManager.sceneCount);
            Assert.IsFalse(SceneManager.GetSceneByName(SceneName.SecondSpace.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(SceneName.ThirdSpace.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(SceneName.SecondThirdScreen.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(SceneName.ThirdScreen.ToString()).IsValid());
        });
    }
}

using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using Extreal.Core.Logging;
using NUnit.Framework;
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

        private void OnStageTransitioning(StageName stage)
            => transitioningStageName = stage;

        private void OnStageTransitioned(StageName stage)
            => transitionedStageName = stage;

        [UnitySetUp]
        public IEnumerator InitializeAsync() => UniTask.ToCoroutine(async () =>
        {
            LoggingManager.Initialize(logLevel: LogLevel.Debug);

            await SceneManager.LoadSceneAsync("TestStageNavigatorScene");

            var provider = Object.FindObjectOfType<StageConfigProvider>();
            stageNavigator = new StageNavigator<StageName, SceneName>(provider.StageConfig);
            stageNavigator.OnStageTransitioning += OnStageTransitioning;
            stageNavigator.OnStageTransitioned += OnStageTransitioned;

            transitioningStageName = StageName.Unused;
            transitionedStageName = StageName.Unused;
        });

        [UnityTearDown]
        public IEnumerator DisposeAsync() => UniTask.ToCoroutine(async () =>
        {
            stageNavigator.OnStageTransitioning -= OnStageTransitioning;
            stageNavigator.OnStageTransitioned -= OnStageTransitioned;
            await UniTask.Yield();
        });

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
            Assert.IsTrue(SceneManager.GetSceneByName(SceneName.TestPermanent.ToString()).IsValid());

            // Transition to FirstStage without leaving history
            await stageNavigator.TransitionAsync(StageName.FirstStage);
            Assert.IsTrue(SceneManager.GetSceneByName(SceneName.TestPermanent.ToString()).IsValid());
        });

        [UnityTest]
        public IEnumerator Transition() => UniTask.ToCoroutine(async () =>
        {
            // Transition to FirstStage without leaving history
            await stageNavigator.TransitionAsync(StageName.FirstStage);
            Assert.AreEqual(StageName.FirstStage, transitioningStageName);
            Assert.AreEqual(StageName.FirstStage, transitionedStageName);
            Assert.AreEqual(4, SceneManager.sceneCount);
            Assert.IsTrue(SceneManager.GetSceneByName(SceneName.TestFirstStage.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(SceneName.TestFirstModal.ToString()).IsValid());

            // Transition to SecondStage without leaving history
            await stageNavigator.TransitionAsync(StageName.SecondStage);
            Assert.AreEqual(StageName.SecondStage, transitioningStageName);
            Assert.AreEqual(StageName.SecondStage, transitionedStageName);
            Assert.AreEqual(4, SceneManager.sceneCount);
            Assert.IsFalse(SceneManager.GetSceneByName(SceneName.TestFirstStage.ToString()).IsValid());
            Assert.IsFalse(SceneManager.GetSceneByName(SceneName.TestFirstModal.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(SceneName.TestSecondStage.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(SceneName.TestSecondThirdModal.ToString()).IsValid());

            // Transition to ThirdStage without leaving history
            await stageNavigator.TransitionAsync(StageName.ThirdStage);
            Assert.AreEqual(StageName.ThirdStage, transitioningStageName);
            Assert.AreEqual(StageName.ThirdStage, transitionedStageName);
            Assert.AreEqual(5, SceneManager.sceneCount);
            Assert.IsFalse(SceneManager.GetSceneByName(SceneName.TestSecondStage.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(SceneName.TestThirdStage.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(SceneName.TestSecondThirdModal.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(SceneName.TestThirdModal.ToString()).IsValid());
        });
    }
}

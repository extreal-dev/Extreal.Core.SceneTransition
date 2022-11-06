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
        private StageName currentStage;
        private IStageNavigator<StageName> stageNavigator;

        private StageName onStageTransitioning;
        private StageName onStageTransitioned;

        private void OnStageTransitioning(StageName stage)
            => onStageTransitioning = stage;

        private void OnStageTransitioned(StageName stage)
        {
            currentStage = stage;
            onStageTransitioned = stage;
        }

        [UnitySetUp]
        public IEnumerator InitializeAsync() => UniTask.ToCoroutine(async () =>
        {
            LoggingManager.Initialize(logLevel: LogLevel.Debug);

            await SceneManager.LoadSceneAsync("TestStageNavigatorScene");

            var provider = Object.FindObjectOfType<StageConfigProvider>();
            stageNavigator = new StageNavigator<StageName, SceneName>(provider.StageConfig);
            stageNavigator.OnStageTransitioning += OnStageTransitioning;
            stageNavigator.OnStageTransitioned += OnStageTransitioned;

            onStageTransitioning = StageName.Unused;
            onStageTransitioned = StageName.Unused;
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
        public IEnumerator Permanent() => UniTask.ToCoroutine(async () =>
        {
            Assert.IsTrue(SceneManager.GetSceneByName(SceneName.TestPermanent.ToString()).IsValid());

            // Transition to FirstScene without leaving history
            await stageNavigator.ReplaceAsync(StageName.FirstStage);
            Assert.IsTrue(SceneManager.GetSceneByName(SceneName.TestPermanent.ToString()).IsValid());

            // Transition to SecondScene with leaving history
            await stageNavigator.PushAsync(StageName.SecondStage);
            Assert.IsTrue(SceneManager.GetSceneByName(SceneName.TestPermanent.ToString()).IsValid());

            // Transition back to FirstScene according to history
            await stageNavigator.PopAsync();
            Assert.IsTrue(SceneManager.GetSceneByName(SceneName.TestPermanent.ToString()).IsValid());

            // Reset transition history
            stageNavigator.Reset();
            Assert.IsTrue(SceneManager.GetSceneByName(SceneName.TestPermanent.ToString()).IsValid());
        });

        [UnityTest]
        public IEnumerator Replace() => UniTask.ToCoroutine(async () =>
        {
            // Transition to FirstScene without leaving history
            await stageNavigator.ReplaceAsync(StageName.FirstStage);
            Assert.AreEqual(StageName.FirstStage, currentStage);
            Assert.AreEqual(4, SceneManager.sceneCount);
            Assert.IsTrue(SceneManager.GetSceneByName(SceneName.TestFirstStage.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(SceneName.TestFirstModal.ToString()).IsValid());

            // Transition to SecondScene without leaving history
            await stageNavigator.ReplaceAsync(StageName.SecondStage);
            Assert.AreEqual(StageName.SecondStage, currentStage);
            Assert.AreEqual(4, SceneManager.sceneCount);
            Assert.IsFalse(SceneManager.GetSceneByName(SceneName.TestFirstStage.ToString()).IsValid());
            Assert.IsFalse(SceneManager.GetSceneByName(SceneName.TestFirstModal.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(SceneName.TestSecondStage.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(SceneName.TestSecondThirdModal.ToString()).IsValid());

            // Transition to ThirdScene without leaving history
            await stageNavigator.ReplaceAsync(StageName.ThirdStage);
            Assert.AreEqual(StageName.ThirdStage, currentStage);
            Assert.AreEqual(5, SceneManager.sceneCount);
            Assert.IsFalse(SceneManager.GetSceneByName(SceneName.TestSecondStage.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(SceneName.TestThirdStage.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(SceneName.TestSecondThirdModal.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(SceneName.TestThirdModal.ToString()).IsValid());
        });

        [UnityTest]
        public IEnumerator Push() => UniTask.ToCoroutine(async () =>
        {
            // Transition to FirstScene with leaving history
            await stageNavigator.PushAsync(StageName.FirstStage);
            Assert.AreEqual(StageName.FirstStage, currentStage);
            Assert.AreEqual(4, SceneManager.sceneCount);
            Assert.IsTrue(SceneManager.GetSceneByName(SceneName.TestFirstStage.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(SceneName.TestFirstModal.ToString()).IsValid());

            // Transition to SecondScene with leaving history
            await stageNavigator.PushAsync(StageName.SecondStage);
            Assert.AreEqual(StageName.SecondStage, currentStage);
            Assert.AreEqual(4, SceneManager.sceneCount);
            Assert.IsFalse(SceneManager.GetSceneByName(SceneName.TestFirstStage.ToString()).IsValid());
            Assert.IsFalse(SceneManager.GetSceneByName(SceneName.TestFirstModal.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(SceneName.TestSecondStage.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(SceneName.TestSecondThirdModal.ToString()).IsValid());

            // Transition to ThirdScene with leaving history
            await stageNavigator.PushAsync(StageName.ThirdStage);
            Assert.AreEqual(StageName.ThirdStage, currentStage);
            Assert.AreEqual(5, SceneManager.sceneCount);
            Assert.IsFalse(SceneManager.GetSceneByName(SceneName.TestSecondStage.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(SceneName.TestThirdStage.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(SceneName.TestSecondThirdModal.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(SceneName.TestThirdModal.ToString()).IsValid());
        });

        [UnityTest]
        public IEnumerator PopIfNoHistory() => UniTask.ToCoroutine(async () =>
        {
            Exception exception = null;
            try
            {
                await stageNavigator.PopAsync();
            }
            catch (Exception e)
            {
                exception = e;
            }

            Assert.IsNotNull(exception);

            void Test() => throw exception;

            Assert.That(Test,
                Throws.TypeOf<InvalidOperationException>()
                    .With.Message.EqualTo("there is no scene transition history"));
        });

        [UnityTest]
        public IEnumerator Pop() => UniTask.ToCoroutine(async () =>
        {
            // Transition to FirstScene with leaving history
            await stageNavigator.PushAsync(StageName.FirstStage);

            // Transition to SecondScene with leaving history
            await stageNavigator.PushAsync(StageName.SecondStage);

            // Transition to ThirdScene with leaving history
            await stageNavigator.PushAsync(StageName.ThirdStage);

            // Transition to FirstScene with leaving history
            await stageNavigator.PushAsync(StageName.FirstStage);

            // Transition back to ThirdScene according to history
            await stageNavigator.PopAsync();
            Assert.AreEqual(StageName.ThirdStage, currentStage);
            Assert.AreEqual(5, SceneManager.sceneCount);
            Assert.IsTrue(SceneManager.GetSceneByName(SceneName.TestThirdStage.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(SceneName.TestSecondThirdModal.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(SceneName.TestThirdModal.ToString()).IsValid());

            // Transition back to SecondScene according to history
            await stageNavigator.PopAsync();
            Assert.AreEqual(StageName.SecondStage, currentStage);
            Assert.AreEqual(4, SceneManager.sceneCount);
            Assert.IsFalse(SceneManager.GetSceneByName(SceneName.TestThirdStage.ToString()).IsValid());
            Assert.IsFalse(SceneManager.GetSceneByName(SceneName.TestThirdModal.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(SceneName.TestSecondStage.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(SceneName.TestSecondThirdModal.ToString()).IsValid());

            // Transition back to FirstScene according to history
            await stageNavigator.PopAsync();
            Assert.AreEqual(StageName.FirstStage, currentStage);
            Assert.AreEqual(4, SceneManager.sceneCount);
            Assert.IsFalse(SceneManager.GetSceneByName(SceneName.TestSecondStage.ToString()).IsValid());
            Assert.IsFalse(SceneManager.GetSceneByName(SceneName.TestSecondThirdModal.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(SceneName.TestFirstStage.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(SceneName.TestFirstModal.ToString()).IsValid());
        });

        [UnityTest]
        public IEnumerator Reset() => UniTask.ToCoroutine(async () =>
        {
            // Transition to FirstScene with leaving history
            await stageNavigator.PushAsync(StageName.FirstStage);

            // Transition to SecondScene with leaving history
            await stageNavigator.PushAsync(StageName.SecondStage);
            Assert.AreEqual(StageName.SecondStage, currentStage);
            Assert.AreEqual(4, SceneManager.sceneCount);
            Assert.IsTrue(SceneManager.GetSceneByName(SceneName.TestSecondStage.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(SceneName.TestSecondThirdModal.ToString()).IsValid());

            // Reset transition history
            stageNavigator.Reset();
        });

        [UnityTest]
        public IEnumerator PushReplacePushPop() => UniTask.ToCoroutine(async () =>
        {
            Assert.That(onStageTransitioning, Is.EqualTo(StageName.Unused));
            Assert.That(onStageTransitioned, Is.EqualTo(StageName.Unused));

            // Initial Transition
            await stageNavigator.ReplaceAsync(StageName.FirstStage);
            LogAssert.Expect(LogType.Log, "[Debug:StageNavigator] Replace: FirstStage");
            Assert.That(onStageTransitioning, Is.EqualTo(StageName.FirstStage));
            Assert.That(onStageTransitioned, Is.EqualTo(StageName.FirstStage));

            // Transition to SecondScene with leaving history
            await stageNavigator.PushAsync(StageName.SecondStage);
            Assert.AreEqual(StageName.SecondStage, currentStage);
            LogAssert.Expect(LogType.Log, "[Debug:StageNavigator] Push: SecondStage");
            Assert.That(onStageTransitioning, Is.EqualTo(StageName.SecondStage));
            Assert.That(onStageTransitioned, Is.EqualTo(StageName.SecondStage));

            // Transition to ThirdScene without leaving history
            await stageNavigator.PushAsync(StageName.ThirdStage);
            Assert.AreEqual(StageName.ThirdStage, currentStage);
            LogAssert.Expect(LogType.Log, "[Debug:StageNavigator] Push: ThirdStage");
            Assert.That(onStageTransitioning, Is.EqualTo(StageName.ThirdStage));
            Assert.That(onStageTransitioned, Is.EqualTo(StageName.ThirdStage));

            // Transition to FirstScene with leaving history
            await stageNavigator.PushAsync(StageName.FirstStage);
            Assert.AreEqual(StageName.FirstStage, currentStage);
            LogAssert.Expect(LogType.Log, "[Debug:StageNavigator] Push: FirstStage");
            Assert.That(onStageTransitioning, Is.EqualTo(StageName.FirstStage));
            Assert.That(onStageTransitioned, Is.EqualTo(StageName.FirstStage));

            // Transition back to Third according to history
            await stageNavigator.PopAsync();
            Assert.AreEqual(StageName.ThirdStage, currentStage);
            LogAssert.Expect(LogType.Log, "[Debug:StageNavigator] Pop: ThirdStage");
            Assert.That(onStageTransitioning, Is.EqualTo(StageName.ThirdStage));
            Assert.That(onStageTransitioned, Is.EqualTo(StageName.ThirdStage));

            // Reset
            onStageTransitioning = StageName.Unused;
            onStageTransitioned = StageName.Unused;
            stageNavigator.Reset();
            LogAssert.Expect(LogType.Log, "[Debug:StageNavigator] Reset");
            Assert.That(onStageTransitioning, Is.EqualTo(StageName.Unused));
            Assert.That(onStageTransitioned, Is.EqualTo(StageName.Unused));
        });
    }
}

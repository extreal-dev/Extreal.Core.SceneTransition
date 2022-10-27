using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using Extreal.Core.Logging;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Extreal.Core.SceneTransition.Test
{
    public class SceneTransitionTest
    {
        private SceneName currentScene;
        private ISceneTransitioner<SceneName> sceneTransitioner;

        [UnitySetUp]
        public IEnumerator InitializeAsync() => UniTask.ToCoroutine(async () =>
        {
            LoggingManager.Initialize(logLevel: LogLevel.Debug);

            await SceneManager.LoadSceneAsync("TestSceneTransitionScene");

            var provider = Object.FindObjectOfType<SceneConfigProvider>();
            sceneTransitioner = new SceneTransitioner<SceneName, UnitySceneName>(provider.SceneConfig);
            sceneTransitioner.OnSceneTransitioned += OnSceneTransitioned;
        });

        [UnityTearDown]
        public IEnumerator DisposeAsync() => UniTask.ToCoroutine(async () =>
        {
            sceneTransitioner.OnSceneTransitioned -= OnSceneTransitioned;
            await UniTask.Yield();
        });

        [Test]
        public void InvalidConfig()
        {
            void TestConfigIsNull() => _ = new SceneTransitioner<SceneName, UnitySceneName>(null);
            Assert.That(TestConfigIsNull,
                Throws.TypeOf<ArgumentNullException>().With.Message.Contains("Parameter name: config"));

            void TestScenesIsNull()
            {
                var config = ScriptableObject.CreateInstance<SceneConfig>();
                _ = new SceneTransitioner<SceneName, UnitySceneName>(config);
            }
            Assert.That(TestScenesIsNull,
                Throws.TypeOf<ArgumentException>()
                    .With.Message.EqualTo("scene config requires at least one scene"));

            void TestScenesIsEmpty()
            {
                var provider = Object.FindObjectOfType<SceneConfigProvider>();
                _ = new SceneTransitioner<SceneName, UnitySceneName>(provider.EmptySceneConfig);
            }
            Assert.That(TestScenesIsEmpty,
                Throws.TypeOf<ArgumentException>()
                    .With.Message.EqualTo("scene config requires at least one scene"));
        }

        [UnityTest]
        public IEnumerator Permanent() => UniTask.ToCoroutine(async () =>
        {
            Assert.IsTrue(SceneManager.GetSceneByName(UnitySceneName.TestPermanent.ToString()).IsValid());

            // Transition to FirstScene without leaving history
            await sceneTransitioner.ReplaceAsync(SceneName.FirstScene);
            Assert.IsTrue(SceneManager.GetSceneByName(UnitySceneName.TestPermanent.ToString()).IsValid());

            // Transition to SecondScene with leaving history
            await sceneTransitioner.PushAsync(SceneName.SecondScene);
            Assert.IsTrue(SceneManager.GetSceneByName(UnitySceneName.TestPermanent.ToString()).IsValid());

            // Transition back to FirstScene according to history
            await sceneTransitioner.PopAsync();
            Assert.IsTrue(SceneManager.GetSceneByName(UnitySceneName.TestPermanent.ToString()).IsValid());

            // Reset transition history
            sceneTransitioner.Reset();
            Assert.IsTrue(SceneManager.GetSceneByName(UnitySceneName.TestPermanent.ToString()).IsValid());
        });

        [UnityTest]
        public IEnumerator Replace() => UniTask.ToCoroutine(async () =>
        {
            // Transition to FirstScene without leaving history
            await sceneTransitioner.ReplaceAsync(SceneName.FirstScene);
            Assert.AreEqual(SceneName.FirstScene, currentScene);
            Assert.AreEqual(4, SceneManager.sceneCount);
            Assert.IsTrue(SceneManager.GetSceneByName(UnitySceneName.TestFirstStage.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(UnitySceneName.TestFirstModal.ToString()).IsValid());

            // Transition to SecondScene without leaving history
            await sceneTransitioner.ReplaceAsync(SceneName.SecondScene);
            Assert.AreEqual(SceneName.SecondScene, currentScene);
            Assert.AreEqual(4, SceneManager.sceneCount);
            Assert.IsFalse(SceneManager.GetSceneByName(UnitySceneName.TestFirstStage.ToString()).IsValid());
            Assert.IsFalse(SceneManager.GetSceneByName(UnitySceneName.TestFirstModal.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(UnitySceneName.TestSecondStage.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(UnitySceneName.TestSecondThirdModal.ToString()).IsValid());

            // Transition to ThirdScene without leaving history
            await sceneTransitioner.ReplaceAsync(SceneName.ThirdScene);
            Assert.AreEqual(SceneName.ThirdScene, currentScene);
            Assert.AreEqual(5, SceneManager.sceneCount);
            Assert.IsFalse(SceneManager.GetSceneByName(UnitySceneName.TestSecondStage.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(UnitySceneName.TestThirdStage.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(UnitySceneName.TestSecondThirdModal.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(UnitySceneName.TestThirdModal.ToString()).IsValid());
        });

        [UnityTest]
        public IEnumerator Push() => UniTask.ToCoroutine(async () =>
        {
            // Transition to FirstScene with leaving history
            await sceneTransitioner.PushAsync(SceneName.FirstScene);
            Assert.AreEqual(SceneName.FirstScene, currentScene);
            Assert.AreEqual(4, SceneManager.sceneCount);
            Assert.IsTrue(SceneManager.GetSceneByName(UnitySceneName.TestFirstStage.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(UnitySceneName.TestFirstModal.ToString()).IsValid());

            // Transition to SecondScene with leaving history
            await sceneTransitioner.PushAsync(SceneName.SecondScene);
            Assert.AreEqual(SceneName.SecondScene, currentScene);
            Assert.AreEqual(4, SceneManager.sceneCount);
            Assert.IsFalse(SceneManager.GetSceneByName(UnitySceneName.TestFirstStage.ToString()).IsValid());
            Assert.IsFalse(SceneManager.GetSceneByName(UnitySceneName.TestFirstModal.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(UnitySceneName.TestSecondStage.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(UnitySceneName.TestSecondThirdModal.ToString()).IsValid());

            // Transition to ThirdScene with leaving history
            await sceneTransitioner.PushAsync(SceneName.ThirdScene);
            Assert.AreEqual(SceneName.ThirdScene, currentScene);
            Assert.AreEqual(5, SceneManager.sceneCount);
            Assert.IsFalse(SceneManager.GetSceneByName(UnitySceneName.TestSecondStage.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(UnitySceneName.TestThirdStage.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(UnitySceneName.TestSecondThirdModal.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(UnitySceneName.TestThirdModal.ToString()).IsValid());
        });

        [UnityTest]
        public IEnumerator PopIfNoHistory() => UniTask.ToCoroutine(async () =>
        {
            Exception exception = null;
            try
            {
                await sceneTransitioner.PopAsync();
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
            await sceneTransitioner.PushAsync(SceneName.FirstScene);

            // Transition to SecondScene with leaving history
            await sceneTransitioner.PushAsync(SceneName.SecondScene);

            // Transition to ThirdScene with leaving history
            await sceneTransitioner.PushAsync(SceneName.ThirdScene);

            // Transition to FirstScene with leaving history
            await sceneTransitioner.PushAsync(SceneName.FirstScene);

            // Transition back to ThirdScene according to history
            await sceneTransitioner.PopAsync();
            Assert.AreEqual(SceneName.ThirdScene, currentScene);
            Assert.AreEqual(5, SceneManager.sceneCount);
            Assert.IsTrue(SceneManager.GetSceneByName(UnitySceneName.TestThirdStage.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(UnitySceneName.TestSecondThirdModal.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(UnitySceneName.TestThirdModal.ToString()).IsValid());

            // Transition back to SecondScene according to history
            await sceneTransitioner.PopAsync();
            Assert.AreEqual(SceneName.SecondScene, currentScene);
            Assert.AreEqual(4, SceneManager.sceneCount);
            Assert.IsFalse(SceneManager.GetSceneByName(UnitySceneName.TestThirdStage.ToString()).IsValid());
            Assert.IsFalse(SceneManager.GetSceneByName(UnitySceneName.TestThirdModal.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(UnitySceneName.TestSecondStage.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(UnitySceneName.TestSecondThirdModal.ToString()).IsValid());

            // Transition back to FirstScene according to history
            await sceneTransitioner.PopAsync();
            Assert.AreEqual(SceneName.FirstScene, currentScene);
            Assert.AreEqual(4, SceneManager.sceneCount);
            Assert.IsFalse(SceneManager.GetSceneByName(UnitySceneName.TestSecondStage.ToString()).IsValid());
            Assert.IsFalse(SceneManager.GetSceneByName(UnitySceneName.TestSecondThirdModal.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(UnitySceneName.TestFirstStage.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(UnitySceneName.TestFirstModal.ToString()).IsValid());
        });

        [UnityTest]
        public IEnumerator Reset() => UniTask.ToCoroutine(async () =>
        {
            // Transition to FirstScene with leaving history
            await sceneTransitioner.PushAsync(SceneName.FirstScene);

            // Transition to SecondScene with leaving history
            await sceneTransitioner.PushAsync(SceneName.SecondScene);
            Assert.AreEqual(SceneName.SecondScene, currentScene);
            Assert.AreEqual(4, SceneManager.sceneCount);
            Assert.IsTrue(SceneManager.GetSceneByName(UnitySceneName.TestSecondStage.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(UnitySceneName.TestSecondThirdModal.ToString()).IsValid());

            // Reset transition history
            sceneTransitioner.Reset();
        });

        [UnityTest]
        public IEnumerator PushReplacePushPop() => UniTask.ToCoroutine(async () =>
        {
            // Initial Transition
            await sceneTransitioner.ReplaceAsync(SceneName.FirstScene);
            LogAssert.Expect(LogType.Log, "[Debug:SceneTransitioner] Replace: FirstScene");

            // Transition to SecondScene with leaving history
            await sceneTransitioner.PushAsync(SceneName.SecondScene);
            Assert.AreEqual(SceneName.SecondScene, currentScene);
            LogAssert.Expect(LogType.Log, "[Debug:SceneTransitioner] Push: SecondScene");

            // Transition to ThirdScene without leaving history
            await sceneTransitioner.PushAsync(SceneName.ThirdScene);
            Assert.AreEqual(SceneName.ThirdScene, currentScene);
            LogAssert.Expect(LogType.Log, "[Debug:SceneTransitioner] Push: ThirdScene");

            // Transition to FirstScene with leaving history
            await sceneTransitioner.PushAsync(SceneName.FirstScene);
            Assert.AreEqual(SceneName.FirstScene, currentScene);
            LogAssert.Expect(LogType.Log, "[Debug:SceneTransitioner] Push: FirstScene");

            // Transition back to Third according to history
            await sceneTransitioner.PopAsync();
            Assert.AreEqual(SceneName.ThirdScene, currentScene);
            LogAssert.Expect(LogType.Log, "[Debug:SceneTransitioner] Pop: ThirdScene");

            // Reset
            sceneTransitioner.Reset();
            LogAssert.Expect(LogType.Log, "[Debug:SceneTransitioner] Reset");
        });

        private void OnSceneTransitioned(SceneName scene)
            => currentScene = scene;
    }
}

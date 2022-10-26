using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
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
        private bool sceneTransitioned;
        private SceneName currentScene;
        private ISceneTransitioner<SceneName> sceneTransitioner;

        [UnitySetUp]
        public IEnumerator Initialize()
        {
            LoggingManager.Initialize(logLevel: LogLevel.Debug);

            var asyncOp = SceneManager.LoadSceneAsync("TestSceneTransitionScene");
            yield return new WaitUntil(() => asyncOp.isDone);

            var provider = Object.FindObjectOfType<SceneConfigProvider>();
            sceneTransitioner = new SceneTransitioner<SceneName, UnitySceneName>(provider.SceneConfig);
            sceneTransitioner.OnSceneTransitioned += OnSceneTransitioned;
            sceneTransitioned = false;
        }

        [TearDown]
        public void Dispose() => sceneTransitioner.OnSceneTransitioned -= OnSceneTransitioned;

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
        public IEnumerator Permanent()
        {
            Assert.IsTrue(SceneManager.GetSceneByName(UnitySceneName.TestPermanent.ToString()).IsValid());

            // Transition to FirstScene without leaving history
            _ = sceneTransitioner.ReplaceAsync(SceneName.FirstScene);
            yield return WaitUntilSceneChanged();

            Assert.IsTrue(SceneManager.GetSceneByName(UnitySceneName.TestPermanent.ToString()).IsValid());

            // Transition to SecondScene with leaving history
            _ = sceneTransitioner.PushAsync(SceneName.SecondScene);
            yield return WaitUntilSceneChanged();

            Assert.IsTrue(SceneManager.GetSceneByName(UnitySceneName.TestPermanent.ToString()).IsValid());

            // Transition back to FirstScene according to history
            _ = sceneTransitioner.PopAsync();
            yield return WaitUntilSceneChanged();

            Assert.IsTrue(SceneManager.GetSceneByName(UnitySceneName.TestPermanent.ToString()).IsValid());

            // Reset transition history
            sceneTransitioner.Reset();

            Assert.IsTrue(SceneManager.GetSceneByName(UnitySceneName.TestPermanent.ToString()).IsValid());
        }

        [UnityTest]
        public IEnumerator Replace()
        {
            // Transition to FirstScene without leaving history
            _ = sceneTransitioner.ReplaceAsync(SceneName.FirstScene);
            yield return WaitUntilSceneChanged();

            Assert.AreEqual(SceneName.FirstScene, currentScene);
            Assert.AreEqual(4, SceneManager.sceneCount);
            Assert.IsTrue(SceneManager.GetSceneByName(UnitySceneName.TestFirstStage.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(UnitySceneName.TestFirstModal.ToString()).IsValid());

            // Transition to SecondScene without leaving history
            _ = sceneTransitioner.ReplaceAsync(SceneName.SecondScene);
            yield return WaitUntilSceneChanged();

            Assert.AreEqual(SceneName.SecondScene, currentScene);
            Assert.AreEqual(4, SceneManager.sceneCount);
            Assert.IsFalse(SceneManager.GetSceneByName(UnitySceneName.TestFirstStage.ToString()).IsValid());
            Assert.IsFalse(SceneManager.GetSceneByName(UnitySceneName.TestFirstModal.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(UnitySceneName.TestSecondStage.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(UnitySceneName.TestSecondThirdModal.ToString()).IsValid());

            // Transition to ThirdScene without leaving history
            _ = sceneTransitioner.ReplaceAsync(SceneName.ThirdScene);
            yield return WaitUntilSceneChanged();

            Assert.AreEqual(SceneName.ThirdScene, currentScene);
            Assert.AreEqual(5, SceneManager.sceneCount);
            Assert.IsFalse(SceneManager.GetSceneByName(UnitySceneName.TestSecondStage.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(UnitySceneName.TestThirdStage.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(UnitySceneName.TestSecondThirdModal.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(UnitySceneName.TestThirdModal.ToString()).IsValid());
        }

        [UnityTest]
        public IEnumerator Push()
        {
            // Transition to FirstScene with leaving history
            _ = sceneTransitioner.PushAsync(SceneName.FirstScene);
            yield return WaitUntilSceneChanged();

            Assert.AreEqual(SceneName.FirstScene, currentScene);
            Assert.AreEqual(4, SceneManager.sceneCount);
            Assert.IsTrue(SceneManager.GetSceneByName(UnitySceneName.TestFirstStage.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(UnitySceneName.TestFirstModal.ToString()).IsValid());

            // Transition to SecondScene with leaving history
            _ = sceneTransitioner.PushAsync(SceneName.SecondScene);
            yield return WaitUntilSceneChanged();

            Assert.AreEqual(SceneName.SecondScene, currentScene);
            Assert.AreEqual(4, SceneManager.sceneCount);
            Assert.IsFalse(SceneManager.GetSceneByName(UnitySceneName.TestFirstStage.ToString()).IsValid());
            Assert.IsFalse(SceneManager.GetSceneByName(UnitySceneName.TestFirstModal.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(UnitySceneName.TestSecondStage.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(UnitySceneName.TestSecondThirdModal.ToString()).IsValid());

            // Transition to ThirdScene with leaving history
            _ = sceneTransitioner.PushAsync(SceneName.ThirdScene);
            yield return WaitUntilSceneChanged();

            Assert.AreEqual(SceneName.ThirdScene, currentScene);
            Assert.AreEqual(5, SceneManager.sceneCount);
            Assert.IsFalse(SceneManager.GetSceneByName(UnitySceneName.TestSecondStage.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(UnitySceneName.TestThirdStage.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(UnitySceneName.TestSecondThirdModal.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(UnitySceneName.TestThirdModal.ToString()).IsValid());
        }

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
        public IEnumerator Pop()
        {
            // Transition to FirstScene with leaving history
            _ = sceneTransitioner.PushAsync(SceneName.FirstScene);
            yield return WaitUntilSceneChanged();

            // Transition to SecondScene with leaving history
            _ = sceneTransitioner.PushAsync(SceneName.SecondScene);
            yield return WaitUntilSceneChanged();

            // Transition to ThirdScene with leaving history
            _ = sceneTransitioner.PushAsync(SceneName.ThirdScene);
            yield return WaitUntilSceneChanged();

            // Transition to FirstScene with leaving history
            _ = sceneTransitioner.PushAsync(SceneName.FirstScene);
            yield return WaitUntilSceneChanged();

            // Transition back to ThirdScene according to history
            _ = sceneTransitioner.PopAsync();
            yield return WaitUntilSceneChanged();

            Assert.AreEqual(SceneName.ThirdScene, currentScene);
            Assert.AreEqual(5, SceneManager.sceneCount);
            Assert.IsTrue(SceneManager.GetSceneByName(UnitySceneName.TestThirdStage.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(UnitySceneName.TestSecondThirdModal.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(UnitySceneName.TestThirdModal.ToString()).IsValid());

            // Transition back to SecondScene according to history
            _ = sceneTransitioner.PopAsync();
            yield return WaitUntilSceneChanged();

            Assert.AreEqual(SceneName.SecondScene, currentScene);
            Assert.AreEqual(4, SceneManager.sceneCount);
            Assert.IsFalse(SceneManager.GetSceneByName(UnitySceneName.TestThirdStage.ToString()).IsValid());
            Assert.IsFalse(SceneManager.GetSceneByName(UnitySceneName.TestThirdModal.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(UnitySceneName.TestSecondStage.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(UnitySceneName.TestSecondThirdModal.ToString()).IsValid());

            // Transition back to FirstScene according to history
            _ = sceneTransitioner.PopAsync();
            yield return WaitUntilSceneChanged();

            Assert.AreEqual(SceneName.FirstScene, currentScene);
            Assert.AreEqual(4, SceneManager.sceneCount);
            Assert.IsFalse(SceneManager.GetSceneByName(UnitySceneName.TestSecondStage.ToString()).IsValid());
            Assert.IsFalse(SceneManager.GetSceneByName(UnitySceneName.TestSecondThirdModal.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(UnitySceneName.TestFirstStage.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(UnitySceneName.TestFirstModal.ToString()).IsValid());
        }

        [UnityTest]
        public IEnumerator Reset()
        {
            // Transition to FirstScene with leaving history
            _ = sceneTransitioner.PushAsync(SceneName.FirstScene);
            yield return WaitUntilSceneChanged();

            // Transition to SecondScene with leaving history
            _ = sceneTransitioner.PushAsync(SceneName.SecondScene);
            yield return WaitUntilSceneChanged();

            Assert.AreEqual(SceneName.SecondScene, currentScene);
            Assert.AreEqual(4, SceneManager.sceneCount);
            Assert.IsTrue(SceneManager.GetSceneByName(UnitySceneName.TestSecondStage.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(UnitySceneName.TestSecondThirdModal.ToString()).IsValid());

            // Reset transition history
            sceneTransitioner.Reset();
        }

        [UnityTest]
        [SuppressMessage("Design", "CC0021:Use nameof")]
        public IEnumerator PushReplacePushPop()
        {
            // Initial Transition
            _ = sceneTransitioner.ReplaceAsync(SceneName.FirstScene);
            yield return WaitUntilSceneChanged();

            LogAssert.Expect(LogType.Log, "[Debug:SceneTransitioner] Replace: FirstScene");

            // Transition to SecondScene with leaving history
            _ = sceneTransitioner.PushAsync(SceneName.SecondScene);
            yield return WaitUntilSceneChanged();

            Assert.AreEqual(SceneName.SecondScene, currentScene);
            LogAssert.Expect(LogType.Log, "[Debug:SceneTransitioner] Push: SecondScene");

            // Transition to ThirdScene without leaving history
            _ = sceneTransitioner.PushAsync(SceneName.ThirdScene);
            yield return WaitUntilSceneChanged();

            Assert.AreEqual(SceneName.ThirdScene, currentScene);
            LogAssert.Expect(LogType.Log, "[Debug:SceneTransitioner] Push: ThirdScene");

            // Transition to FirstScene with leaving history
            _ = sceneTransitioner.PushAsync(SceneName.FirstScene);
            yield return WaitUntilSceneChanged();

            Assert.AreEqual(SceneName.FirstScene, currentScene);
            LogAssert.Expect(LogType.Log, "[Debug:SceneTransitioner] Push: FirstScene");

            // Transition back to Third according to history
            _ = sceneTransitioner.PopAsync();
            yield return WaitUntilSceneChanged();

            Assert.AreEqual(SceneName.ThirdScene, currentScene);
            LogAssert.Expect(LogType.Log, "[Debug:SceneTransitioner] Pop: ThirdScene");

            // Reset
            sceneTransitioner.Reset();

            LogAssert.Expect(LogType.Log, "[Debug:SceneTransitioner] Reset");
        }

        private IEnumerator WaitUntilSceneChanged()
        {
            yield return new WaitUntil(() => sceneTransitioned);
            sceneTransitioned = false;
        }

        private void OnSceneTransitioned(SceneName scene)
        {
            currentScene = scene;
            sceneTransitioned = true;
        }
    }
}

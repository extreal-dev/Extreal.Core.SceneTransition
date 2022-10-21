using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Extreal.Core.SceneTransition.Test
{
    public class SceneTransitionTest
    {
        private bool _sceneChanged;
        private SceneName _currentScene;
        private ISceneTransitioner<SceneName> _sceneTransitioner;

        [UnitySetUp]
        public IEnumerator Initialize()
        {
            var asyncOp = SceneManager.LoadSceneAsync("TestMainScene");
            yield return new WaitUntil(() => asyncOp.isDone);

            var config = Object.FindObjectOfType<Configuration>();
            _sceneTransitioner = new SceneTransitioner<PageName, SceneName>(config._config);
            _sceneTransitioner.OnSceneChanged += OnSceneChanged;
            _sceneChanged = false;
        }

        [TearDown]
        public void Dispose()
        {
            _sceneTransitioner.OnSceneChanged -= OnSceneChanged;
        }

        [UnityTest]
        public IEnumerator Permanent()
        {
            Assert.IsTrue(SceneManager.GetSceneByName(PageName.TestPermanent.ToString()).IsValid());

            // Transition to FirstScene without leaving history
            _ = _sceneTransitioner.ReplaceAsync(SceneName.FirstScene);
            yield return WaitUntilSceneChanged();

            Assert.IsTrue(SceneManager.GetSceneByName(PageName.TestPermanent.ToString()).IsValid());

            // Transition to SecondScene with leaving history
            _ = _sceneTransitioner.PushAsync(SceneName.SecondScene);
            yield return WaitUntilSceneChanged();

            Assert.IsTrue(SceneManager.GetSceneByName(PageName.TestPermanent.ToString()).IsValid());

            // Transition back to FirstScene according to history
            _ = _sceneTransitioner.PopAsync();
            yield return WaitUntilSceneChanged();

            Assert.IsTrue(SceneManager.GetSceneByName(PageName.TestPermanent.ToString()).IsValid());

            // Reset transition history
            _sceneTransitioner.Reset();

            Assert.IsTrue(SceneManager.GetSceneByName(PageName.TestPermanent.ToString()).IsValid());
        }

        [UnityTest]
        public IEnumerator Replace()
        {
            // Transition to FirstScene without leaving history
            _ = _sceneTransitioner.ReplaceAsync(SceneName.FirstScene);
            yield return WaitUntilSceneChanged();

            Assert.AreEqual(SceneName.FirstScene, _currentScene);
            Assert.AreEqual(4, SceneManager.sceneCount);
            Assert.IsTrue(SceneManager.GetSceneByName(PageName.TestFirstStage.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(PageName.TestFirstModal.ToString()).IsValid());

            // Transition to SecondScene without leaving history
            _ = _sceneTransitioner.ReplaceAsync(SceneName.SecondScene);
            yield return WaitUntilSceneChanged();

            Assert.AreEqual(SceneName.SecondScene, _currentScene);
            Assert.AreEqual(4, SceneManager.sceneCount);
            Assert.IsFalse(SceneManager.GetSceneByName(PageName.TestFirstStage.ToString()).IsValid());
            Assert.IsFalse(SceneManager.GetSceneByName(PageName.TestFirstModal.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(PageName.TestSecondStage.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(PageName.TestSecondThirdModal.ToString()).IsValid());

            // Transition to ThirdScene without leaving history
            _ = _sceneTransitioner.ReplaceAsync(SceneName.ThirdScene);
            yield return WaitUntilSceneChanged();

            Assert.AreEqual(SceneName.ThirdScene, _currentScene);
            Assert.AreEqual(5, SceneManager.sceneCount);
            Assert.IsFalse(SceneManager.GetSceneByName(PageName.TestSecondStage.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(PageName.TestThirdStage.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(PageName.TestSecondThirdModal.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(PageName.TestThirdModal.ToString()).IsValid());
        }

        [UnityTest]
        public IEnumerator Push()
        {
            // Transition to FirstScene with leaving history
            _ = _sceneTransitioner.PushAsync(SceneName.FirstScene);
            yield return WaitUntilSceneChanged();

            Assert.AreEqual(SceneName.FirstScene, _currentScene);
            Assert.AreEqual(4, SceneManager.sceneCount);
            Assert.IsTrue(SceneManager.GetSceneByName(PageName.TestFirstStage.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(PageName.TestFirstModal.ToString()).IsValid());

            // Transition to SecondScene with leaving history
            _ = _sceneTransitioner.PushAsync(SceneName.SecondScene);
            yield return WaitUntilSceneChanged();

            Assert.AreEqual(SceneName.SecondScene, _currentScene);
            Assert.AreEqual(4, SceneManager.sceneCount);
            Assert.IsFalse(SceneManager.GetSceneByName(PageName.TestFirstStage.ToString()).IsValid());
            Assert.IsFalse(SceneManager.GetSceneByName(PageName.TestFirstModal.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(PageName.TestSecondStage.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(PageName.TestSecondThirdModal.ToString()).IsValid());

            // Transition to ThirdScene with leaving history
            _ = _sceneTransitioner.PushAsync(SceneName.ThirdScene);
            yield return WaitUntilSceneChanged();

            Assert.AreEqual(SceneName.ThirdScene, _currentScene);
            Assert.AreEqual(5, SceneManager.sceneCount);
            Assert.IsFalse(SceneManager.GetSceneByName(PageName.TestSecondStage.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(PageName.TestThirdStage.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(PageName.TestSecondThirdModal.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(PageName.TestThirdModal.ToString()).IsValid());
        }

        [UnityTest]
        public IEnumerator Pop()
        {
            // Transition to FirstScene with leaving history
            _ = _sceneTransitioner.PushAsync(SceneName.FirstScene);
            yield return WaitUntilSceneChanged();

            // Transition to SecondScene with leaving history
            _ = _sceneTransitioner.PushAsync(SceneName.SecondScene);
            yield return WaitUntilSceneChanged();

            // Transition to ThirdScene with leaving history
            _ = _sceneTransitioner.PushAsync(SceneName.ThirdScene);
            yield return WaitUntilSceneChanged();

            // Transition to FirstScene with leaving history
            _ = _sceneTransitioner.PushAsync(SceneName.FirstScene);
            yield return WaitUntilSceneChanged();

            // Transition back to ThirdScene according to history
            _ = _sceneTransitioner.PopAsync();
            yield return WaitUntilSceneChanged();

            Assert.AreEqual(SceneName.ThirdScene, _currentScene);
            Assert.AreEqual(5, SceneManager.sceneCount);
            Assert.IsTrue(SceneManager.GetSceneByName(PageName.TestThirdStage.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(PageName.TestSecondThirdModal.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(PageName.TestThirdModal.ToString()).IsValid());

            // Transition back to SecondScene according to history
            _ = _sceneTransitioner.PopAsync();
            yield return WaitUntilSceneChanged();

            Assert.AreEqual(SceneName.SecondScene, _currentScene);
            Assert.AreEqual(4, SceneManager.sceneCount);
            Assert.IsFalse(SceneManager.GetSceneByName(PageName.TestThirdStage.ToString()).IsValid());
            Assert.IsFalse(SceneManager.GetSceneByName(PageName.TestThirdModal.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(PageName.TestSecondStage.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(PageName.TestSecondThirdModal.ToString()).IsValid());

            // Transition back to FirstScene according to history
            _ = _sceneTransitioner.PopAsync();
            yield return WaitUntilSceneChanged();

            Assert.AreEqual(SceneName.FirstScene, _currentScene);
            Assert.AreEqual(4, SceneManager.sceneCount);
            Assert.IsFalse(SceneManager.GetSceneByName(PageName.TestSecondStage.ToString()).IsValid());
            Assert.IsFalse(SceneManager.GetSceneByName(PageName.TestSecondThirdModal.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(PageName.TestFirstStage.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(PageName.TestFirstModal.ToString()).IsValid());
        }

        [UnityTest]
        public IEnumerator Reset()
        {
            // Transition to FirstScene with leaving history
            _ = _sceneTransitioner.PushAsync(SceneName.FirstScene);
            yield return WaitUntilSceneChanged();

            // Transition to SecondScene with leaving history
            _ = _sceneTransitioner.PushAsync(SceneName.SecondScene);
            yield return WaitUntilSceneChanged();

            Assert.AreEqual(SceneName.SecondScene, _currentScene);
            Assert.AreEqual(4, SceneManager.sceneCount);
            Assert.IsTrue(SceneManager.GetSceneByName(PageName.TestSecondStage.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(PageName.TestSecondThirdModal.ToString()).IsValid());

            // Reset transition history
            _sceneTransitioner.Reset();

            // Transition back according to history
            // Nothing is executed
            _ = _sceneTransitioner.PopAsync();
            yield return new WaitForSeconds(1);

            Assert.IsFalse(_sceneChanged);
        }

        [UnityTest]
        public IEnumerator PopWithoutPush()
        {
            // Transition back according to history
            // Nothing is executed
            _ = _sceneTransitioner.PopAsync();
            yield return new WaitForSeconds(1);

            Assert.IsFalse(_sceneChanged);

            // Transition to FirstScene without leaving history
            _ = _sceneTransitioner.ReplaceAsync(SceneName.FirstScene);
            yield return WaitUntilSceneChanged();

            Assert.AreEqual(SceneName.FirstScene, _currentScene);
            Assert.AreEqual(4, SceneManager.sceneCount);
            Assert.IsTrue(SceneManager.GetSceneByName(PageName.TestFirstStage.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(PageName.TestFirstModal.ToString()).IsValid());

            // Transition back according to history
            // Nothing is executed
            _ = _sceneTransitioner.PopAsync();
            yield return new WaitForSeconds(1);

            Assert.IsFalse(_sceneChanged);
        }

        [UnityTest]
        public IEnumerator PushReplacePushPop()
        {
            // Initial Transition
            _ = _sceneTransitioner.ReplaceAsync(SceneName.FirstScene);
            yield return WaitUntilSceneChanged();

            // Transition to SecondScene with leaving history
            _ = _sceneTransitioner.PushAsync(SceneName.SecondScene);
            yield return WaitUntilSceneChanged();

            Assert.AreEqual(SceneName.SecondScene, _currentScene);

            // Transition to ThirdScene without leaving history
            _ = _sceneTransitioner.PushAsync(SceneName.ThirdScene);
            yield return WaitUntilSceneChanged();

            Assert.AreEqual(SceneName.ThirdScene, _currentScene);

            // Transition to FirstScene with leaving history
            _ = _sceneTransitioner.PushAsync(SceneName.FirstScene);
            yield return WaitUntilSceneChanged();

            Assert.AreEqual(SceneName.FirstScene, _currentScene);

            // Transition back to Third according to history
            _ = _sceneTransitioner.PopAsync();
            yield return WaitUntilSceneChanged();

            Assert.AreEqual(SceneName.ThirdScene, _currentScene);
        }

        private IEnumerator WaitUntilSceneChanged()
        {
            yield return new WaitUntil(() => _sceneChanged);
            _sceneChanged = false;
        }

        private void OnSceneChanged(SceneName scene)
        {
            _currentScene = scene;
            _sceneChanged = true;
        }
    }
}

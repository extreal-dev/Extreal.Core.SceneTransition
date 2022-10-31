using System;
using Cysharp.Threading.Tasks;

namespace Extreal.Core.SceneTransition
{
    /// <summary>
    /// Interface for implementation transitioning scenes
    /// </summary>
    /// <typeparam name="TScene">Enum for scene names</typeparam>
    public interface ISceneTransitioner<TScene>
    {
        /// <summary>
        /// Invokes when scene is transitioned
        /// </summary>
        event Action<TScene> OnSceneTransitioned;

        /// <summary>
        /// Transitions scene without leaving scene transition history
        /// </summary>
        /// <param name="scene">Scene Name to transition to</param>
        /// <returns>UniTask of this method</returns>
        UniTask ReplaceAsync(TScene scene);

        /// <summary>
        /// Transitions scene with leaving scene transition history
        /// </summary>
        /// <param name="scene">Scene Name to transition to</param>
        /// <returns>UniTask of this method</returns>
        UniTask PushAsync(TScene scene);

        /// <summary>
        /// Transitions back according to scene transition history
        /// </summary>
        /// <returns>UniTask of this method</returns>
        /// <exception cref="InvalidOperationException">If there is no scene transition history</exception>
        UniTask PopAsync();

        /// <summary>
        /// Resets scene transition history
        /// </summary>
        void Reset();
    }
}

using System;
using Cysharp.Threading.Tasks;

namespace Extreal.Core.StageNavigation
{
    /// <summary>
    /// Interface for implementation transitioning stages
    /// </summary>
    /// <typeparam name="TStage">Enum for stage names</typeparam>
    public interface IStageNavigator<TStage>
    {
        /// <summary>
        /// Invokes just before a stage transitioning
        /// </summary>
        event Action<TStage> OnStageTransitioning;

        /// <summary>
        /// Invokes immediately after a stage transitioned
        /// </summary>
        event Action<TStage> OnStageTransitioned;

        /// <summary>
        /// Transitions stage without leaving stage transition history
        /// </summary>
        /// <param name="stage">Stage Name to transition to</param>
        /// <returns>UniTask of this method</returns>
        UniTask ReplaceAsync(TStage stage);

        /// <summary>
        /// Transitions stage with leaving stage transition history
        /// </summary>
        /// <param name="stage">Stage Name to transition to</param>
        /// <returns>UniTask of this method</returns>
        UniTask PushAsync(TStage stage);

        /// <summary>
        /// Transitions back according to stage transition history
        /// </summary>
        /// <returns>UniTask of this method</returns>
        /// <exception cref="InvalidOperationException">If there is no stage transition history</exception>
        UniTask PopAsync();

        /// <summary>
        /// Resets stage transition history
        /// </summary>
        void Reset();
    }
}

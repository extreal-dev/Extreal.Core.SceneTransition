using System;
using Cysharp.Threading.Tasks;

namespace Extreal.Core.SceneTransition
{
    public interface ISceneTransitioner<TPage>
    {
        event Action<TPage> OnPageChanged;

        UniTask ReplaceAsync(TPage page);
        UniTask PushAsync(TPage page);
        UniTask PopAsync();
        void Reset();
    }
}

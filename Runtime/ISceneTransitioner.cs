using System;
using Cysharp.Threading.Tasks;

namespace Extreal.Core.SceneTransition
{
    public interface ISceneTransitioner<TScene>
    {
        event Action<TScene> OnSceneChanged;

        UniTask ReplaceAsync(TScene page);
        UniTask PushAsync(TScene page);
        UniTask PopAsync();
        void Reset();
    }
}

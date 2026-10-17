using UnityEngine;

namespace PokerOnline
{
    /// <summary>
    /// Точка входа клиента: создаёт стол до загрузки сцены,
    /// поэтому игра работает и в Play Mode, и в собранном билде без настройки сцены.
    /// </summary>
    public static class Bootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            var go = new GameObject("PokerTable");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<PokerTableController>();
        }
    }
}

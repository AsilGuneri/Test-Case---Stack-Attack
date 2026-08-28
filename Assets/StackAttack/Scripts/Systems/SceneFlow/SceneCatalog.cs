using UnityEngine;
using UnityEngine.AddressableAssets;

namespace StackAttack.Systems
{
    [CreateAssetMenu(fileName = "SceneCatalog", menuName = "Stack Attack/Scene Catalog")]
    public sealed class SceneCatalog : ScriptableObject
    {
        [SerializeField] private AssetReference menuScene = new AssetReference();
        [SerializeField] private AssetReference gameScene = new AssetReference();

        public AssetReference MenuScene => menuScene;
        public AssetReference GameScene => gameScene;
    }
}

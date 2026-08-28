using StackAttack.Gameplay;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace StackAttack.Systems
{
    public sealed class BaseLifetimeScope : LifetimeScope
    {
        [SerializeField] private SceneCatalog sceneCatalog;
        [SerializeField] private LevelCatalog levelCatalog;
        [SerializeField] private AddressableSceneFlowService sceneFlowService;
        [SerializeField] private SceneTransitionView transitionView;

        protected override void Configure(IContainerBuilder builder)
        {
            if (sceneCatalog == null || levelCatalog == null)
            {
                throw new MissingReferenceException(
                    "BaseLifetimeScope requires its scene and level catalogs.");
            }

            if (sceneFlowService == null || transitionView == null)
            {
                throw new MissingReferenceException(
                    "BaseLifetimeScope requires its scene flow service and transition view.");
            }

            builder.RegisterInstance(sceneCatalog);
            builder.RegisterInstance(levelCatalog);
            builder.RegisterComponent(transitionView);
            builder.RegisterComponent(sceneFlowService).As<ISceneFlowService>();
            builder.Register<SaveManager>(Lifetime.Scoped).As<ISaveManager>();
            builder.Register<LevelSelectionService>(Lifetime.Scoped)
                .As<ILevelSelectionService>();
            builder.RegisterEntryPoint<BaseEntryPoint>();
        }
    }
}

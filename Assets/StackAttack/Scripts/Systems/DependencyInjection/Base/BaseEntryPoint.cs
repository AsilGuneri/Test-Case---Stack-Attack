using VContainer.Unity;

namespace StackAttack.Systems
{
    public sealed class BaseEntryPoint : IStartable
    {
        private readonly ISceneFlowService _sceneFlow;

        public BaseEntryPoint(ISceneFlowService sceneFlow)
        {
            _sceneFlow = sceneFlow;
        }

        public void Start()
        {
            _sceneFlow.TryShowMenu();
        }
    }
}

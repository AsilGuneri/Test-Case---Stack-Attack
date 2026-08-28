namespace StackAttack.Systems
{
    public interface ISceneFlowService
    {
        bool IsTransitioning { get; }
        bool TryShowMenu();
        bool TryStartGame(GameStartRequest request);
    }
}

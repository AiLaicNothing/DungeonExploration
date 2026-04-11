public interface IActivator
{
    void RegisterReceiver(PuzzleReceiver receiver);
    bool IsActive { get; }
}
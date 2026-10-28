namespace CarPartManager.MVVM
{
    public interface IHasUnsavedChanges
    {
        bool HasUnsavedChanges { get; }
    }
}

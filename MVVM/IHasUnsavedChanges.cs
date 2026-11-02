namespace PharmacyManager.MVVM
{
    public interface IHasUnsavedChanges
    {
        bool HasUnsavedChanges { get; }
    }
}

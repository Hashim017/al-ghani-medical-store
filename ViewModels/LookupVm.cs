namespace AlGhaniMedicalStore.ViewModels;

public record LookupItem(int Id, string Name);

public class LookupVm
{
    public string Title { get; set; } = "";
    public string ControllerName { get; set; } = "";
    public List<LookupItem> Items { get; set; } = new();
}
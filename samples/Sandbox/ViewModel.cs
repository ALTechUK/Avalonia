using Avalonia.Collections;

namespace Sandbox;
public class ViewModel
{
    public DataGridItem Selected { get; set; }

    public AvaloniaList<string> TextItems { get; } = new()
    {
        "Item a", "Item b", "Item C", "item d"
    };

    public AvaloniaList<DataGridItem> DataGridItems { get; }

    public ViewModel()
    {
        DataGridItems = new()
        {
            new() { Name = "First", Value = TextItems[0] },
            new() { Name = "Second", Value = TextItems[1] }
        };
    }
}

public class DataGridItem
{
    public string Name { get; set; }
    public string Value { get; set; }
}

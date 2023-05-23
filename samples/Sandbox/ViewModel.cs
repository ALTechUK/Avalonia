using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Collections;

namespace Sandbox;
public class ViewModel : ViewModelBase
{
    private DataGridItem _selected;
    public DataGridItem Selected
    {
        get => _selected;
        set => RaiseAndSetIfChanged(ref _selected, value);
    }
    string _selectedTextItem = "Item C";
    public string SelectedTextItem
    {
        get => _selectedTextItem;
        set => RaiseAndSetIfChanged(ref _selectedTextItem, value);
    }

    string _itemText = "Item C";
    public string ItemText
    {
        get => _itemText;
        set => RaiseAndSetIfChanged(ref _itemText, value);
    }

    public AvaloniaList<string> TextItems { get; } = new()
    {
        "Item a", "Item b", "Item C", "item d"
    };

    public AvaloniaList<DataGridItem> DataGridItems { get; }
    public static List<ComplexItem> DgiValueOptions { get; } = new()
    {
        new() { Display = "An item", Value = "ListItem1" },
        new() { Display = "second", Value = "ListItem2" }
    };

    public ViewModel()
    {
        DataGridItems = new()
        {
            new() { Name = "First", DGI_Value = DgiValueOptions[0].Value, ComplexItem = DgiValueOptions[0] },
            new() { Name = "Second", DGI_Value = DgiValueOptions[1].Value, ComplexItem = DgiValueOptions[1] }
        };
    }
}

public class ComplexItem
{
    public string Display { get; set; }
    public string Value { get; set; }

    public override string ToString()
    {
        return $"disp: {Display}. Val: {Value}";
    }
}

public class DataGridItem : ViewModelBase
{
    private string _name = string.Empty;
    private string _value = string.Empty;
    private ComplexItem _complexItem;
    public string Name
    {
        get => _name;
        set => RaiseAndSetIfChanged(ref _name, value);
    }
    public string DGI_Value
    {
        get => _value;
        set => RaiseAndSetIfChanged(ref _value, value);
    }
    public ComplexItem ComplexItem
    {
        get => _complexItem;
        set => RaiseAndSetIfChanged(ref _complexItem, value);
    }
}


public class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;
    protected bool RaiseAndSetIfChanged<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
    {
        if (!EqualityComparer<T>.Default.Equals(field, value))
        {
            field = value;
            RaisePropertyChanged(propertyName);
            return true;
        }
        return false;
    }


    protected void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

using System.Collections.Generic;
using System.ComponentModel;
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
    string _dgiValue = string.Empty;
    public string DGIValue
    {
        get => _dgiValue;
        set => RaiseAndSetIfChanged(ref _dgiValue, value);
    }
    public ViewModel()
    {
        DataGridItems = new()
        {
            new() { Name = "First", Value = TextItems[0] },
            new() { Name = "Second", Value = TextItems[1] }
        };
    }
}

public class DataGridItem : ViewModelBase
{
    private string _name = string.Empty;
    private string _value = string.Empty;
    public string Name
    {
        get => _name;
        set => RaiseAndSetIfChanged(ref _name, value);
    }
    public string Value
    {
        get => _value;
        set => RaiseAndSetIfChanged(ref _value, value);
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

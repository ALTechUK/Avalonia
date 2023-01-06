using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Metadata;
using Avalonia.Styling;

namespace Avalonia.Controls;
public class DataGridComboBoxColumn : DataGridBoundColumn
{
    [AssignBinding]
    public virtual IBinding SelectedItemBinding
    {
        get => Binding;
        set => Binding = value;
    }
    

    /// <summary>
    /// Defines the <see cref="Items"/> property.
    /// </summary>
    public static readonly DirectProperty<DataGridComboBoxColumn, IEnumerable> ItemsProperty =
        ItemsControl.ItemsProperty.AddOwner<DataGridComboBoxColumn>(o => o.Items, (o, v) => o.Items = v);

    /// <summary>
    /// Gets or sets the items to display.
    /// </summary>
    [Content]
    public IEnumerable Items
    {
        get => _items;
        set => SetAndRaise(ItemsProperty, ref _items, value);
    }

    /// <summary>
    /// Defines the <see cref="SelectedItemTemplate"/> property.
    /// </summary>
    public static readonly StyledProperty<IDataTemplate> SelectedItemTemplateProperty =
        ComboBox.SelectedItemTemplateProperty.AddOwner<DataGridComboBoxColumn>();

    /// <summary>
    /// Gets or sets the data template used to display the item in the combo box (not the dropdown)
    /// </summary>
    public IDataTemplate SelectedItemTemplate
    {
        get => GetValue(SelectedItemTemplateProperty);
        set => SetValue(SelectedItemTemplateProperty, value);
    }

    /// <summary>
    /// Defines the <see cref="ItemTemplate"/> property.
    /// </summary>
    public static readonly StyledProperty<IDataTemplate> ItemTemplateProperty =
        ItemsControl.ItemTemplateProperty.AddOwner<DataGridComboBoxColumn>();

    /// <summary>
    /// Gets or sets the data template used to display the items in the control.
    /// </summary>
    public IDataTemplate ItemTemplate
    {
        get => GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    /// <summary>
    /// Defines the <see cref="DisplayMemberBinding" /> property
    /// </summary>
    public static readonly StyledProperty<IBinding> DisplayMemberBindingProperty =
        ItemsControl.DisplayMemberBindingProperty.AddOwner<DataGridComboBoxColumn>();

    /// <summary>
    /// Gets or sets the <see cref="IBinding"/> to use for binding to the display member of each item.
    /// </summary>
    [AssignBinding]
    public IBinding DisplayMemberBinding
    {
        get => GetValue(DisplayMemberBindingProperty);
        set => SetValue(DisplayMemberBindingProperty, value);
    }

    private IEnumerable _items = new AvaloniaList<object>();
    private readonly Lazy<ControlTheme> _cellComboBoxTheme;
    private readonly Lazy<ControlTheme> _cellTextBlockTheme;

    public DataGridComboBoxColumn()
    {
        BindingTarget = Primitives.SelectingItemsControl.SelectedItemProperty;

        _cellComboBoxTheme = new Lazy<ControlTheme>(() =>
                OwningGrid.TryFindResource("DataGridCellComboBoxTheme", out var theme) ? (ControlTheme)theme : null);
        _cellTextBlockTheme = new Lazy<ControlTheme>(() =>
            OwningGrid.TryFindResource("DataGridCellTextBlockTheme", out var theme) ? (ControlTheme)theme : null);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if(change.Property == SelectedItemTemplateProperty 
            || change.Property == DisplayMemberBindingProperty
            || change.Property == ItemsProperty)
        {
            NotifyPropertyChanged(change.Property.Name);
        }
    }

    /// <summary>
    /// Gets a <see cref="T:Avalonia.Controls.ComboBox" /> control that is bound to the column's <see cref="SelectedItemBinding"/> property value.
    /// </summary>
    /// <param name="cell">The cell that will contain the generated element.</param>
    /// <param name="dataItem">The data item represented by the row that contains the intended cell.</param>
    /// <returns>A new <see cref="T:Avalonia.Controls.ComboBox" /> control that is bound to the column's <see cref="SelectedItemBinding"/> property value.</returns>
    protected override Control GenerateEditingElementDirect(DataGridCell cell, object dataItem)
    {
        ComboBox comboBox = new ComboBox
        {
            Name = "CellComboBox"
        };

        if (_cellComboBoxTheme.Value is { } theme)
        {
            comboBox.Theme = theme;
        }

        SyncProperties(comboBox);

        return comboBox;
    }

    protected override Control GenerateElement(DataGridCell cell, object dataItem)
    {
        if (SelectedItemTemplate != null)
            return createFromTemplate(SelectedItemTemplate);

        var textBlockElement = new TextBlock
        {
            Name = "CellTextBlock"            
        };
        if (_cellTextBlockTheme.Value is { } theme)
        {
            textBlockElement.Theme = theme;        
        }

        if (Binding != null && dataItem != DataGridCollectionView.NewItemPlaceholder)
        {
            if (DisplayMemberBinding != null)
            {
                textBlockElement.Bind(StyledElement.DataContextProperty, Binding);
                textBlockElement.Bind(TextBlock.TextProperty, DisplayMemberBinding);
            }
            else
                textBlockElement.Bind(TextBlock.TextProperty, Binding);
        }
        return textBlockElement;

        Control createFromTemplate(IDataTemplate template) =>
            template is IRecyclingDataTemplate recyclingDataTemplate 
            ? recyclingDataTemplate.Build(dataItem)
            : template.Build(dataItem);
    }

    protected override object PrepareCellForEdit(Control editingElement, RoutedEventArgs editingEventArgs)
    {
        if(editingElement is ComboBox comboBox)
        {
            comboBox.IsDropDownOpen = true;
            return comboBox.SelectedItem;
        }
        return null;
    }

    private void SyncProperties(AvaloniaObject content)
    {
        //using the helper doesn't work for direct properties so set manually
        content.SetValue(ItemsControl.ItemsProperty, Items);
        DataGridHelper.SyncColumnProperty(this, content, ItemTemplateProperty);
        DataGridHelper.SyncColumnProperty(this, content, SelectedItemTemplateProperty);
        DataGridHelper.SyncColumnProperty(this, content, DisplayMemberBindingProperty);
    }

    public override bool IsReadOnly 
    {   
        get
        {
            if (OwningGrid == null)
                return base.IsReadOnly;
            if (OwningGrid.IsReadOnly)
                return true;

            string path = (Binding as Binding)?.Path ?? (Binding as CompiledBindingExtension)?.Path.ToString();
            return OwningGrid.DataConnection.PropertyIsReadOnly(path, out _);
        }
        set
        {
            base.IsReadOnly = value;
        }
    }
}

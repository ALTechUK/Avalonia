using System;
using System.Collections;
using System.ComponentModel;
using Avalonia.Collections;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Metadata;
using Avalonia.Reactive;
using Avalonia.Styling;

namespace Avalonia.Controls;
public class DataGridComboBoxColumn : DataGridBoundColumn
{
    [AssignBinding]
    [InheritDataTypeFromItems(nameof(DataGrid.ItemsSource), AncestorType = typeof(DataGrid))]
    public virtual IBinding SelectedItemBinding
    {
        get => Binding;
        set => Binding = value;
    }

    
    /// <summary>
    /// Defines the <see cref="SelectedValue"/> property
    /// </summary>
    public static readonly StyledProperty<IBinding> SelectedValueProperty =
        AvaloniaProperty.Register<DataGridComboBoxColumn, IBinding>(nameof(SelectedValue));

    /// <summary>
    /// The binding used to get the value of the selected item
    /// </summary>
    [AssignBinding]
    [InheritDataTypeFromItems(nameof(DataGrid.ItemsSource), AncestorType = typeof(DataGrid))]
    public IBinding SelectedValue
    {
        get => GetValue(SelectedValueProperty);
        set => SetValue(SelectedValueProperty, value);
    }


    /// <summary>
    /// Defines the <see cref="SelectedValueBinding"/> property
    /// </summary>
    public static readonly StyledProperty<IBinding> SelectedValueBindingProperty =
        SelectingItemsControl.SelectedValueBindingProperty.AddOwner<DataGridComboBoxColumn>();

    /// <summary>
    /// A binding used to get the value of an item selected in the combobox
    /// </summary>
    [AssignBinding]
    [InheritDataTypeFromItems(nameof(ItemsSource), AncestorType = typeof(DataGridComboBoxColumn))]
    public IBinding SelectedValueBinding
    {
        get => GetValue(SelectedValueBindingProperty);
        set => SetValue(SelectedValueBindingProperty, value);
    }


    /// <summary>
    /// Defines the <see cref="ItemsSource"/> property.
    /// </summary>
    public static readonly StyledProperty<IEnumerable> ItemsSourceProperty =
        ItemsControl.ItemsSourceProperty.AddOwner<DataGridComboBoxColumn>();

    /// <summary>
    /// Gets or sets the items to display.
    /// </summary>
    public IEnumerable ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    /// <summary>
    /// Defines the <see cref="SelectedItemTemplate"/> property.
    /// </summary>
    public static readonly StyledProperty<IDataTemplate> SelectedItemTemplateProperty =
        ComboBox.SelectedItemTemplateProperty.AddOwner<DataGridComboBoxColumn>();

    /// <summary>
    /// Gets or sets the data template used to display the item in the combo box (not the dropdown) 
    /// </summary>
    [InheritDataTypeFromItems(nameof(ItemsSource))]
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
    /// Gets or sets the data template used to display an item in the combobox
    /// </summary>
    [InheritDataTypeFromItems(nameof(DataGrid.ItemsSource), AncestorType = typeof(DataGrid))]
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
    /// A binding used in the combox to display the item
    /// </summary>
    [AssignBinding]
    [InheritDataTypeFromItems(nameof(ItemsSource))]
    public IBinding DisplayMemberBinding
    {
        get => GetValue(DisplayMemberBindingProperty);
        set => SetValue(DisplayMemberBindingProperty, value);
    }

    
    private readonly Lazy<ControlTheme> _cellComboBoxTheme;

    public DataGridComboBoxColumn()
    {
        BindingTarget = SelectingItemsControl.SelectedItemProperty;

        _cellComboBoxTheme = new Lazy<ControlTheme>(() =>
                OwningGrid.TryFindResource("DataGridCellComboBoxTheme", out var theme) ? (ControlTheme)theme : null);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if(change.Property == SelectedItemTemplateProperty 
            || change.Property == DisplayMemberBindingProperty
            || change.Property == ItemsSourceProperty)
        {
            NotifyPropertyChanged(change.Property.Name);
        }

        //if using the SelectedValue binding then the combobox needs to be bound using the selected value
        //otherwise use the default SelectedItem
        if (change.Property == SelectedValueProperty)
            BindingTarget = change.NewValue == null 
                ? SelectingItemsControl.SelectedItemProperty
                : SelectingItemsControl.SelectedValueProperty;
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
            comboBox.Theme = theme;

        SyncProperties(comboBox);

        return comboBox;
    }

    protected override Control GenerateElement(DataGridCell cell, object dataItem)
    {
        ComboBox comboBox = new ComboBox
        {
            Name = "DisplayValueComboBox",
            IsHitTestVisible = false
        };

        if (_cellComboBoxTheme.Value is { } theme)
            comboBox.Theme = theme;

        SyncProperties(comboBox);

        if (Binding != null && dataItem != DataGridCollectionView.NewItemPlaceholder)
            comboBox.Bind(BindingTarget, Binding);

        return comboBox;
    }

    protected override object PrepareCellForEdit(Control editingElement, RoutedEventArgs editingEventArgs)
    {
        if(editingElement is ComboBox comboBox)
        {
            comboBox.IsDropDownOpen = true;
            if (BindingTarget == SelectingItemsControl.SelectedValueProperty)
                return comboBox.SelectedValue;

            return comboBox.SelectedItem;
        }
        return null;
    }

    private void SyncProperties(ComboBox comboBox)
    {
        DataGridHelper.SyncColumnProperty(this, comboBox, ItemsSourceProperty);
        DataGridHelper.SyncColumnProperty(this, comboBox, ItemTemplateProperty);
        DataGridHelper.SyncColumnProperty(this, comboBox, SelectedItemTemplateProperty);
        DataGridHelper.SyncColumnProperty(this, comboBox, DisplayMemberBindingProperty);
        DataGridHelper.SyncColumnProperty(this, comboBox, SelectedValueBindingProperty);

        //if binding using SelectedItem then the DataGridBoundColumn handles that, otherwise we need to
        if (BindingTarget == SelectingItemsControl.SelectedValueProperty)
            comboBox.Bind(SelectingItemsControl.SelectedValueProperty, SelectedValue);
    }

    public override bool IsReadOnly 
    {
        get
        {
            if (OwningGrid == null)
                return base.IsReadOnly;
            if (OwningGrid.IsReadOnly)
                return true;

            IBinding valueBinding = Binding ?? SelectedValue;
            string path = (valueBinding as Binding)?.Path 
                        ?? (valueBinding as CompiledBindingExtension)?.Path.ToString();
            return OwningGrid.DataConnection.PropertyIsReadOnly(path, out _);
        }
        set
        {
            base.IsReadOnly = value;
        }
    }
}

using System;
using System.Linq;
using Avalonia.Automation.Peers;
using Avalonia.Controls.Metadata;
using Avalonia.Reactive;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Avalonia.Data;
using Avalonia.Metadata;

namespace Avalonia.Controls
{
    /// <summary>
    /// A drop-down list control.
    /// </summary>
    [TemplatePart("PART_Popup", typeof(Popup))]
    [TemplatePart("PART_InputText", typeof(TextBox))]
    [PseudoClasses(pcDropdownOpen, pcPressed)]
    public class ComboBox : SelectingItemsControl
    {
        internal const string pcDropdownOpen = ":dropdownopen";
        internal const string pcPressed = ":pressed";

        /// <summary>
        /// The default value for the <see cref="ItemsControl.ItemsPanel"/> property.
        /// </summary>
        private static readonly FuncTemplate<Panel?> DefaultPanel =
            new(() => new VirtualizingStackPanel());

        /// <summary>
        /// Defines the <see cref="IsDropDownOpen"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsDropDownOpenProperty =
            AvaloniaProperty.Register<ComboBox, bool>(nameof(IsDropDownOpen));

        /// <summary>
        /// Defines the <see cref="IsEditable"/> property.
        /// </summary>
        public static readonly DirectProperty<ComboBox, bool> IsEditableProperty =
            AvaloniaProperty.RegisterDirect<ComboBox, bool>(
                nameof(IsEditable),
                o => o.IsEditable,
                (o, v) => o.IsEditable = v);

        /// <summary>
        /// Defines the <see cref="MaxDropDownHeight"/> property.
        /// </summary>
        public static readonly StyledProperty<double> MaxDropDownHeightProperty =
            AvaloniaProperty.Register<ComboBox, double>(nameof(MaxDropDownHeight), 200);

        /// <summary>
        /// Defines the <see cref="SelectionBoxItem"/> property.
        /// </summary>
        public static readonly DirectProperty<ComboBox, object?> SelectionBoxItemProperty =
            AvaloniaProperty.RegisterDirect<ComboBox, object?>(nameof(SelectionBoxItem), o => o.SelectionBoxItem);

        /// <summary>
        /// Defines the <see cref="PlaceholderText"/> property.
        /// </summary>
        public static readonly StyledProperty<string?> PlaceholderTextProperty =
            AvaloniaProperty.Register<ComboBox, string?>(nameof(PlaceholderText));

        /// <summary>
        /// Defines the <see cref="PlaceholderForeground"/> property.
        /// </summary>
        public static readonly StyledProperty<IBrush?> PlaceholderForegroundProperty =
            AvaloniaProperty.Register<ComboBox, IBrush?>(nameof(PlaceholderForeground));

        /// <summary>
        /// Defines the <see cref="HorizontalContentAlignment"/> property.
        /// </summary>
        public static readonly StyledProperty<HorizontalAlignment> HorizontalContentAlignmentProperty =
            ContentControl.HorizontalContentAlignmentProperty.AddOwner<ComboBox>();

        /// <summary>
        /// Defines the <see cref="VerticalContentAlignment"/> property.
        /// </summary>
        public static readonly StyledProperty<VerticalAlignment> VerticalContentAlignmentProperty =
            ContentControl.VerticalContentAlignmentProperty.AddOwner<ComboBox>();

        /// <summary>
        /// Defines the <see cref="Text"/> property
        /// </summary>
        public static readonly StyledProperty< string?> TextProperty =
            TextBlock.TextProperty.AddOwner<ComboBox>(new(string.Empty, BindingMode.TwoWay));

        /// <summary>
        /// Defines the <see cref="SelectedItemTemplate"/> property.
        /// </summary>
        public static readonly StyledProperty<IDataTemplate?> SelectedItemTemplateProperty =
            AvaloniaProperty.Register<ComboBox, IDataTemplate?>(nameof(SelectedItemTemplate));

        private bool _isEditable;
        private Popup? _popup;
        private TextBox? _inputText;
        private object? _selectionBoxItem;
        private bool _ignoreNextInputTextUpdate;
        private readonly CompositeDisposable _subscriptionsOnOpen = new CompositeDisposable();

        /// <summary>
        /// Initializes static members of the <see cref="ComboBox"/> class.
        /// </summary>
        static ComboBox()
        {
            ItemsPanelProperty.OverrideDefaultValue<ComboBox>(DefaultPanel);
            FocusableProperty.OverrideDefaultValue<ComboBox>(true);
            IsTextSearchEnabledProperty.OverrideDefaultValue<ComboBox>(true);
            TextProperty.Changed.AddClassHandler<ComboBox>((x, e) => x.TextChanged(e));
            //when the items change we need to simulate a text change to validate the text being an item or not and selecting it
            ItemsSourceProperty.Changed.AddClassHandler<ComboBox>((x, e) => x.TextChanged(
                new AvaloniaPropertyChangedEventArgs<string?>(e.Sender, TextProperty, x.Text, x.Text, e.Priority)));
        }

        /// <summary>
        /// Occurs after the drop-down (popup) list of the <see cref="ComboBox"/> closes.
        /// </summary>
        public event EventHandler? DropDownClosed;

        /// <summary>
        /// Occurs after the drop-down (popup) list of the <see cref="ComboBox"/> opens.
        /// </summary>
        public event EventHandler? DropDownOpened;

        /// <summary>
        /// Gets or sets a value indicating whether the dropdown is currently open.
        /// </summary>
        public bool IsDropDownOpen
        {
            get => GetValue(IsDropDownOpenProperty);
            set => SetValue(IsDropDownOpenProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the control is editable
        /// </summary>
        public bool IsEditable
        {
            get => _isEditable;
            set => SetAndRaise(IsEditableProperty, ref _isEditable, value);
        }

        /// <summary>
        /// Gets or sets the maximum height for the dropdown list.
        /// </summary>
        public double MaxDropDownHeight
        {
            get => GetValue(MaxDropDownHeightProperty);
            set => SetValue(MaxDropDownHeightProperty, value);
        }

        /// <summary>
        /// Gets or sets the item to display as the control's content.
        /// </summary>
        public object? SelectionBoxItem
        {
            get => _selectionBoxItem;
            protected set => SetAndRaise(SelectionBoxItemProperty, ref _selectionBoxItem, value);
        }

        /// <summary>
        /// Gets or sets the PlaceHolder text.
        /// </summary>
        public string? PlaceholderText
        {
            get => GetValue(PlaceholderTextProperty);
            set => SetValue(PlaceholderTextProperty, value);
        }

        /// <summary>
        /// Gets or sets the Brush that renders the placeholder text.
        /// </summary>
        public IBrush? PlaceholderForeground
        {
            get => GetValue(PlaceholderForegroundProperty);
            set => SetValue(PlaceholderForegroundProperty, value);
        }

        /// <summary>
        /// Gets or sets the horizontal alignment of the content within the control.
        /// </summary>
        public HorizontalAlignment HorizontalContentAlignment
        {
            get => GetValue(HorizontalContentAlignmentProperty);
            set => SetValue(HorizontalContentAlignmentProperty, value);
        }

        /// <summary>
        /// Gets or sets the vertical alignment of the content within the control.
        /// </summary>
        public VerticalAlignment VerticalContentAlignment
        {
            get => GetValue(VerticalContentAlignmentProperty);
            set => SetValue(VerticalContentAlignmentProperty, value);
        }

        /// <summary>
        /// Gets or sets the text used when <see cref="IsEditable"/> is true.
        /// </summary>
        public string? Text
        {
            get => GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        /// <summary>
        /// Gets or sets the data template used to display the item in the combo box when collapsed
        /// </summary>
        [InheritDataTypeFromItems(nameof(ItemsSource))]
        public IDataTemplate? SelectedItemTemplate
        {
            get => GetValue(SelectedItemTemplateProperty);
            set => SetValue(SelectedItemTemplateProperty, value);
        }

        /// <inheritdoc/>
        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            UpdateSelectionBoxItem(SelectedItem);
        }

        protected internal override void InvalidateMirrorTransform()
        {
            base.InvalidateMirrorTransform();
            UpdateFlowDirection();
        }

        protected internal override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey)
        {
            return new ComboBoxItem();
        }

        protected internal override bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
        {
            return NeedsContainer<ComboBoxItem>(item, out recycleKey);
        }

        /// <inheritdoc/>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Handled)
                return;

            if ((e.Key == Key.F4 && e.KeyModifiers.HasAllFlags(KeyModifiers.Alt) == false) ||
                ((e.Key == Key.Down || e.Key == Key.Up) && e.KeyModifiers.HasAllFlags(KeyModifiers.Alt)))
            {
                SetCurrentValue(IsDropDownOpenProperty, !IsDropDownOpen);
                e.Handled = true;
            }
            else if (IsDropDownOpen && e.Key == Key.Escape)
            {
                SetCurrentValue(IsDropDownOpenProperty, false);
                e.Handled = true;
            }
            else if (!IsDropDownOpen && !IsEditable && (e.Key == Key.Enter || e.Key == Key.Space))
            {
                SetCurrentValue(IsDropDownOpenProperty, true);
                e.Handled = true;
            }
            else if (IsDropDownOpen && (e.Key == Key.Enter || e.Key == Key.Space))
            {
                SelectFocusedItem();
                SetCurrentValue(IsDropDownOpenProperty, false);
                e.Handled = true;
            }
            else if (!IsDropDownOpen)
            {
                if (e.Key == Key.Down)
                {
                    SelectNext();
                    e.Handled = true;
                }
                else if (e.Key == Key.Up)
                {
                    SelectPrevious();
                    e.Handled = true;
                }
            }
            // This part of code is needed just to acquire initial focus, subsequent focus navigation will be done by ItemsControl.
            else if (IsDropDownOpen && SelectedIndex < 0 && ItemCount > 0 &&
                     (e.Key == Key.Up || e.Key == Key.Down) && IsFocused == true)
            {
                var firstChild = Presenter?.Panel?.Children.FirstOrDefault(c => CanFocus(c));
                if (firstChild != null)
                {
                    e.Handled = firstChild.Focus(NavigationMethod.Directional);
                }
            }
        }

        /// <inheritdoc/>
        protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
        {
            base.OnPointerWheelChanged(e);

            if (!e.Handled)
            {
                if (!IsDropDownOpen)
                {
                    if (IsFocused)
                    {
                        if (e.Delta.Y < 0)
                            SelectNext();
                        else
                            SelectPrevious();

                        e.Handled = true;
                    }
                }
                else
                {
                    e.Handled = true;
                }
            }
        }

        /// <inheritdoc/>
        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);
            if(!e.Handled && e.Source is Visual source)
            {
                if (_popup?.IsInsidePopup(source) == true)
                {
                    e.Handled = true;
                    return;
                }
            }
            PseudoClasses.Set(pcPressed, true);
        }

        /// <inheritdoc/>
        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            //if the user clicked in the input text we don't want to open the dropdown
            if (_inputText != null 
                && !e.Handled 
                && e.Source is StyledElement styledSource 
                && styledSource.TemplatedParent == _inputText)
            {
                return;
            }

            if (!e.Handled && e.Source is Visual source)
            {
                if (_popup?.IsInsidePopup(source) == true)
                {
                    if (UpdateSelectionFromEventSource(e.Source))
                    {
                        _popup?.Close();
                        e.Handled = true;
                    }
                }
                else
                {
                    SetCurrentValue(IsDropDownOpenProperty, !IsDropDownOpen);
                    e.Handled = true;
                }
            }

            PseudoClasses.Set(pcPressed, false);
            base.OnPointerReleased(e);
        }

        /// <inheritdoc/>
        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            if (_popup != null)
            {
                _popup.Opened -= PopupOpened;
                _popup.Closed -= PopupClosed;
            }

            _popup = e.NameScope.Get<Popup>("PART_Popup");
            _popup.Opened += PopupOpened;
            _popup.Closed += PopupClosed;

            _inputText = e.NameScope.Get<TextBox>("PART_InputText");
        }

        /// <inheritdoc/>
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            if (change.Property == SelectedItemProperty)
            {
                UpdateSelectionBoxItem(change.NewValue);
                TryFocusSelectedItem();
                UpdateInputTextFromSelection(change.NewValue);
            }
            else if (change.Property == IsDropDownOpenProperty)
            {
                PseudoClasses.Set(pcDropdownOpen, change.GetNewValue<bool>());
            }
            else if (change.Property == IsEditableProperty && change.GetNewValue<bool>())
            {
                UpdateInputTextFromSelection(SelectedItem);
            }
            else if(change.Property == SelectedItemTemplateProperty
                 || change.Property == ItemTemplateProperty 
                 || change.Property == DisplayMemberBindingProperty)
            {
                CheckAndUpdateSelectedItemTemplate();
            }

            base.OnPropertyChanged(change);
        }

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new ComboBoxAutomationPeer(this);
        }

        internal void ItemFocused(ComboBoxItem dropDownItem)
        {
            if (IsDropDownOpen && dropDownItem.IsFocused && dropDownItem.IsArrangeValid)
            {
                dropDownItem.BringIntoView();
            }
        }

        private void PopupClosed(object? sender, EventArgs e)
        {
            _subscriptionsOnOpen.Clear();

            if (CanFocus(this))
            {
                Focus();
            }

            DropDownClosed?.Invoke(this, EventArgs.Empty);
        }

        private void PopupOpened(object? sender, EventArgs e)
        {
            TryFocusSelectedItem();

            _subscriptionsOnOpen.Clear();

            this.GetObservable(IsVisibleProperty).Subscribe(IsVisibleChanged).DisposeWith(_subscriptionsOnOpen);

            foreach (var parent in this.GetVisualAncestors().OfType<Control>())
            {
                parent.GetObservable(IsVisibleProperty).Subscribe(IsVisibleChanged).DisposeWith(_subscriptionsOnOpen);
            }

            UpdateFlowDirection();

            DropDownOpened?.Invoke(this, EventArgs.Empty);
        }

        private void IsVisibleChanged(bool isVisible)
        {
            if (!isVisible && IsDropDownOpen)
            {
                SetCurrentValue(IsDropDownOpenProperty, false);
            }
        }

        private void TryFocusSelectedItem()
        {
            var selectedIndex = SelectedIndex;
            if (IsDropDownOpen && selectedIndex != -1)
            {
                var container = ContainerFromIndex(selectedIndex);

                if (container == null && SelectedIndex != -1)
                {
                    ScrollIntoView(Selection.SelectedIndex);
                    container = ContainerFromIndex(selectedIndex);
                }

                if (container != null && CanFocus(container))
                {
                    container.Focus();
                }
            }
        }

        private bool CanFocus(Control control) => control.Focusable && control.IsEffectivelyEnabled && control.IsVisible;

        private void UpdateSelectionBoxItem(object? item)
        {
            var contentControl = item as IContentControl;

            if (contentControl != null)
            {
                item = contentControl.Content;
            }

            var control = item as Control;

            if (control != null)
            {
                if (VisualRoot is object)
                {
                    control.Measure(Size.Infinity);

                    SelectionBoxItem = new Rectangle
                    {
                        Width = control.DesiredSize.Width,
                        Height = control.DesiredSize.Height,
                        Fill = new VisualBrush
                        {
                            Visual = control,
                            Stretch = Stretch.None,
                            AlignmentX = AlignmentX.Left,
                        }
                    };
                }

                UpdateFlowDirection();
            }
            else
            {
                SelectionBoxItem = item;
            }
        }

        private void UpdateInputTextFromSelection(object? item)
        {
            if (_ignoreNextInputTextUpdate)
                return;

            _ignoreNextInputTextUpdate = true;
            string text;
            if (item is IContentControl cbItem)
                text = cbItem.Content?.ToString() ?? string.Empty;
            else
                text = item?.ToString() ?? string.Empty;
            
            SetCurrentValue(TextProperty, text);
            _ignoreNextInputTextUpdate = false;
        }

        private void UpdateFlowDirection()
        {
            if (SelectionBoxItem is Rectangle rectangle)
            {
                if ((rectangle.Fill as VisualBrush)?.Visual is Visual content)
                {
                    var flowDirection = content.VisualParent?.FlowDirection ?? FlowDirection.LeftToRight;
                    rectangle.FlowDirection = flowDirection;
                }
            }
        }

        private void SelectFocusedItem()
        {
            foreach (var dropdownItem in GetRealizedContainers())
            {
                if (dropdownItem.IsFocused)
                {
                    SelectedIndex = IndexFromContainer(dropdownItem);
                    break;
                }
            }
        }

        private void SelectNext() => MoveSelection(SelectedIndex, 1, WrapSelection);
        private void SelectPrevious() => MoveSelection(SelectedIndex, -1, WrapSelection);

        private void MoveSelection(int startIndex, int step, bool wrap)
        {
            static bool IsSelectable(object? o) => (o as AvaloniaObject)?.GetValue(IsEnabledProperty) ?? true;

            var count = ItemCount;

            for (int i = startIndex + step; i != startIndex; i += step)
            {
                if (i < 0 || i >= count)
                {
                    if (wrap)
                    {
                        if (i < 0)
                            i += count;
                        else if (i >= count)
                            i %= count;
                    }
                    else
                    {
                        return;
                    }
                }

                var item = ItemsView[i];
                var container = ContainerFromIndex(i);
                
                if (IsSelectable(item) && IsSelectable(container))
                {
                    SelectedIndex = i;
                    break;
                }
            }
        }

        private void TextChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (Items == null || !IsEditable || _ignoreNextInputTextUpdate)
                return;

            string newVal = e.GetNewValue<string>();
            int selectedIdx = -1;
            object? selectedItem = null;
            string? selectedItemText = null;
            int i = -1;
            foreach (object? o in Items)
            {
                i++;
                string? text = o is IContentControl contentControl 
                    ? contentControl.Content?.ToString() 
                    : o?.ToString();

                if (string.Equals(newVal, text, StringComparison.CurrentCultureIgnoreCase))
                {
                    selectedIdx = i;
                    selectedItem = o;
                    selectedItemText = text;
                    break;
                }
            }
            bool settingSelectedItem = selectedIdx > -1 && SelectedIndex != selectedIdx;

            _ignoreNextInputTextUpdate = true;
            SelectedIndex = selectedIdx;
            SelectedItem = selectedItem;
            if (settingSelectedItem)
                SetCurrentValue(TextProperty, selectedItemText ?? newVal);
            _ignoreNextInputTextUpdate = false;
        }

        /// <summary>
        /// If the <see cref="SelectedItemTemplate"/> is null and the <see cref="ItemsControl.DisplayMemberBinding"/>
        /// is something then set the template to use the display member
        /// </summary>
        private void CheckAndUpdateSelectedItemTemplate()
        {
            if(SelectedItemTemplate == null && ItemTemplate == null && DisplayMemberBinding != null)
            {
                //similar to the FuncDataTemplate.Default, but instead of binding the text to the datacontext,
                //we bind the text the same way ItemContainerGenerator does it
                SetCurrentValue(SelectedItemTemplateProperty, new FuncDataTemplate<object?>(
                    (data, s) =>
                    {
                        TextBlock result = new();
                        result.Bind(TextBlock.TextProperty, DisplayMemberBinding, BindingPriority.Style);
                        return result;
                    }, true)
                );
            }
        }

        /// <summary>
        /// Clears the selection
        /// </summary>
        public void Clear()
        {
            SelectedItem = null;
            SelectedIndex = -1;
        }
    }
}

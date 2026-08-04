using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree; // 🚀 【关键引入】用于在 visual 树中向上追踪 ListBoxItem
using Avalonia.Interactivity;
using MCAJNLP.ViewModels;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MCAJNLP.Views
{
    public partial class MainWindow : Window
    {
        // 拖拽状态
        private Point _dragStartPoint;
        private bool _isPressed;
        private string? _draggedKey;

        public MainWindow()
        {
            InitializeComponent();
            var viewModel = new MainWindowViewModel();
            DataContext = viewModel;

            // 完美的 ListBox 交互注册
            var listBox = this.FindControl<ListBox>("VersionListBox");
            if (listBox != null)
            {
                // 1. 完美保留双击编辑
                listBox.DoubleTapped += (s, e) =>
                {
                    if (listBox.SelectedItem is string selectedKey)
                    {
                        viewModel.LoadToForm(selectedKey);
                    }
                };

                // 2. 🚀【核心修复】使用 handledEventsToo: true 截获被底层 ListBoxItem 拦截的 Pointer 事件
                listBox.AddHandler(InputElement.PointerPressedEvent, OnListBoxPointerPressed, RoutingStrategies.Bubble, true);
                listBox.AddHandler(InputElement.PointerMovedEvent, OnListBoxPointerMoved, RoutingStrategies.Bubble, true);
                listBox.AddHandler(InputElement.PointerReleasedEvent, OnListBoxPointerReleased, RoutingStrategies.Bubble, true);

                // 3. 注册容器级拖放监听
                DragDrop.SetAllowDrop(listBox, true);
                listBox.AddHandler(DragDrop.DragOverEvent, OnListBoxDragOver);
                listBox.AddHandler(DragDrop.DropEvent, OnListBoxDrop);
            }
        }

        private void OpenGitHub_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            string url = "https://github.com/Hawk/MCAHTML"; 
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Process.Start("xdg-open", url);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start("open", url);
                }
            }
            catch
            {
                // 忽略异常，防止意外崩溃
            }
        }

        // 1. 鼠标按下：截获点击，利用 visual 树自动追溯出当前的 ListBoxItem
        private void OnListBoxPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            var pointerProperties = e.GetCurrentPoint(this).Properties;
            if (pointerProperties.IsLeftButtonPressed)
            {
                // 哪怕点击的是文字，也会向上追溯到其父级 ListBoxItem
                if (e.Source is Control sourceControl)
                {
                    var listBoxItem = sourceControl.FindAncestorOfType<ListBoxItem>(); //
                    if (listBoxItem != null && listBoxItem.DataContext is string versionKey)
                    {
                        _dragStartPoint = e.GetPosition(this);
                        _isPressed = true;
                        _draggedKey = versionKey;
                    }
                }
            }
        }

        // 2. 鼠标移动：滑动距离大于 5 像素直接激活拖放
        private async void OnListBoxPointerMoved(object? sender, PointerEventArgs e)
        {
            if (!_isPressed || string.IsNullOrEmpty(_draggedKey)) return;

            var currentPos = e.GetPosition(this);
            var deltaX = Math.Abs(currentPos.X - _dragStartPoint.X);
            var deltaY = Math.Abs(currentPos.Y - _dragStartPoint.Y);

            // 移动超过5像素说明不是正常点击，而是用户故意在拖拽
            if (deltaX > 5 || deltaY > 5)
            {
                _isPressed = false; // 立即消费掉，防止在一次拖动中重复初始化触发报错

                var dragData = new DataObject();
                dragData.Set(DataFormats.Text, _draggedKey);

                // 🚀 唤醒并跟踪系统级拖放进程 (注：若编译提示找不到 DoDragDrop，可将其更名为 DoDragDropAsync)
                await DragDrop.DoDragDrop(e, dragData, DragDropEffects.Move);

                _draggedKey = null; // 释放拖拽锁
            }
        }

        private void OnListBoxPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            _isPressed = false;
            _draggedKey = null;
        }

        // 3. 拖拽悬停：检查拖拽物格式是否为内部版本 Key 文本，强制显示为可拖动
        private void OnListBoxDragOver(object? sender, DragEventArgs e)
        {
            var draggedKey = e.Data.Get(DataFormats.Text) as string;
            
            if (DataContext is MainWindowViewModel vm && !string.IsNullOrEmpty(draggedKey) && vm.VersionKeys.Contains(draggedKey))
            {
                e.DragEffects = DragDropEffects.Move;
            }
            else
            {
                e.DragEffects = DragDropEffects.None;
            }
        }

        // 4. 放开落下：利用 FindAncestor 溯源释放位置对应的 ListBoxItem 触发位置挪动
        private void OnListBoxDrop(object? sender, DragEventArgs e)
        {
            _isPressed = false;
            _draggedKey = null;

            var draggedKey = e.Data.Get(DataFormats.Text) as string;
            if (string.IsNullOrEmpty(draggedKey)) return;

            if (e.Source is Control sourceControl)
            {
                // 向上追溯到鼠标落下时对应的具体 ListBoxItem
                var targetItem = sourceControl.FindAncestorOfType<ListBoxItem>();
                if (targetItem != null && targetItem.DataContext is string targetKey)
                {
                    if (draggedKey == targetKey) return;

                    if (DataContext is MainWindowViewModel vm && vm.VersionKeys.Contains(draggedKey))
                    {
                        // 触发排序并重构 JSON 文件
                        vm.MoveVersion(draggedKey, targetKey);
                    }
                }
            }
        }
    }
}
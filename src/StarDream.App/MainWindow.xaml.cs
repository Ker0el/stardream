using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;using StarDream.Core.Editing;
using StarDream.Core.Export;
using StarDream.Core.Math;
using StarDream.Core.Scene;

namespace StarDream.App;

public sealed partial class MainWindow : Window
{
    private bool _syncing;
    private bool _recoveryChecked;
    private SceneObject? _clipboard;

    private const int UndoLimit = 64;
    private readonly List<string> _undo = new();
    private readonly List<string> _redo = new();
    private string _lastUndoTag = string.Empty;
    private DateTime _lastUndoAt = DateTime.MinValue;

    /// <summary>Snapshots the scene before a mutation. Repeated edits of the same kind within
    /// a second collapse into one entry so dragging a number box does not flood the stack.</summary>
    private void PushUndo(string tag)
    {
        var now = DateTime.UtcNow;
        if (tag == _lastUndoTag && (now - _lastUndoAt).TotalMilliseconds < 800)
        {
            return;
        }

        _lastUndoTag = tag;
        _lastUndoAt = now;
        _undo.Add(SceneSerializer.Save(Document));
        if (_undo.Count > UndoLimit)
        {
            _undo.RemoveAt(0);
        }

        _redo.Clear();
    }

    private void OnUndo(object sender, RoutedEventArgs e)
    {
        if (_undo.Count == 0)
        {
            StatusText.Text = "没有可撤销的操作";
            return;
        }

        _redo.Add(SceneSerializer.Save(Document));
        var json = _undo[^1];
        _undo.RemoveAt(_undo.Count - 1);

        SceneSerializer.LoadInto(Document, json);
        Document.IsDirty = true;
        RefreshOutliner();
        RefreshProperties();
        UpdateStatus();
        StatusText.Text = "已撤销";
    }

    private void OnRedo(object sender, RoutedEventArgs e)
    {
        if (_redo.Count == 0)
        {
            StatusText.Text = "没有可重做的操作";
            return;
        }

        _undo.Add(SceneSerializer.Save(Document));
        var json = _redo[^1];
        _redo.RemoveAt(_redo.Count - 1);

        SceneSerializer.LoadInto(Document, json);
        Document.IsDirty = true;
        RefreshOutliner();
        RefreshProperties();
        UpdateStatus();
        StatusText.Text = "已重做";
    }
    private readonly DispatcherTimer _autoSaveTimer = new() { Interval = TimeSpan.FromSeconds(30) };

    private static string AutoSavePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "StarDream",
        "autosave.stardream");

    public MainWindow()
    {
        InitializeComponent();
        SystemBackdrop = new DesktopAcrylicBackdrop();
        LoadDemoScene();
        ViewportHost.Document.SelectionChanged += OnSelectionChanged;
        ViewportHost.BeforeMutation += () => PushUndo("gizmo");

        _autoSaveTimer.Tick += OnAutoSaveTick;
        _autoSaveTimer.Start();

        // The recovery dialog needs a live XamlRoot, which only exists once the
        // window content is loaded; asking earlier throws E_INVALIDARG.
        if (Content is FrameworkElement root)
        {
            root.Loaded += OnRootLoaded;
        }
    }

    private void OnRootLoaded(object sender, RoutedEventArgs e)
    {
        if (Content is FrameworkElement root)
        {
            root.Loaded -= OnRootLoaded;
        }

        _ = CheckForRecoveredDraftAsync();
    }

    private void OnAutoSaveTick(object? sender, object e)
    {
        if (!Document.IsDirty)
        {
            return;
        }

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(AutoSavePath)!);
            File.WriteAllText(AutoSavePath, SceneSerializer.Save(Document));
        }
        catch (IOException)
        {
            // A failed draft write must never interrupt editing.
        }
    }

    private async System.Threading.Tasks.Task CheckForRecoveredDraftAsync()
    {
        if (_recoveryChecked)
        {
            return;
        }

        _recoveryChecked = true;

        if (!File.Exists(AutoSavePath))
        {
            return;
        }

        var dialog = new ContentDialog
        {
            XamlRoot = Content.XamlRoot,
            Title = "发现未保存的内容",
            Content = "上次退出前有未保存的修改，要恢复吗？",
            PrimaryButtonText = "恢复",
            CloseButtonText = "丢弃",
            DefaultButton = ContentDialogButton.Primary,
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            try
            {
                SceneSerializer.LoadInto(Document, File.ReadAllText(AutoSavePath));
                Document.IsDirty = true;
                RefreshOutliner();
                RefreshProperties();
                UpdateStatus();
            }
            catch (SceneLoadException)
            {
                StatusText.Text = "草稿文件已损坏，无法恢复";
            }
        }

        File.Delete(AutoSavePath);
        UpdateStatus();
    }

    private SceneDocument Document => ViewportHost.Document;

    private SceneObject? Selected => Document.Selected;

    /// <summary>Temporary content so the viewport is not empty on first launch.</summary>
    private void LoadDemoScene()
    {
        var demo = DemoScene.Create();

        foreach (var source in demo.Objects)
        {
            var target = Document.AddPrimitive(source.Primitive ?? PrimitiveParams.Cube());
            target.Position = source.Position;
            target.ColorHex = source.ColorHex;
            target.Name = source.Name;
        }

        Document.Select(null);
        Document.Camera.Target = new Vec3(0f, 0.5f, 0f);
        Document.Camera.Yaw = 0.6f;
        Document.Camera.Pitch = 0.4f;
        Document.Camera.Distance = 12f;
        Document.IsDirty = false;

        RefreshOutliner();
        RefreshProperties();
        Document.IsDirty = false;
        UpdateStatus();
    }

    private void OnSelectionChanged()
    {
        RefreshOutliner();
        RefreshProperties();
        UpdateStatus();
    }

    private void RefreshOutliner()
    {
        _syncing = true;
        try
        {
            OutlinerList.ItemsSource = Document.Objects.Select(o => o.Name).ToList();
            OutlinerList.SelectedIndex = Selected is null ? -1 : Document.Objects.IndexOf(Selected);
        }
        finally
        {
            _syncing = false;
        }
    }

    private void RefreshProperties()
    {
        _syncing = true;
        try
        {
            var obj = Selected;
            var enabled = obj is not null;

            NameBox.IsEnabled = enabled;
            PosX.IsEnabled = enabled;
            PosY.IsEnabled = enabled;
            PosZ.IsEnabled = enabled;
            RotX.IsEnabled = enabled;
            RotY.IsEnabled = enabled;
            RotZ.IsEnabled = enabled;
            ScaleX.IsEnabled = enabled;
            ScaleY.IsEnabled = enabled;
            ScaleZ.IsEnabled = enabled;
            ColorBox.IsEnabled = enabled;
            VisibleCheck.IsEnabled = enabled;

            if (obj is null)
            {
                NameBox.Text = string.Empty;
                ColorBox.Text = string.Empty;
                VisibleCheck.IsChecked = false;
                return;
            }

            NameBox.Text = obj.Name;
            PosX.Value = obj.Position.X;
            PosY.Value = obj.Position.Y;
            PosZ.Value = obj.Position.Z;
            RotX.Value = obj.Rotation.X;
            RotY.Value = obj.Rotation.Y;
            RotZ.Value = obj.Rotation.Z;
            ScaleX.Value = obj.Scale.X;
            ScaleY.Value = obj.Scale.Y;
            ScaleZ.Value = obj.Scale.Z;
            ColorBox.Text = obj.ColorHex;
            VisibleCheck.IsChecked = obj.Visible;
        }
        finally
        {
            _syncing = false;
        }
    }

    private void UpdateStatus()
    {
        var mode = ViewportHost.IsEditMode
            ? $"编辑模式 · {ElementName(ViewportHost.Edit.Selection.Mode)} · 选中 {ViewportHost.Edit.Selection.Count}"
            : Selected is null
                ? "无选中"
                : $"已选中 {Selected.Name} · {ToolName(ViewportHost)}";

        StatusText.Text = $"{mode} · {Document.Objects.Count} 个对象 · {Document.TriangleCount} 三角形";
        Title = Document.IsDirty ? "星梦 — 未保存*" : "星梦";
    }

    private static string ToolName(ViewportControl viewport)
    {
        if (!viewport.GizmoVisible)
        {
            return "选择";
        }

        return viewport.GizmoMode switch
        {
            GizmoMode.Translate => "移动",
            GizmoMode.Rotate => "旋转",
            _ => "缩放",
        };
    }

    private static string ElementName(ElementMode mode) => mode switch
    {
        ElementMode.Vertex => "点",
        ElementMode.Edge => "线",
        _ => "面",
    };

    private void OnOutlinerSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_syncing || OutlinerList.SelectedIndex < 0 || OutlinerList.SelectedIndex >= Document.Objects.Count)
        {
            return;
        }

        Document.Select(Document.Objects[OutlinerList.SelectedIndex]);
    }

    private void OnPropertyEdited(object sender, TextChangedEventArgs e)
    {
        if (_syncing || Selected is null)
        {
            return;
        }

        if (ReferenceEquals(sender, NameBox))
        {
            Selected.Name = NameBox.Text;
            Document.IsDirty = true;
            RefreshOutliner();
        }
        else if (ReferenceEquals(sender, ColorBox))
        {
            Selected.ColorHex = ColorBox.Text;
            Document.IsDirty = true;
        }

        UpdateStatus();
    }

    private void OnNumberChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (_syncing || Selected is null || double.IsNaN(args.NewValue))
        {
            return;
        }

        var obj = Selected;
        PushUndo("transform");
        obj.Position = new Vec3((float)PosX.Value, (float)PosY.Value, (float)PosZ.Value);
        obj.Rotation = new Vec3((float)RotX.Value, (float)RotY.Value, (float)RotZ.Value);
        obj.Scale = new Vec3(
            MathF.Max(0.01f, (float)ScaleX.Value),
            MathF.Max(0.01f, (float)ScaleY.Value),
            MathF.Max(0.01f, (float)ScaleZ.Value));
        Document.IsDirty = true;
        UpdateStatus();
    }

    private void OnVisibleToggled(object sender, RoutedEventArgs e)
    {
        if (_syncing || Selected is null)
        {
            return;
        }

        PushUndo("visibility");
        Document.SetVisible(Selected, VisibleCheck.IsChecked == true);
        UpdateStatus();
    }

    private void OnCutKey(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        => OnCut(sender, new RoutedEventArgs());

    private void OnCopyKey(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        => OnCopy(sender, new RoutedEventArgs());

    private void OnPasteKey(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        => OnPaste(sender, new RoutedEventArgs());

    private void OnUndoKey(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        => OnUndo(sender, new RoutedEventArgs());

    private void OnRedoKey(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        => OnRedo(sender, new RoutedEventArgs());

    private void OnNewKey(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        => OnNewScene(sender, new RoutedEventArgs());

    private void OnOpenKey(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        => OnOpenScene(sender, new RoutedEventArgs());

    private void OnSaveKey(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        => OnSaveScene(sender, new RoutedEventArgs());

    private void OnSelectTool(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        ViewportHost.GizmoVisible = false;
        UpdateStatus();
    }

    private void OnTranslateTool(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        => SetGizmo(GizmoMode.Translate);

    private void OnRotateTool(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        => SetGizmo(GizmoMode.Rotate);

    private void OnScaleTool(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        => SetGizmo(GizmoMode.Scale);

    private void SetGizmo(GizmoMode mode)
    {
        ViewportHost.GizmoMode = mode;
        ViewportHost.GizmoVisible = true;
        UpdateStatus();
    }

    private void OnToggleEditMode(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        if (!ViewportHost.ToggleEditMode())
        {
            StatusText.Text = "先选中一个物体，再按 F8 进入编辑模式";
            return;
        }

        UpdateStatus();
    }

    private void OnVertexMode(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        ViewportHost.SetElementMode(ElementMode.Vertex);
        UpdateStatus();
    }

    private void OnEdgeMode(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        ViewportHost.SetElementMode(ElementMode.Edge);
        UpdateStatus();
    }

    private void OnFaceMode(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        ViewportHost.SetElementMode(ElementMode.Face);
        UpdateStatus();
    }

    private void OnDeleteKey(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        if (ViewportHost.IsEditMode && ViewportHost.Edit.Target is not null)
        {
            var target = ViewportHost.Edit.Target;
            PushUndo("delete-element");
            MeshEditor.Delete(target.GetMesh(), ViewportHost.Edit.Selection);
            Document.IsDirty = true;
            UpdateStatus();
            return;
        }

        OnDelete(sender, new RoutedEventArgs());
    }

    private void OnExtrudeKey(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        if (!ViewportHost.IsEditMode || ViewportHost.Edit.Target is null)
        {
            return;
        }

        var target = ViewportHost.Edit.Target;
        PushUndo("extrude");
        var result = MeshEditor.Extrude(
            target.GetMesh(), ViewportHost.Edit.Selection, MeshEditor.DefaultExtrudeDistance);

        StatusText.Text = result.Succeeded
            ? $"已挤出 {result.NewFaceIndices.Count} 个面"
            : "挤出失败：" + result.Error;

        if (result.Succeeded)
        {
            Document.IsDirty = true;
            UpdateStatus();
        }
    }

    private void OnDuplicateKey(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        OnDuplicate(sender, new RoutedEventArgs());
    }

    private void OnFrameSelected(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        if (Selected is null)
        {
            return;
        }

        var mesh = Selected.GetMesh();
        float radius = 1f;
        foreach (var position in mesh.Positions)
        {
            radius = MathF.Max(radius, position.Length);
        }

        Document.Camera.Frame(Selected.Position, radius);
    }

    private void OnAddCube(object sender, RoutedEventArgs e) => AddPrimitive(PrimitiveParams.Cube());

    private void OnAddSphere(object sender, RoutedEventArgs e) => AddPrimitive(PrimitiveParams.Sphere());

    private void OnAddCylinder(object sender, RoutedEventArgs e) => AddPrimitive(PrimitiveParams.Cylinder());

    private void OnAddPlane(object sender, RoutedEventArgs e) => AddPrimitive(PrimitiveParams.Plane());

    private void AddPrimitive(PrimitiveParams parameters)
    {
        PushUndo("add");
        Document.AddPrimitive(parameters);
        UpdateStatus();
    }

    private void OnCut(object sender, RoutedEventArgs e)
    {
        if (Selected is null)
        {
            return;
        }

        _clipboard = Selected.CloneWithNewId();
        PushUndo("cut");
        Document.Remove(Selected);
        UpdateStatus();
        StatusText.Text = $"已剪切 {_clipboard.Name}，Ctrl+V 粘贴";
    }

    private void OnCopy(object sender, RoutedEventArgs e)
    {
        if (Selected is null)
        {
            return;
        }

        _clipboard = Selected.CloneWithNewId();
        StatusText.Text = $"已复制 {_clipboard.Name}，Ctrl+V 粘贴";
    }

    private void OnPaste(object sender, RoutedEventArgs e)
    {
        if (_clipboard is null)
        {
            StatusText.Text = "剪贴板是空的";
            return;
        }

        var copy = _clipboard.CloneWithNewId();
        copy.Name = _clipboard.Name;
        copy.Position = copy.Position + new Vec3(0.5f, 0f, 0f);
        PushUndo("paste");
        Document.Add(copy);
        UpdateStatus();
        StatusText.Text = $"已粘贴 {copy.Name}";
    }

    private void OnDuplicate(object sender, RoutedEventArgs e)
    {
        if (Selected is not null)
        {
            PushUndo("duplicate");
            Document.Duplicate(Selected);
            UpdateStatus();
        }
    }

    private void OnDelete(object sender, RoutedEventArgs e)
    {
        if (Selected is not null)
        {
            PushUndo("delete");
            Document.Remove(Selected);
            UpdateStatus();
        }
    }

    private void OnOutlinerKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Delete)
        {
            e.Handled = true;
            OnDelete(sender, new RoutedEventArgs());
        }
    }

    private void OnToggleVisibleFromOutliner(object sender, RoutedEventArgs e)
    {
        if (Selected is null)
        {
            return;
        }

        Document.SetVisible(Selected, !Selected.Visible);
        RefreshProperties();
        UpdateStatus();
    }

    private void OnFrameAll(object sender, RoutedEventArgs e)
    {
        Document.Camera.Frame(new Vec3(0f, 0.5f, 0f), 4f);
    }

    private void OnNewScene(object sender, RoutedEventArgs e)
    {
        Document.Clear();
        RefreshOutliner();
        RefreshProperties();
        UpdateStatus();
    }

    private async void OnOpenScene(object sender, RoutedEventArgs e)
    {
        var picker = new Windows.Storage.Pickers.FileOpenPicker();
        WinRT.Interop.InitializeWithWindow.Initialize(
            picker, WinRT.Interop.WindowNative.GetWindowHandle(this));
        picker.FileTypeFilter.Add(".stardream");

        var file = await picker.PickSingleFileAsync();
        if (file is null)
        {
            return;
        }

        try
        {
            var json = await File.ReadAllTextAsync(file.Path);
            SceneSerializer.LoadInto(Document, json);
            Document.FilePath = file.Path;
            RefreshOutliner();
            RefreshProperties();
            UpdateStatus();
            StatusText.Text = $"已打开 {Path.GetFileName(file.Path)}";
        }
        catch (SceneLoadException ex)
        {
            StatusText.Text = "打开失败：" + ex.Message;
        }
        catch (IOException ex)
        {
            StatusText.Text = "读取失败：" + ex.Message;
        }
    }

    private async void OnSaveScene(object sender, RoutedEventArgs e)
    {
        var path = Document.FilePath;

        if (string.IsNullOrEmpty(path))
        {
            var picker = new Windows.Storage.Pickers.FileSavePicker();
            WinRT.Interop.InitializeWithWindow.Initialize(
                picker, WinRT.Interop.WindowNative.GetWindowHandle(this));
            picker.FileTypeChoices.Add("星梦场景", new List<string> { ".stardream" });
            picker.SuggestedFileName = "场景";

            var file = await picker.PickSaveFileAsync();
            if (file is null)
            {
                return;
            }

            path = file.Path;
        }

        try
        {
            await File.WriteAllTextAsync(path, SceneSerializer.Save(Document));
            Document.FilePath = path;
            Document.IsDirty = false;
            UpdateStatus();
            StatusText.Text = $"已保存 {Path.GetFileName(path)}";

            if (File.Exists(AutoSavePath))
            {
                File.Delete(AutoSavePath);
            }
        }
        catch (IOException ex)
        {
            StatusText.Text = "保存失败：" + ex.Message;
        }
    }

    private async void OnExportFbx(object sender, RoutedEventArgs e)
    {
        var picker = new Windows.Storage.Pickers.FileSavePicker();
        WinRT.Interop.InitializeWithWindow.Initialize(
            picker, WinRT.Interop.WindowNative.GetWindowHandle(this));
        picker.FileTypeChoices.Add("FBX", new List<string> { ".fbx" });
        picker.SuggestedFileName = "星梦场景";

        var file = await picker.PickSaveFileAsync();
        if (file is null)
        {
            return;
        }

        try
        {
            var ok = await Task.Run(() => FbxExporter.Export(Document, file.Path));
            StatusText.Text = ok ? $"已导出 {file.Path}" : "导出失败";
        }
        catch (Exception ex)
        {
            StatusText.Text = "导出失败：" + ex.Message;
        }
    }
}

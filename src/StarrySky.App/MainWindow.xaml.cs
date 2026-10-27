using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using StarrySky.Core.Export;
using StarrySky.Core.Math;
using StarrySky.Core.Scene;

namespace StarrySky.App;

public sealed partial class MainWindow : Window
{
    private bool _syncing;

    public MainWindow()
    {
        InitializeComponent();
        SystemBackdrop = new DesktopAcrylicBackdrop();
        LoadDemoScene();
        ViewportHost.Document.SelectionChanged += OnSelectionChanged;
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
        var mode = Selected is null ? "无选中" : $"已选中 {Selected.Name}";
        StatusText.Text = $"{mode} · {Document.Objects.Count} 个对象 · {Document.TriangleCount} 三角形";
        Title = Document.IsDirty ? "星空 — 未保存*" : "星空";
    }

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

        Document.SetVisible(Selected, VisibleCheck.IsChecked == true);
        UpdateStatus();
    }

    private void OnAddCube(object sender, RoutedEventArgs e) => AddPrimitive(PrimitiveParams.Cube());

    private void OnAddSphere(object sender, RoutedEventArgs e) => AddPrimitive(PrimitiveParams.Sphere());

    private void OnAddCylinder(object sender, RoutedEventArgs e) => AddPrimitive(PrimitiveParams.Cylinder());

    private void OnAddPlane(object sender, RoutedEventArgs e) => AddPrimitive(PrimitiveParams.Plane());

    private void AddPrimitive(PrimitiveParams parameters)
    {
        Document.AddPrimitive(parameters);
        UpdateStatus();
    }

    private void OnDuplicate(object sender, RoutedEventArgs e)
    {
        if (Selected is not null)
        {
            Document.Duplicate(Selected);
            UpdateStatus();
        }
    }

    private void OnDelete(object sender, RoutedEventArgs e)
    {
        if (Selected is not null)
        {
            Document.Remove(Selected);
            UpdateStatus();
        }
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

    private void OnOpenScene(object sender, RoutedEventArgs e)
    {
        StatusText.Text = "打开功能在下一阶段接入";
    }

    private void OnSaveScene(object sender, RoutedEventArgs e)
    {
        StatusText.Text = "保存功能在下一阶段接入";
    }

    private async void OnExportFbx(object sender, RoutedEventArgs e)
    {
        var picker = new Windows.Storage.Pickers.FileSavePicker();
        WinRT.Interop.InitializeWithWindow.Initialize(
            picker, WinRT.Interop.WindowNative.GetWindowHandle(this));
        picker.FileTypeChoices.Add("FBX", new List<string> { ".fbx" });
        picker.SuggestedFileName = "星空场景";

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

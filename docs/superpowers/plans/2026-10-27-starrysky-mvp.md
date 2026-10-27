# 星空（StarDream）MVP 实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 交付一个可运行的 Windows 桌面 3D 建模工具：能加基本体、进编辑模式改点线面、挤出成型、保存重开、导出 UE 可用的 FBX。

**Architecture:** 三个 .NET 项目加一个原生 FBX DLL。`StarDream.Core` 放纯逻辑（数学、网格、场景、拾取、编辑、导出），不依赖 UI，全部可单测；`StarDream.Render` 用 Vortice 把 Core 数据画到 D3D11 交换链；`StarDream.App` 是 WinUI 3 外壳，负责布局、输入和绑定。FBX 由 ufbx-write（MIT）编成原生 DLL，通过 P/Invoke 调用。

**Tech Stack:** C# / .NET 8、WinUI 3（Windows App SDK）、Vortice.Direct3D11 / DXGI / D3DCompiler / WinUI、CommunityToolkit.WinUI.Controls.Sizers、xUnit、ufbx-write（C99）。

**Spec:** `D:\111\docs\superpowers\specs\2026-10-27-stardream-mvp-design.md`

## Global Constraints

- 操作系统 Windows 11；.NET SDK 10.0.402；Windows SDK 10.0.26100；VS 18.11 Community 提供 MSVC。
- 目标框架 `net8.0-windows10.0.26100.0`。若 Windows App SDK 最新稳定版要求更高，才改 `net10.0-windows10.0.26100.0`。
- WinUI 3，`WindowsPackageType=None`，`WindowsAppSDKSelfContained=true`。
- 所有 NuGet 依赖锁定精确版本（不写 `*`、不写浮动范围）。
- 坐标系：右手系、Y 轴向上、1 单位 = 1 米。
- 欧拉角：XYZ 外旋，`R = Rz * Ry * Rx`，单位为度，范围 ±180。
- gizmo：移动与旋转作用**世界轴**；缩放作用**物体局部轴**。
- 法线：逐角法线 + 30° 自动平滑。
- 界面：深色 + `DesktopAcrylicBackdrop`；视口不透明。
- 快捷键遵循 Maya：Alt+左腾挪 / Alt+中平移 / Alt+右推拉；Q/W/E/R 工具；F8 对象↔编辑；F9/F10/F11 点/线/面；Ctrl+E 挤出；4/5 线框/着色；F 聚焦；A 全览。
- 场景文件扩展名 `.stardream`，JSON，`version: 1`。
- FBX：二进制、version 7400、Y-up（+X 右 / +Y 上 / −Z 前）、`unit_scale_factor = 100`、三角化、UV0 盒式投影、逐角法线、每对象一个带 T/R/S 的节点。
- 无撤销；有 30 秒自动保存草稿。
- 许可：只用 MIT/公有领域/BSD 依赖；不复制 Blender（GPL）源码。

## Review Focus

这些是规格暗示但没有任何验收项覆盖、最容易让用户踩到的地方。每条在括号里指定的任务中加测试。

1. **空场景 / 没有选中对象时按任何快捷键**（W/E/R/F/Delete/Ctrl+E/Ctrl+D）：应该什么都不发生，不抛异常，不崩溃。—— 测试落在 Task 18、Task 27。
2. **退化数值**：缩放设为 0 或负数、位置/旋转出现 NaN、挤出距离为 0、旋转恰好 ±180°：不能产生 NaN 顶点，不能崩。—— 测试落在 Task 5、Task 18、Task 27。
3. **打开损坏或版本不符的 `.stardream`**：应抛可捕获的错误并**保持当前文档不变**，而不是崩溃或清空场景。—— 测试落在 Task 19。
4. **视口尺寸退化为 0**（最小化、拖到极窄、DPI 切换）：交换链 resize 不能崩，恢复后画面正常。—— 测试落在 Task 3。
5. **网格被删到 0 个面之后**：退出编辑模式、保存、导出都应正常工作，不崩溃。—— 测试落在 Task 26、Task 28。

---

### Task 1: 解决方案骨架与依赖锁定

**Files:**
- Create: `.gitignore`, `Directory.Build.props`, `StarDream.sln`
- Create: `src/StarDream.Core/StarDream.Core.csproj`
- Create: `src/StarDream.Render/StarDream.Render.csproj`
- Create: `src/StarDream.App/StarDream.App.csproj`
- Create: `tests/StarDream.Core.Tests/StarDream.Core.Tests.csproj`

**Interfaces:**
- Consumes: 无
- Produces: 三个可编译的空项目 + 测试项目；后续所有任务都在这个骨架上加文件。

- [ ] **Step 1: 初始化仓库**

```powershell
cd D:\111
git init
```

`.gitignore` 写入：`bin/`, `obj/`, `.vs/`, `native/**/x64/`, `native/**/*.dll`, `*.user`, `TestResults/`。

- [ ] **Step 2: 建项目**

```powershell
dotnet new sln -n StarDream
dotnet new classlib -n StarDream.Core -o src/StarDream.Core -f net8.0
dotnet new classlib -n StarDream.Render -o src/StarDream.Render -f net8.0
dotnet new xunit -n StarDream.Core.Tests -o tests/StarDream.Core.Tests -f net8.0
dotnet sln add src/StarDream.Core src/StarDream.Render tests/StarDream.Core.Tests
```

`StarDream.App` 手写（模板依赖 VS）：`net8.0-windows10.0.26100.0`、`UseWinUI=true`、`WindowsPackageType=None`、`WindowsAppSDKSelfContained=true`、`OutputType=WinExe`。

- [ ] **Step 3: 加引用与精确版本**

Core 无外部依赖。Render 引用 Core + `Vortice.Direct3D11`、`Vortice.WinUI`、`Vortice.D3DCompiler`（写死版本号，如 `3.8.3`）。App 引用 Core、Render、`Microsoft.WindowsAppSDK`、`CommunityToolkit.WinUI.Controls.Sizers`。测试项目引用 Core。

- [ ] **Step 4: 验证构建**

Run: `dotnet build StarDream.sln`
Expected: `Build succeeded`，0 error。

Run: `dotnet test tests/StarDream.Core.Tests/StarDream.Core.Tests.csproj`
Expected: 1 个占位测试通过。

- [ ] **Step 5: 提交**

```bash
git add -A
git commit -m "chore: solution skeleton with pinned dependencies"
```

---

### Task 2: 深色亚克力窗口

**Files:**
- Create: `src/StarDream.App/App.xaml`, `src/StarDream.App/App.xaml.cs`
- Create: `src/StarDream.App/MainWindow.xaml`, `src/StarDream.App/MainWindow.xaml.cs`

**Interfaces:**
- Consumes: Task 1 的项目骨架
- Produces: `MainWindow`，后续任务往里加视口和面板；`App.RequestedTheme = ElementTheme.Dark`。

- [ ] **Step 1: 写 App.xaml**

`Application` 资源里放深色主题；`App.xaml.cs` 构造 `MainWindow` 并 `Activate()`。

- [ ] **Step 2: 写 MainWindow**

`MainWindow.xaml`：`Grid` 三行（菜单行 / 内容 / 状态栏），内容行三列（工具条 48px / 视口 * / 右侧面板 280px）。窗口 `SystemBackdrop = new DesktopAcrylicBackdrop()`，标题 `星空`。

- [ ] **Step 3: 运行验证**

Run: `dotnet run --project src/StarDream.App`
Expected: 窗口出现，标题"星空"，背景为深色亚克力（能透出桌面模糊），无异常退出。

- [ ] **Step 4: 提交**

```bash
git add src/StarDream.App
git commit -m "feat: dark acrylic main window"
```

---

### Task 3: D3D11 设备与空视口清屏

**Files:**
- Create: `src/StarDream.Render/Device.cs`
- Create: `src/StarDream.Render/ViewportRenderer.cs`
- Create: `src/StarDream.App/ViewportControl.xaml`, `ViewportControl.xaml.cs`
- Modify: `src/StarDream.App/MainWindow.xaml`

**Interfaces:**
- Consumes: Task 2 的窗口布局
- Produces: `ViewportRenderer`，后续所有渲染任务往里加：
  - `ViewportRenderer(ID3D11Device device)`
  - `void Resize(int pixelWidth, int pixelHeight)`
  - `void SetSwapChainForPanel(SwapChainPanel panel)` — 内部用 `ISwapChainPanelNative.SetSwapChain`
  - `void Render(RenderState state)`
  - `void Dispose()`

- [ ] **Step 1: 写 Device.cs**

创建 `ID3D11Device` / `ID3D11DeviceContext`（`DriverType.Hardware`，必要时 fallback WARP）、`IDXGIDevice`、`IDXGISwapChain`（`B8G8R8A8_UNorm`，双缓冲，`SwapEffect.FlipSequential`）、深度缓冲 `D24_UNorm_S8_UInt`。

**尺寸守卫**：`Resize(0,0)` 或无变化时直接返回，不解绑也不重建——这是 Review Focus 第 4 条。

- [ ] **Step 2: 写 ViewportRenderer.Clear**

`Render` 先 `ClearRenderTargetView`（颜色 `#1E1F22`）和 `ClearDepthStencilView`，再 `Present(1, None)`。

- [ ] **Step 3: 写 ViewportControl**

`SwapChainPanel` + `Loaded` 时把交换链挂上去；`SizeChanged` 时用 `CompositionScaleX/Y` 计算像素尺寸并 `Resize`；用 `CompositionTarget.Rendering` 驱动每帧 `Render`。

- [ ] **Step 4: 运行验证**

Run: `dotnet run --project src/StarDream.App`
Expected: 视口区域是 `#1E1F22` 深灰，缩放窗口不崩、不模糊，拖动窗口大小后画面填满。

- [ ] **Step 5: 提交**

```bash
git add -A
git commit -m "feat: d3d11 device and swapchain panel viewport"
```

---

### Task 4: 发布冒烟（M1.5）

**Files:**
- Create: `scripts/publish.ps1`
- Modify: `src/StarDream.App/StarDream.App.csproj`

**Interfaces:**
- Consumes: Task 3
- Produces: `win-x64` 自包含产物目录。

- [ ] **Step 1: 写 publish.ps1**

`dotnet publish src/StarDream.App -c Release -r win-x64 --self-contained true -o publish/`。

- [ ] **Step 2: 发布并搬移验证**

Run: `pwsh scripts/publish.ps1`
Expected: `publish/StarDream.App.exe` 存在。

把整个 `publish/` 复制到 `C:\Users\Star\Desktop\starrysmoke\`，双击 exe。
Expected: 窗口正常打开。

- [ ] **Step 3: 依赖检查**

Run: `dumpbin /dependents publish\StarDream.App.exe`（用 VS 的 dumpbin）
Expected: 只依赖系统 DLL 和随包 DLL，不出现 `VCRUNTIME140.dll` 之外的 VS 私有依赖缺失。

- [ ] **Step 4: 记录结论**

在本任务下方写一行：**无干净机器，仅做搬移 + 依赖检查，未在无 VS/SDK 环境实测。**

- [ ] **Step 5: 提交**

```bash
git add scripts src/StarDream.App/StarDream.App.csproj
git commit -m "build: self-contained publish script and smoke check"
```

---

### Task 5: 核心数学

**Files:**
- Create: `src/StarDream.Core/Math/Vec3.cs`, `Mat4.cs`, `Ray.cs`, `MathUtil.cs`
- Create: `tests/StarDream.Core.Tests/MathTests.cs`

**Interfaces:**
- Produces:
  - `Vec3(float x, float y, float z)`，`+ - *`、`Dot`、`Cross`、`Length`、`Normalized`、`Lerp`
  - `Mat4` 行主序，`*`、`TransformPoint`、`TransformDirection`、`TryInvert`
  - `Mat4.Translation(Vec3)`、`RotationX/Y/Z(float radians)`、`Scale(Vec3)`、`PerspectiveFov(float fovY, float aspect, float near, float far)`、`LookAt(Vec3 eye, Vec3 target, Vec3 up)`、`FromTrs(Vec3 translation, Vec3 rotationDegrees, Vec3 scale)`
  - `Ray(Vec3 origin, Vec3 direction)`
  - `MathUtil.DegToRad`、`Vec3DegToRad(Vec3)`

- [ ] **Step 1: 写失败测试**

```csharp
[Fact] public void FromTrs_Applies_Scale_Then_RotateZ_Then_Translate()
// 输入 t=(10,0,0), r=(0,0,90), s=(2,1,1)，点 (1,0,0)
// 期望 (10,2,0)：先缩放成 (2,0,0)，再绕 Z 转 90° 成 (0,2,0)，再平移

[Fact] public void FromTrs_Rejects_NaN_At_Entry()
// 传入 float.NaN 的 translation，期望抛 ArgumentException（Review Focus 第 2 条）

[Fact] public void LookAt_Looks_Down_NegativeZ_From_Origin()

[Fact] public void PerspectiveFov_Maps_Near_Plane_To_MinusOne()
```

- [ ] **Step 2: 运行确认失败**

Run: `dotnet test --filter FullyQualifiedName~MathTests`
Expected: 编译失败（类型不存在）。

- [ ] **Step 3: 实现**

按上面签名实现。`FromTrs` 组合顺序 `T * Rz * Ry * Rx * S`。`TryInvert` 用余子式法。

- [ ] **Step 4: 运行确认通过**

Run: `dotnet test --filter FullyQualifiedName~MathTests`
Expected: 4 passed。

- [ ] **Step 5: 提交**

```bash
git add src/StarDream.Core/Math tests/StarDream.Core.Tests/MathTests.cs
git commit -m "feat: core vector and matrix math"
```

---

### Task 6: 环绕相机

**Files:**
- Create: `src/StarDream.Core/Cameras/OrbitCamera.cs`
- Create: `tests/StarDream.Core.Tests/OrbitCameraTests.cs`

**Interfaces:**
- Produces:
  - `enum CameraNav { Tumble, Track, Dolly }`
  - `OrbitCamera`：`Vec3 Target`、`float Distance`、`float Yaw`、`float Pitch`、`float FovYDegrees`（默认 45）
  - `void Orbit(float deltaYawRad, float deltaPitchRad)`
  - `void Pan(float dxPixels, float dyPixels, int viewportHeight)`
  - `void Dolly(float factor)`
  - `void Frame(Vec3 center, float radius)`
  - `Mat4 GetView()`、`Mat4 GetProjection(float aspect)`、`Ray ScreenToRay(Vec2 ndc)`

- [ ] **Step 1: 写失败测试**

```csharp
[Fact] public void Orbit_Clamps_Pitch_To_89_Degrees()
[Fact] public void Dolly_Clamps_Distance_Between_0_5_And_200()
[Fact] public void Frame_Centers_And_Fits_Bounding_Sphere()
```

- [ ] **Step 2: 运行确认失败**

Run: `dotnet test --filter FullyQualifiedName~OrbitCameraTests`
Expected: FAIL，类型不存在。

- [ ] **Step 3: 实现**

球坐标：`eye = target + distance * (cosPitch*sinYaw, sinPitch, cosPitch*cosYaw)`。`Pitch` 夹在 ±89°。`Distance` 夹到 [0.5, 200]。

- [ ] **Step 4: 运行确认通过并提交**

```bash
dotnet test --filter FullyQualifiedName~OrbitCameraTests
git add src/StarDream.Core/Cameras tests/StarDream.Core.Tests/OrbitCameraTests.cs
git commit -m "feat: orbit camera with maya-style navigation"
```

---

### Task 7: 地面网格与世界坐标轴

**Files:**
- Create: `src/StarDream.Render/LineBatch.cs`
- Create: `src/StarDream.Render/SceneRenderer.cs`
- Create: `src/StarDream.Render/Shaders/line.hlsl`
- Modify: `src/StarDream.Render/ViewportRenderer.cs`

**Interfaces:**
- Consumes: Task 6 的 `OrbitCamera`
- Produces: `SceneRenderer.DrawGrid(Mat4 viewProj)`，后续实体绘制也加到这里。

- [ ] **Step 1: 实现 LineBatch**

顶点格式 `(float3 pos, float4 color)`，`D3D11_PRIMITIVE_TOPOLOGY_LINELIST`，一次 Draw 画完整个批次。

- [ ] **Step 2: 生成网格与坐标轴**

网格：XZ 平面上 ±10 m，间距 1 m；中心两条线更亮。坐标轴：X 红 / Y 绿 / Z 蓝，长 2 m。

- [ ] **Step 3: 运行验证**

Run: `dotnet run --project src/StarDream.App`
Expected: 视口里能看到地面网格和三条彩色轴，能旋转/平移/缩放（本任务先接固定相机，导航在 Task 17 接线后生效）。

- [ ] **Step 4: 提交**

```bash
git add src/StarDream.Render
git commit -m "feat: ground grid and world axes"
```

---

### Task 8: 网格数据结构与拓扑

**Files:**
- Create: `src/StarDream.Core/Meshes/Edge.cs`, `Mesh.cs`
- Create: `tests/StarDream.Core.Tests/MeshTests.cs`

**Interfaces:**
- Produces:
  - `readonly struct Edge(int a, int b)`，构造时规范化 `A < B`，实现 `IEquatable<Edge>`
  - `Mesh`：`List<Vec3> Positions`、`List<int[]> Faces`、`List<Edge> Edges`、`List<int[]> Triangles`、`Vec3[] FaceNormals`、`Vec3[] CornerNormals`
  - `int CornerOffset(int faceIndex)` — 该面第一个角在 `CornerNormals` 里的下标
  - `void RebuildDerived()` — 重算 Edges / Triangles / FaceNormals
  - `Mesh Clone()`

- [ ] **Step 1: 写失败测试**

```csharp
[Fact] public void Rebuild_Dedupes_Shared_Edges()
// 两个共享一条边的三角形 → Edges.Count == 5

[Fact] public void Triangulation_Fan_Splits_Quad_Into_Two_Triangles()

[Fact] public void CornerOffset_Sums_Preceding_Face_Vertex_Counts()

[Fact] public void Rebuild_On_Empty_Mesh_Does_Not_Throw()   // Review Focus 第 5 条
```

- [ ] **Step 2: 运行确认失败，然后实现**

Edges 用 `HashSet<Edge>` 累积后排序输出。Triangles 扇形：面 `[a,b,c,d]` → `(a,b,c)`、`(a,c,d)`。空位置或越界索引时 `RebuildDerived` 抛 `InvalidOperationException`。

- [ ] **Step 3: 运行确认通过并提交**

```bash
dotnet test --filter FullyQualifiedName~MeshTests
git add src/StarDream.Core/Meshes tests/StarDream.Core.Tests/MeshTests.cs
git commit -m "feat: mesh data structure and topology derivation"
```

---

### Task 9: 基本体生成

**Files:**
- Create: `src/StarDream.Core/Meshes/Primitives.cs`
- Create: `src/StarDream.Core/Scene/PrimitiveParams.cs`
- Create: `tests/StarDream.Core.Tests/PrimitivesTests.cs`

**Interfaces:**
- Produces:
  - `PrimitiveParams`：`string Type`（`"cube"|"sphere"|"cylinder"|"plane"`）、`float Size/Radius/Height/Width/Depth`、`int Segments/Rings`，静态工厂 `Cube()/Sphere()/Cylinder()/Plane()` 给默认值
  - `Mesh Primitives.Build(PrimitiveParams p)`
  - `float Primitives.BaseOffsetY(PrimitiveParams p)` — 让底面贴地需要抬高多少

- [ ] **Step 1: 写失败测试**

```csharp
[Fact] public void Cube_Has_8_Vertices_6_Quads_12_Edges()
[Fact] public void Sphere_With_16x8_Has_Expected_Face_Count()
[Fact] public void Cylinder_Closes_Both_Caps()
[Fact] public void Plane_Is_One_Quad()
[Fact] public void Build_With_Zero_Segments_Clamps_To_Minimum()   // 退化输入不崩
```

- [ ] **Step 2: 运行确认失败，然后实现**

所有面逆时针。`Segments` 至少 3，`Rings` 至少 2。

- [ ] **Step 3: 运行确认通过并提交**

```bash
dotnet test --filter FullyQualifiedName~PrimitivesTests
git add src/StarDream.Core/Meshes src/StarDream.Core/Scene tests/StarDream.Core.Tests/PrimitivesTests.cs
git commit -m "feat: primitive mesh generators"
```

---

### Task 10: 逐角法线与 30° 平滑

**Files:**
- Create: `src/StarDream.Core/Meshes/NormalSmoother.cs`
- Modify: `src/StarDream.Core/Meshes/Mesh.cs`
- Create: `tests/StarDream.Core.Tests/NormalSmootherTests.cs`

**Interfaces:**
- Produces: `NormalSmoother.Compute(Mesh mesh)`，写满 `mesh.CornerNormals`；常量 `NormalSmoother.SmoothAngleDegrees = 30f`。

- [ ] **Step 1: 写失败测试**

```csharp
[Fact] public void Cube_All_Corners_Of_A_Face_Use_That_Face_Normal()
// 立方体每个面的 4 个角法线都等于该面法线（硬边）

[Fact] public void Sphere_Corner_Normals_Are_Radial()
// 球面每个角法线与该点位置归一化后点积 > 0.95

[Fact] public void Faces_Sharing_Edge_Under_30_Degrees_Share_Normal()
// 两个夹角 20° 的相邻面，共享顶点处两侧角法线相同

[Fact] public void Faces_Over_30_Degrees_Do_Not_Share()
// 夹角 45° 的两面，共享顶点处角法线不同
```

- [ ] **Step 2: 运行确认失败，然后实现**

对每个顶点收集相邻面；按面法线之间的夹角聚成组（并查集或贪心分组，阈值 30°）；组内法线 = 面积加权平均；角法线 = 该角所在面所属组的法线。零长度法线回退到面法线，再回退到 `(0,1,0)`。

- [ ] **Step 3: 运行确认通过并提交**

```bash
dotnet test --filter FullyQualifiedName~NormalSmootherTests
git add src/StarDream.Core/Meshes tests/StarDream.Core.Tests/NormalSmootherTests.cs
git commit -m "feat: per-corner normals with 30 degree auto smoothing"
```

---

### Task 11: 实体渲染与光照

**Files:**
- Create: `src/StarDream.Render/MeshBuffers.cs`
- Create: `src/StarDream.Render/Shaders/solid.hlsl`
- Modify: `src/StarDream.Render/SceneRenderer.cs`

**Interfaces:**
- Consumes: Task 8/10 的 `Mesh`、Task 6 的相机
- Produces: `SceneRenderer.DrawMeshes(IReadOnlyList<(Mat4 model, Mesh mesh, Vec4 color)> meshes, Mat4 viewProj, Vec3 cameraPos)`

- [ ] **Step 1: 顶点布局与上传**

按「(顶点, 平滑组)」拆分后的顶点：位置、法线、颜色。用 `CornerNormals` 判断同顶点不同组的角要不要拆开。按网格引用做缓存，拓扑版本号变了才重建缓冲。

- [ ] **Step 2: 写着色器**

Lambert + 半球环境光：`ambient = lerp(groundColor, skyColor, n.y*0.5+0.5)`，主光方向 `normalize(0.4, -1, -0.3)`。

- [ ] **Step 3: 运行验证**

临时在 `ViewportRenderer.Render` 里硬编码一个立方体和一个球和一个圆柱一个平面。
Run: `dotnet run --project src/StarDream.App`
Expected: 四个基本体可见；立方体每个面颜色均匀（硬边）、球面光滑过渡。

- [ ] **Step 4: 提交**

```bash
git add src/StarDream.Render
git commit -m "feat: solid mesh rendering with lambert shading"
```

---

### Task 12: 场景对象与参数化 Properties

**Files:**
- Create: `src/StarDream.Core/Scene/SceneObject.cs`, `SceneDocument.cs`
- Create: `src/StarDream.App/Panels/PropertiesPanel.xaml`, `PropertiesPanel.xaml.cs`
- Create: `src/StarDream.App/ViewModels/SceneObjectViewModel.cs`
- Create: `tests/StarDream.Core.Tests/SceneObjectTests.cs`

**Interfaces:**
- Produces:
  - `SceneObject`：`Guid Id`、`string Name`、`Vec3 Position/Rotation/Scale`、`string ColorHex`、`bool Visible`、`PrimitiveParams? Primitive`、`Mesh? Baked`
  - `Mesh GetMesh()` — 有 Baked 返回它，否则按 Primitive 生成并缓存
  - `void Bake()` — 把当前生成的网格存进 Baked 并清空 Primitive
  - `Mat4 LocalMatrix()` — `FromTrs(Position, Rotation, Scale)`
  - `SceneDocument`：`List<SceneObject> Objects`、`OrbitCamera Camera`、`bool IsDirty`、`string? FilePath`
  - `void AddPrimitive(PrimitiveParams p)` — 生成对象、底面贴地、加入列表、标记脏
  - `SceneObject Duplicate(SceneObject o)` — 新 `Id`、名字加 `_copy` 后缀、`Position.X += 0.5`
  - `void Remove(SceneObject o)` — 移除并清空选中
  - `void SetVisible(SceneObject o, bool visible)`

- [ ] **Step 1: 写失败测试**

```csharp
[Fact] public void AddPrimitive_Places_Bottom_On_Ground()
// 加 size=1 的立方体 → Position.Y == 0.5

[Fact] public void Bake_Freezes_Params_And_Mesh()
// Bake 后 Primitive 为 null，Baked 非空，改参数不可行

[Fact] public void Changing_Primitive_Params_Rebuilds_Mesh()
// 改 Segments 后 GetMesh() 的面数与新参数一致

[Fact] public void Duplicate_Offsets_X_By_Half_And_Gets_New_Id()
[Fact] public void Remove_Drops_Object_From_Document()
[Fact] public void Duplicate_Of_Baked_Mesh_Keeps_Baked_Geometry()
```

- [ ] **Step 2: 运行确认失败，然后实现**

- [ ] **Step 3: 写 PropertiesPanel**

绑定 `SelectedObject`；名称、位置/旋转/缩放三行 `NumberBox`、颜色、可见开关、以及按 `Primitive.Type` 变化的参数区（已烘焙时参数区只读并显示"已编辑"）。

- [ ] **Step 4: 运行验证并提交**

Run: `dotnet run --project src/StarDream.App`
Expected: 改分段数，视口里球的面数实时变化。

```bash
git add -A
git commit -m "feat: scene objects and parametric properties panel"
```

---

### Task 13: FBX 原生 DLL 与最小导出

**Files:**
- Create: `native/README.md`（记录 ufbx-write 的 commit hash）
- Create: `native/StarDream.FbxNative/ufbx_write.c`, `ufbx_write.h`（从锁定 commit 复制）
- Create: `native/build.ps1`
- Create: `src/StarDream.Core/Export/FbxNative.cs`, `FbxExporter.cs`
- Create: `tests/StarDream.Core.Tests/FbxExporterTests.cs`

**Interfaces:**
- Produces:
  - `FbxNative`：`DllImport` 声明 `ufbxw_create_scene`、`ufbxw_free_scene`、`ufbxw_scene_set_coordinate_axes`、`ufbxw_scene_set_unit_scale_factor`、`ufbxw_create_node`、`ufbxw_set_name`、`ufbxw_create_mesh`、`ufbxw_mesh_add_instance`、`ufbxw_view_vec3_array`、`ufbxw_view_int_array`、`ufbxw_mesh_set_vertices`、`ufbxw_mesh_set_triangles`、`ufbxw_prepare_scene`、`ufbxw_save_file`
  - `FbxExporter.Export(SceneDocument doc, string path)` → `bool`

- [ ] **Step 1: 编 DLL**

`native/build.ps1`：用 vswhere 找 VS，调 `vcvars64.bat`，`cl /LD /O2 /DNDEBUG ufbx_write.c /Fe:StarDream.FbxNative.dll`，输出到 `native/bin/x64/`，再复制到 App 输出目录。

- [ ] **Step 2: 写失败测试**

```csharp
[Fact] public void Export_Writes_A_Binary_FBX_File()
// 临时目录导出空场景 → 文件存在、长度 > 1KB、开头是 "Kaydara FBX Binary"

[Fact] public void Export_Single_Cube_Does_Not_Throw()
```

- [ ] **Step 3: 运行确认失败，然后实现 P/Invoke 与最小导出**

场景坐标轴设为 `right=+X, up=+Y, front=-Z`，`unit_scale_factor = 100`；`save_opts.format = BINARY`、`version = 7400`。

- [ ] **Step 4: 运行确认通过并提交**

```bash
dotnet test --filter FullyQualifiedName~FbxExporterTests
git add -A
git commit -m "feat: ufbx-write native bridge and minimal fbx export"
```

---

### Task 14: UE / Blender 无头校验脚本

**Files:**
- Create: `tests/interop/validate_blender.py`
- Create: `tests/interop/validate_ue.py`
- Create: `scripts/validate-fbx.ps1`

**Interfaces:**
- Consumes: Task 13 导出的 `.fbx`
- Produces: 两个脚本，输入 FBX 路径，输出 JSON `{ meshes, triangles, bboxMin, bboxMax }`。

- [ ] **Step 1: 写 Blender 脚本**

`E:\3D\Blender\blender.exe -b --python tests/interop/validate_blender.py -- <fbx>`
脚本里 `bpy.ops.import_scene.fbx(filepath=...)`，统计网格数和三角形数，打印 JSON 到 stdout。

- [ ] **Step 2: 写 UE 脚本**

`tests/interop/validate_ue.py` 用 `unreal.AssetImportTask` 导入到 `/Game/Validation`，读静态网格的三角形数与包围盒，打印 JSON。
运行：`E:\UE_5.8\Engine\Binaries\Win64\UnrealEditor-Cmd.exe <一个空工程> -run=pythonscript -script="tests/interop/validate_ue.py" -unattended -nop4`

- [ ] **Step 3: 写校验入口**

`scripts/validate-fbx.ps1 -Fbx <path>`，两个都跑，比对三角形数与包围盒（误差 < 0.1 cm），不一致时 `exit 1`。

- [ ] **Step 4: 运行验证**

Run: `pwsh scripts/validate-fbx.ps1 -Fbx <Task 13 导出的立方体>`
Expected: 两边三角形数都是 12，包围盒一致。

- [ ] **Step 5: 提交**

```bash
git add tests/interop scripts/validate-fbx.ps1
git commit -m "test: headless fbx validation for unreal and blender"
```

---

### Task 15: 对象拾取

**Files:**
- Create: `src/StarDream.Core/Picking/RayPicker.cs`
- Create: `tests/StarDream.Core.Tests/RayPickerTests.cs`

**Interfaces:**
- Produces:
  - `bool RayPicker.RayTriangle(Ray ray, Vec3 a, Vec3 b, Vec3 c, out float t)`
  - `SceneObject? RayPicker.PickObject(SceneDocument doc, Ray worldRay)`

- [ ] **Step 1: 写失败测试**

```csharp
[Fact] public void Ray_Through_Cube_Center_Hits_Front_Face()
[Fact] public void Ray_Missing_Cube_Returns_Null()
[Fact] public void Pick_Respects_Object_Transform()
// 立方体移到 (5,0,0)，射线打原点不命中，打 (5,0,1) 命中
[Fact] public void Pick_Skips_Invisible_Objects()
```

- [ ] **Step 2: 运行确认失败，然后实现**

Möller–Trumbore。先给射线乘对象 `TryInvert(LocalMatrix())` 转到局部空间，再和 `Triangles` 求交。

- [ ] **Step 3: 运行确认通过并提交**

```bash
dotnet test --filter FullyQualifiedName~RayPickerTests
git add src/StarDream.Core/Picking tests/StarDream.Core.Tests/RayPickerTests.cs
git commit -m "feat: object ray picking"
```

---

### Task 16: 选择、描边与 Outliner 同步

**Files:**
- Create: `src/StarDream.App/ViewModels/MainViewModel.cs`
- Create: `src/StarDream.App/Panels/OutlinerPanel.xaml`, `OutlinerPanel.xaml.cs`
- Create: `src/StarDream.Render/OutlineRenderer.cs`
- Modify: `src/StarDream.App/ViewportControl.xaml.cs`

**Interfaces:**
- Consumes: Task 15 的拾取
- Produces:
  - `MainViewModel.SelectedObject`、`SelectedObjectChanged` 事件、`ObservableCollection<SceneObjectViewModel> Objects`
  - `OutlineRenderer.Draw(Mat4 model, Mesh mesh, Mat4 viewProj, Vec4 color)`

- [ ] **Step 1: 接线输入**

视口左键按下 → `ScreenToRay` → `PickObject` → 更新 `SelectedObject`。空白处取消选择。

左键拖动 → 对象级框选：把每个对象的包围盒 8 个角投影到屏幕，包围盒与矩形相交即选中。Maya 里拖动就是框选，不加修饰键。

同时接上 `Ctrl+D` 复制、`Delete` 删除、`Ctrl+A` 全选、`H` 显隐——没有选中对象时这些都是空操作。

- [ ] **Step 2: 描边**

背面外扩：`RasterizerState` 剔除正面 + 顶点位置沿法线外扩 0.02 m，纯色绘制。

- [ ] **Step 3: Outliner**

`ListView` 绑定 `Objects`，选中双向同步。F2 重命名。

- [ ] **Step 4: 运行验证并提交**

Expected: 点哪个选哪个；Outliner 高亮同步；选中物体带描边。

```bash
git add -A
git commit -m "feat: object selection, outline and outliner sync"
```

---

### Task 17: Gizmo 绘制与命中

**Files:**
- Create: `src/StarDream.Core/Editing/Gizmo.cs`（枚举与常量）
- Create: `src/StarDream.Core/Editing/GizmoHitTest.cs`
- Create: `src/StarDream.Render/GizmoRenderer.cs`
- Create: `tests/StarDream.Core.Tests/GizmoHitTestTests.cs`

**Interfaces:**
- Produces:
  - `enum GizmoMode { Translate, Rotate, Scale }`、`enum GizmoAxis { X, Y, Z, Center }`
  - `GizmoHitTest.Hit(Vec3 origin, Mat4 orientation, GizmoMode mode, Mat4 viewProj, Vec2 screen, float pixelWidth, float pixelHeight, float threshold) → (GizmoAxis axis, bool hit)`

- [ ] **Step 1: 写失败测试**

```csharp
[Fact] public void Hit_Detects_X_Axis_When_Mouse_Near_Projected_Arrow()
[Fact] public void Hit_Misses_When_Far_From_All_Handles()
[Fact] public void Rotate_Rings_Are_Hit_By_Pixel_Distance_Not_Depth()
```

- [ ] **Step 2: 实现命中**

把轴/环采样成折线 → 投影到屏幕 → 点到线段距离 < 8px 命中，取最近。

- [ ] **Step 3: 实现绘制**

三套 gizmo：移动（三箭头 + 中心方块）、旋转（三圆环）、缩放（三线 + 末端方块 + 中心方块）。关闭深度写入。

- [ ] **Step 4: 运行验证并提交**

Expected: 选中物体后能看到 gizmo，鼠标悬停轴会变色。

```bash
dotnet test --filter FullyQualifiedName~GizmoHitTestTests
git add -A
git commit -m "feat: gizmo rendering and screen-space hit testing"
```

---

### Task 18: Gizmo 拖动

**Files:**
- Create: `src/StarDream.Core/Editing/GizmoDrag.cs`
- Create: `tests/StarDream.Core.Tests/GizmoDragTests.cs`
- Modify: `src/StarDream.App/ViewportControl.xaml.cs`

**Interfaces:**
- Produces:
  - `GizmoDrag.Begin(SceneObject target, GizmoMode mode, GizmoAxis axis, Ray ray, Vec3 pivot)`
  - `void Update(Ray ray)` — 改写目标的 Position / Rotation / Scale
  - `void End()`
  - 缩放时用**局部轴**，移动/旋转用**世界轴**

- [ ] **Step 1: 写失败测试**

```csharp
[Fact] public void Translate_X_Moves_Only_X()
[Fact] public void Rotate_Z_By_90_Turns_LocalY_Into_WorldX()
[Fact] public void Scale_Along_Local_Axis_Does_Not_Produce_Shear()
// 物体先绕 Y 转 45°，沿局部 X 拉 2 倍；断言 LocalMatrix 的三列仍正交
[Fact] public void Scale_Is_Clamped_To_Minimum_0_01()   // Review Focus 第 2 条
[Fact] public void Drag_With_No_Target_Is_A_NoOp()      // Review Focus 第 1 条
```

- [ ] **Step 2: 运行确认失败，然后实现**

- [ ] **Step 3: 运行确认通过并提交**

```bash
dotnet test --filter FullyQualifiedName~GizmoDragTests
git add -A
git commit -m "feat: gizmo drag transforms with world and local axes"
```

---

### Task 19: 场景序列化

**Files:**
- Create: `src/StarDream.Core/Scene/SceneSerializer.cs`, `SceneLoadException.cs`
- Create: `tests/StarDream.Core.Tests/SceneSerializerTests.cs`

**Interfaces:**
- Produces:
  - `string SceneSerializer.Save(SceneDocument doc)`
  - `SceneDocument SceneSerializer.Load(string json)` — 版本不符或 JSON 损坏抛 `SceneLoadException`
  - 格式见规格 §8.9：`version`、`camera`、`objects[]`，每个对象 `primitive` 与 `mesh` 二选一

- [ ] **Step 1: 写失败测试**

```csharp
[Fact] public void RoundTrip_Preserves_Objects_Transforms_And_Camera()
[Fact] public void Baked_Mesh_RoundTrips_With_Positions_And_Faces()
[Fact] public void Unknown_Version_Throws_SceneLoadException()
[Fact] public void Malformed_Json_Throws_And_Leaves_Document_Untouched()
// Review Focus 第 3 条：先建好 doc，调用 Load 失败后断言 doc 内容没变
```

- [ ] **Step 2: 运行确认失败，然后实现**

`System.Text.Json` + 自定义 `Vec3` 转换器。

- [ ] **Step 3: 运行确认通过并提交**

```bash
dotnet test --filter FullyQualifiedName~SceneSerializerTests
git add -A
git commit -m "feat: scene json serialization with version guard"
```

---

### Task 20: 文件交互、脏状态与关窗拦截

**Files:**
- Create: `src/StarDream.App/Services/FileService.cs`
- Modify: `src/StarDream.App/MainWindow.xaml.cs`, `src/StarDream.App/ViewModels/MainViewModel.cs`

**Interfaces:**
- Consumes: Task 19 的序列化
- Produces:
  - `FileService.SaveAsync(Window owner, SceneDocument doc, bool forcePicker) → bool`
  - `FileService.OpenAsync(Window owner) → SceneDocument?`
  - `MainViewModel.IsDirty`、`FileName`

- [ ] **Step 1: 实现 picker**

`FileSavePicker` / `FileOpenPicker` + `InitializeWithWindow.Initialize(picker, hwnd)`。扩展名 `.stardream`。

- [ ] **Step 2: 脏规则**

只有对象增删、变换、颜色、显隐、拓扑、参数变化才 `IsDirty = true`。相机、选择、工具、模式都不算。标题 `星空 — 名字*`。

- [ ] **Step 3: 关窗拦截**

`AppWindow.Closing` + `args.Cancel = true` + 异步 `ContentDialog`（保存 / 不保存 / 取消）。保存被取消或写盘失败 → **不关窗**，文档保持脏，状态栏报错。

- [ ] **Step 4: 手动验证并提交**

Expected: 改点东西 → 标题出现 `*` → 关窗弹框 → 选"取消"窗口还在。

```bash
git add -A
git commit -m "feat: save open dirty tracking and close interception"
```

---

### Task 21: 自动保存与恢复

**Files:**
- Create: `src/StarDream.App/Services/AutoSaveService.cs`
- Modify: `src/StarDream.App/App.xaml.cs`

**Interfaces:**
- Produces:
  - `AutoSaveService.Start(SceneDocument doc)` / `Stop()`
  - 路径 `%LOCALAPPDATA%\StarDream\autosave.stardream`
  - `AutoSaveService.TryRecover() → SceneDocument?`

- [ ] **Step 1: 实现**

`DispatcherTimer` 30 秒；`IsDirty` 为真才写。正常保存成功后删除草稿。启动时草稿比最后保存新 → 弹"恢复上次内容？"。

- [ ] **Step 2: 手动验证并提交**

Expected: 开着程序删掉对象 → 强杀进程 → 重开弹恢复提示 → 恢复后对象在。

```bash
git add -A
git commit -m "feat: autosave draft and crash recovery"
```

---

### Task 22: 编辑模式的选择集与拓扑查询

**Files:**
- Create: `src/StarDream.Core/Editing/EditSelection.cs`, `MeshQueries.cs`
- Create: `tests/StarDream.Core.Tests/EditSelectionTests.cs`

**Interfaces:**
- Produces:
  - `enum ElementMode { Vertex, Edge, Face }`
  - `EditSelection`：`Mode`、`HashSet<int> Vertices`、`HashSet<Edge> Edges`、`HashSet<int> Faces`、`Count`、`Clear()`、`Toggle...`
  - `MeshQueries.FacesOfVertex(Mesh, int) → IReadOnlyList<int>`
  - `MeshQueries.FacesOfEdge(Mesh, Edge) → IReadOnlyList<int>`
  - `MeshQueries.VerticesOfFace(Mesh, int)`
  - `MeshQueries.BoundaryEdges(Mesh, IReadOnlySet<int> faces) → List<Edge>`
  - `MeshQueries.ConnectedComponents(Mesh, IReadOnlySet<int> faces) → List<HashSet<int>>`
  - `MeshQueries.Centroid(Mesh, EditSelection) → Vec3`

- [ ] **Step 1: 写失败测试**

```csharp
[Fact] public void FacesOfEdge_Returns_Both_Adjacent_Quads()
[Fact] public void BoundaryEdges_Of_Single_Quad_Is_Its_Four_Edges()
[Fact] public void BoundaryEdges_Of_Whole_Cube_Is_Empty()
[Fact] public void ConnectedComponents_Splits_Two_Opposite_Faces()
[Fact] public void Centroid_Of_Empty_Selection_Is_Zero_Without_Throwing()
```

- [ ] **Step 2: 运行确认失败，然后实现**

- [ ] **Step 3: 运行确认通过并提交**

```bash
dotnet test --filter FullyQualifiedName~EditSelectionTests
git add -A
git commit -m "feat: edit selection sets and mesh topology queries"
```

---

### Task 23: 编辑模式渲染

**Files:**
- Create: `src/StarDream.Render/EditModeRenderer.cs`
- Create: `src/StarDream.Render/Shaders/point.hlsl`, `Shaders/overlay.hlsl`
- Modify: `src/StarDream.Render/ViewportRenderer.cs`

**Interfaces:**
- Produces: `EditModeRenderer.Draw(Mesh mesh, Mat4 model, Mat4 viewProj, EditSelection selection, Vec3 cameraPos)`

- [ ] **Step 1: 半透明实体 + 边线 + 顶点**

实体用 0.35 alpha；边线用深度偏移；顶点用实例化小四边形（屏幕恒定 5px），朝向相机。

- [ ] **Step 2: 选中高亮**

面填充橙色半透明、边线橙色加粗、点橙色放大。

- [ ] **Step 3: 运行验证并提交**

Expected: 按 F8 进入编辑模式，能同时看到点/边/面和选中高亮。

```bash
git add src/StarDream.Render
git commit -m "feat: edit mode rendering"
```

---

### Task 24: 元素拾取与框选

**Files:**
- Create: `src/StarDream.Core/Picking/ElementPicker.cs`
- Create: `tests/StarDream.Core.Tests/ElementPickerTests.cs`

**Interfaces:**
- Produces:
  - `int? PickVertex(Mesh mesh, Mat4 model, Mat4 viewProj, Vec2 screenPx, int w, int h, float radiusPx = 8)`
  - `Edge? PickEdge(... 同上 ..., float radiusPx = 6)`
  - `int? PickFace(Mesh mesh, Mat4 model, Ray worldRay)`
  - `List<int> BoxSelectVertices(Mesh mesh, Mat4 model, Mat4 viewProj, Rect screenRect)`

- [ ] **Step 1: 写失败测试**

```csharp
[Fact] public void PickVertex_Finds_Nearest_Within_Radius()
[Fact] public void PickEdge_Uses_Screen_Distance_Not_World_Distance()
[Fact] public void PickFace_Returns_Front_Most_Face()
[Fact] public void BoxSelect_Returns_All_Vertices_Inside_Rect()
```

- [ ] **Step 2: 运行确认失败，然后实现**

- [ ] **Step 3: 运行确认通过并提交**

```bash
dotnet test --filter FullyQualifiedName~ElementPickerTests
git add -A
git commit -m "feat: element picking and box selection"
```

---

### Task 25: 元素变换

**Files:**
- Create: `src/StarDream.Core/Editing/ElementTransform.cs`
- Create: `tests/StarDream.Core.Tests/ElementTransformTests.cs`

**Interfaces:**
- Produces:
  - `void Translate(Mesh mesh, EditSelection sel, Vec3 delta)`
  - `void Rotate(Mesh mesh, EditSelection sel, Vec3 axis, float angleRad, Vec3 pivot)`
  - `void Scale(Mesh mesh, EditSelection sel, Vec3 factor, Vec3 pivot)`
  - 结束后调用 `mesh.RebuildDerived()` 和 `NormalSmoother.Compute(mesh)`

- [ ] **Step 1: 写失败测试**

```csharp
[Fact] public void Translate_Moves_Selected_Vertices_Only()
[Fact] public void Translate_On_Face_Selection_Moves_Its_Vertices()
[Fact] public void Rotate_Around_Pivot_Leaves_Pivot_Fixed()
[Fact] public void Scale_With_Zero_Factor_Clamps_Instead_Of_Collapsing()   // Review Focus 第 2 条
```

- [ ] **Step 2: 运行确认失败，然后实现**

- [ ] **Step 3: 运行确认通过并提交**

```bash
dotnet test --filter FullyQualifiedName~ElementTransformTests
git add -A
git commit -m "feat: element transform in edit mode"
```

---

### Task 26: 删除元素

**Files:**
- Modify: `src/StarDream.Core/Editing/MeshEditor.cs`（新建）
- Create: `tests/StarDream.Core.Tests/MeshEditorDeleteTests.cs`

**Interfaces:**
- Produces: `void MeshEditor.Delete(Mesh mesh, EditSelection sel)` — 面模式删面、点模式删引用该点的所有面、线模式删共享该边的面；之后清理孤立顶点并 `RebuildDerived()`。

- [ ] **Step 1: 写失败测试**

```csharp
[Fact] public void Delete_Face_Removes_Only_That_Face()
[Fact] public void Delete_Face_Removes_Orphaned_Vertices()
[Fact] public void Delete_Vertex_Removes_All_Faces_Using_It()
[Fact] public void Delete_All_Faces_Leaves_Empty_Mesh_That_Still_Rebuilds()
// Review Focus 第 5 条
[Fact] public void Delete_With_Empty_Selection_Is_A_NoOp()   // Review Focus 第 1 条
```

- [ ] **Step 2: 运行确认失败，然后实现**

- [ ] **Step 3: 运行确认通过并提交**

```bash
dotnet test --filter FullyQualifiedName~MeshEditorDeleteTests
git add -A
git commit -m "feat: element deletion with orphan cleanup"
```

---

### Task 27: 挤出面

**Files:**
- Modify: `src/StarDream.Core/Editing/MeshEditor.cs`
- Create: `tests/StarDream.Core.Tests/MeshEditorExtrudeTests.cs`
- Modify: `src/StarDream.App/ViewportControl.xaml.cs`（Ctrl+E 接线）

**Interfaces:**
- Produces:
  - `ExtrudeResult MeshEditor.Extrude(Mesh mesh, EditSelection sel, float distance)`
  - `ExtrudeResult`：`List<int> NewFaceIndices`、`string? Error`
  - 默认挤出距离 `0.2f`（米）

- [ ] **Step 1: 写失败测试**

```csharp
[Fact] public void Extrude_Single_Quad_Adds_Four_Side_Faces_And_Moves_Cap()
[Fact] public void Extrude_Whole_Cube_Is_Rejected_With_Error()
// 闭合选区没有边界边 → Error 非空，网格不变
[Fact] public void Extrude_Two_Opposite_Faces_Is_Rejected_With_Error()
// 法线互相抵消 → Error 非空，网格不变
[Fact] public void Extrude_Selects_New_Cap_Faces()
[Fact] public void Extrude_With_Zero_Distance_Does_Not_Throw()    // Review Focus 第 2 条
[Fact] public void Extrude_With_Empty_Selection_Returns_Error_Not_Exception()
// Review Focus 第 1 条
```

- [ ] **Step 2: 运行确认失败，然后实现**

按规格 §8.7：连通块拆分 → 逐块检查（无边界边 / 法线长度 < 0.1 则拒绝）→ 复制顶点 → 顶盖改指向 → 边界边补侧面 → 平移 `distance * normal` → 选择切到新顶盖。

- [ ] **Step 3: 运行确认通过并提交**

```bash
dotnet test --filter FullyQualifiedName~MeshEditorExtrudeTests
git add -A
git commit -m "feat: face extrusion with connected-component handling"
```

---

### Task 28: 完整 FBX 导出

**Files:**
- Create: `src/StarDream.Core/Export/UvProjector.cs`
- Modify: `src/StarDream.Core/Export/FbxExporter.cs`
- Create: `tests/StarDream.Core.Tests/FbxExportContentTests.cs`

**Interfaces:**
- Produces:
  - `UvProjector.BoxProject(Mesh mesh) → Vec2[]`（长度 = 角数）
  - `FbxExporter.Export(SceneDocument doc, string path)` 现在导出：每对象一个 node（T/R/S）、三角面、逐角法线、UV0、Phong 材质（`ufbxw_add_vec3(scene, id, "DiffuseColor", UFBXW_PROP_TYPE_COLOR, color)`）

- [ ] **Step 1: 写失败测试**

```csharp
[Fact] public void Export_Empty_Scene_Still_Writes_A_File()      // Review Focus 第 5 条
[Fact] public void UvProjection_Has_One_Uv_Per_Corner()
[Fact] public void Export_Cube_Produces_12_Triangles_And_8_Node_Vertices()
[Fact] public void Export_Preserves_Object_Transform()
// 导出后在 Blender 里读回，物体位置与场景一致（调用 Task 14 的脚本）
```

- [ ] **Step 2: 运行确认失败，然后实现**

- [ ] **Step 3: 运行确认通过并提交**

```bash
dotnet test --filter FullyQualifiedName~FbxExportContentTests
git add -A
git commit -m "feat: full fbx export with uvs materials and transforms"
```

---

### Task 29: 端到端导出验证（M9 门槛）

**Files:**
- Create: `tests/interop/roundtrip.ps1`
- Modify: `docs/superpowers/specs/2026-10-27-stardream-mvp-design.md`（记录验证结论）

**Interfaces:**
- Consumes: Task 14 的脚本、Task 28 的导出器
- Produces: 一条命令跑完 UE + Blender 校验。

- [ ] **Step 1: 造一个三物体场景**

立方体 + 球 + 圆柱，各带不同颜色和变换，导出到 `artifacts/scene.fbx`。

- [ ] **Step 2: 跑 UE 无头导入**

Run: `pwsh tests/interop/roundtrip.ps1`
Expected: 三个网格，三角形数与场景一致，包围盒误差 < 0.1 cm，朝向没翻（Y 在 UE 里是 Z 向上）。

- [ ] **Step 3: 跑 Blender 导入并比对**

Expected: 同样三个网格，位置与场景一致。

- [ ] **Step 4: Maya 人工确认一次**

打开 `artifacts/scene.fbx`，看形状、位置、朝向。
Expected: 与编辑器里一致。

- [ ] **Step 5: 记录结论并提交**

```bash
git add -A
git commit -m "test: end-to-end fbx validation against unreal, blender and maya"
```

---

### Task 30: 收尾打磨

**Files:**
- Modify: `src/StarDream.App/MainWindow.xaml`, `MainWindow.xaml.cs`, `ViewportControl.xaml.cs`
- Create: `src/StarDream.App/Controls/StatusBar.xaml`

**Interfaces:**
- Consumes: 前面所有任务
- Produces: 完整的快捷键表、状态栏、空场景提示。

- [ ] **Step 1: 快捷键**

按 Global Constraints 的表接 `KeyboardAccelerator`。注意 Alt+三键是相机导航，不要和菜单热键冲突。

- [ ] **Step 2: 状态栏**

显示工具、模式、选择数、对象数、三角形数。

- [ ] **Step 3: 空场景提示**

没有对象时视口中央显示"按 + 添加基本体"。

- [ ] **Step 4: 逐条走验收**

按规格 F1–F18 手工过一遍，把不通过项修掉。

- [ ] **Step 5: 提交**

```bash
git add -A
git commit -m "feat: shortcuts status bar and final polish"
```

---

## 自检结论

- **规格覆盖**：F1–F18 分别落在 Task 12/12/15-16/18/18/18/12/19-20/19/20/22-24/22-24/25/26/27/28-29/30/21。M0–M10 里程碑全部有对应任务，M1.5 在 Task 4，M3.5 在 Task 13–14。
- **Review Focus 覆盖**：5 条分别落在 Task 3、Task 5/18/27、Task 19、Task 3/25、Task 26/28。
- **类型一致性**：`Mesh`、`EditSelection`、`GizmoMode`、`SceneDocument` 的签名在各任务里一致。
- **比例**：本计划决定接口与验收，不写实现体。

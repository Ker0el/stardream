# 星梦（StarDream）

**免费开源的 Windows 桌面 3D 建模软件。** 界面参考 Blender 的分区，操作参考 Maya 的习惯，3D 视口用自写的 Direct3D 11 渲染器嵌在 WinUI 3 的 `SwapChainPanel` 里。

> 应用显示名是**星梦**，代码命名空间与程序集名是 `StarDream`，场景文件扩展名是 `.stardream`。

现有功能还很少，但它是完整的：从建模、编辑点线面，到保存工程、导出虚幻引擎可用的 FBX，整条链路都是通的。代码全部开源（MIT），欢迎 fork 和提 issue。

## 现在能做什么

- 基本体：立方体 / 球体 / 圆柱 / 平面，参数可在右侧「属性」里改
- 选择：视口左键点选，右侧「大纲」同步
- 变换：属性面板输入位置 / 旋转 / 缩放
- 存盘：`.stardream`（JSON），30 秒自动保存草稿，启动时询问是否恢复
- 编辑模式：F8 进入，F9 / F10 / F11 切点 / 线 / 面，左键选元素，Delete 删除，Ctrl+E 挤出面
- 导出 FBX：二进制、Y-up、厘米、三角化、逐角法线、UV0、Phong 材质，已在虚幻引擎 5.8 与 Blender 5.2.3 验证导入

## 环境要求

- Windows 11
- .NET SDK 10（构建用 .NET 8 目标框架）
- Visual Studio 2022/2026，需要 C++ 桌面工作负载（编译 FBX 原生库）
- Windows SDK 10.0.26100

## 构建与运行

```powershell
# 1. 先编译 FBX 原生库（生成 native/bin/x64/StarDream.FbxNative.dll）
powershell -File native/build.ps1

# 2. 构建
dotnet build StarDream.slnx

# 3. 跑起来
dotnet run --project src/StarDream.App
```

## 测试

```powershell
dotnet test tests/StarDream.Core.Tests/StarDream.Core.Tests.csproj
```

## 快捷键（Maya 习惯）

| 操作 | 键 |
|---|---|
| 环绕 / 平移 / 推拉 | Alt + 左键 / 中键 / 右键（滚轮也可推拉） |
| 选中 / 移动 / 旋转 / 缩放工具 | Q / W / E / R |
| 对象模式 ↔ 编辑模式 | F8 |
| 点 / 线 / 面 | F9 / F10 / F11 |
| 挤出面 | Ctrl + E |
| 删除 | Delete |
| 复制 | Ctrl + D |
| 聚焦选中 | F |
| 新建 / 打开 / 保存 | Ctrl + N / O / S |
| 导出 FBX | Ctrl + Shift + E |

## 导出到虚幻引擎

导出后在 UE 里导入时建议：Convert Scene ✓、Import Translation/Rotation/Scale ✓、Combine Meshes ✗、Generate Lightmap UVs ✓、Normals = Import。

FBX 导出用的是 [ufbx-write](https://github.com/ufbx/ufbx-write)（MIT / 公有领域），源码锁在 `native/StarDream.FbxNative` 的固定 commit。

## 许可证

本项目采用 [MIT](LICENSE)。

第三方组件：
- [ufbx-write](https://github.com/ufbx/ufbx-write)：MIT 或公有领域（任选其一），源码锁在 `native/StarDream.FbxNative` 的固定 commit，许可见 `ufbx_write.h` 末尾。
- Vortice.Windows、CommunityToolkit.WinUI、xUnit：各自原有许可。

## 项目结构

```
src/StarDream.Core/     数学、网格、场景、拾取、编辑、导出（纯逻辑，全部单测）
src/StarDream.Render/   Direct3D 11 渲染器
src/StarDream.App/      WinUI 3 外壳
native/                 ufbx-write 与它的构建脚本
tools/StarDream.Tools/  命令行工具（导演示场景等）
tests/                  单元测试与 UE / Blender 无头校验脚本
docs/superpowers/       设计文档与实现计划
```

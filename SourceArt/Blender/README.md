# 蚊子源资产

`Mosquito.blend` 是早期原型源文件。当前接入游戏的新蚊子源模型位于 `ReferenceAssets.blend`，导出至 Unity 的 `Assets/Game/Resources/Models/Mosquito.fbx`。

在仓库根目录重新生成：

```powershell
& 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe' --background --threads 6 --python .\Tools\create_reference_assets.py
```

当前蚊子 8,252 三角面，包含分节腹部、浅色环纹、复眼、双翼与翼脉、六条腿、触角和口器。Unity 导入开启网格读写，按材质和翅膀分组合并后进行 GPU 实例绘制；翅膜和翼脉一起摆动。`create_mosquito.py` 是早期原型脚本，不用于生成当前资产。

`ReferenceAssets.blend` 同时保留手掌（连续皮肤网格和指甲）、蓝框电蚊拍（交叉金属网）、绿色螺旋蚊香、招财猫和铁塔摆件。它们已分别导出为 Hand、Zapper、Incense、LuckyCat、Tower FBX；材质参数由 `ReferenceMaterials.json` 传给 Unity。手掌和电拍用于攻击动画，蚊香摆在桌面并在使用时释放烟雾，招财猫与铁塔位于桌面和书柜。造型为参考图的游戏化简化版本，非照片级复刻。

## 参考图书房

`Room.blend` 按用户提供的 Cozy Study Room 参考图制作房间与家具，采用适合当前游戏的简化造型，非照片级复刻。包含木地板、灰白抹灰墙、黑框窗、带抽屉书桌、布垫木椅、落地书柜、台灯、书本、地球仪、盆栽及垂吊藤蔓。

源文件按 Architecture、Window、Writing desk、Chair、Bookcase、Desktop ornaments、Wall details 分组，保留独立可编辑部件。当前房间有 888 个部件、91,516 三角面、21 个材质组。桌椅已调整为更接近真实家具的比例，椅背为弧形曲面，植物叶片有弯曲和厚度，墙上保留真实窗洞。

木纹、墙面与织物使用 ambientCG CC0 贴图，包含颜色、法线、粗糙度；Unity 使用打包的金属度/光滑度图。木纹跟随构件长度方向，按尺寸展开 UV。贴图已打包进 `.blend`，并保存在仓库中，日常编辑与运行不需要下载。窗外是独立生成的远景贴图，房间与家具仍是三维网格。素材来源与远景生成提示词见 `SourceArt/TextureSources.md`。

运行时使用 `Room.fbx`、`RoomMaterials.json` 和 `RoomTextures` 中的 PNG，将同材质物体合并以减少绘制批次。`StudyLighting` 统一编辑器与游戏的镜头、窗光、台灯、反射和调色。URP 开启软阴影、接触阴影和后处理。

```powershell
& 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe' --background --threads 6 --python .\Tools\create_room.py
```

可添加 `-- --skip-render` 跳过预览渲染。`Tools/room_modeling.py` 提供建模与贴图函数。输出统计见 `Room-report.json`，Blender 预览位于 `Docs/Preview/room-blender.png`。

修改 FBX 或材质参数后，通过编辑器菜单 `Mosquito/刷新书房模型与材质` 重新生成正式场景内的模型和材质；该操作会替换生成的 Blender 子对象，先保存个人场景修改。游戏运行会复用这些场景对象。批处理可执行 `Mosquito.Editor.ReferenceSceneSetup.Prepare`。正式场景为 `Assets/Game/Scenes/Observatory.unity`。

`Docs/Preview/reference-gameplay.png` 是 Unity 正式场景的游戏画面，包含 HUD 与蚊群。自动测试检查模型导入、PBR 贴图、场景显示与武器反馈，不代表已完成完整性能或手动输入验收。Blender 的离线光照与 Unity 实时光照仍有差异。

地面顶面 Y=-0.2、后墙内侧 Z=3.875、左墙内侧 X=-5.875 与游戏放卵坐标保持一致。Unity 运行时自动加载新房间；虫卵避开新地毯、书桌、书柜、窗户及装饰画。FBX 导出已处理 Unity 坐标系的左右转换。当前只是模型与源码更新，未构建安装包。

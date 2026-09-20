# 蚊子源资产

`Mosquito.blend` 是 Blender 5.2.1 LTS 创建的可编辑模型，导出至 Unity 的 `Assets/Game/Resources/Models/Mosquito.fbx`。

在仓库根目录重新生成：

```powershell
& 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe' --background --python .\Tools\create_mosquito.py
```

模型共 996 个三角面，包含身体、腹部、眼睛、双翼、六条腿和口器。Unity 导入开启网格读写，将同材质部件合并后以 GPU 实例绘制；翅膀在运行时摆动。

这是首个资产管线样例。公母外观差异、正式武器资产和最终房间美术仍待迭代。当前手掌、电拍、家具使用工程内的程序灰盒模型。

# 安装包资源

`MosquitoObservatory.iss` 由 `../Installer.ps1` 调用，将 Windows 构建打包为单个安装程序。

运行 `Tools/TestInstaller.ps1 -InstallerPath <安装包绝对路径>` 可以验证安装文件哈希、桌面与开始菜单快捷方式、游戏实际运行，以及卸载。测试会在 `TestResults` 下包含中文和空格的路径中安装，并临时写入当前用户的快捷方式和卸载注册信息，最后卸载测试副本。发现已有同名安装或快捷方式时会停止，避免影响已安装的游戏。运行日志和验证记录保存在 `TestResults/installer-*`。

`ChineseSimplified.isl` 为 Inno Setup 简体中文翻译，维护者 Zhenghan Yang；保留原文件中的署名。2026-09-20 从 [Inno Setup 官方翻译页面](https://jrsoftware.org/files/istrans/) 指向的 [源文件](https://raw.githubusercontent.com/jrsoftware/issrc/refs/heads/main/Files/Languages/ChineseSimplified.isl) 获取并随工程固定保存，避免不同编译器安装的语言资源差异。

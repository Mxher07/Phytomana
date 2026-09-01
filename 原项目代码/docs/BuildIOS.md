# iOS 编译与签名

## 环境要求

- macOS + Xcode（当前 CI 使用 26.6）
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- iOS 工作负载：`dotnet workload install ios`

## 首次构建前生成资源包

`Content.zip` 已被 gitignore，不会随仓库提供，缺失会导致构建报错：

```bash
cd Survivalcraft/Content
zip -r ../Content.zip *
cd ../..
```

## 编译 IPA

```bash
dotnet publish "Survivalcraft.IOS/Survivalcraft.IOS.csproj" -c Release -p:CodesignKey="Apple Development: 1144822034@qq.com (YZ3PDQZTKN)" -p:CodesignProvision=dac32268-04a1-4a17-8304-143d0ce4c49e -p:RuntimeIdentifier=ios-arm64 -nowarn:CS8618,CS8765,CS8625,CS8600,CS8602,CS8603 /p:Codesign=false /p:EnableCodeSigning=false -p:MtouchUseLlvm=false
```

- Debug 版将 `-c Release` 改为 `-c Debug`
- 产物：`Survivalcraft.IOS/bin/Release/net10.0-ios/ios-arm64/publish/Survivalcraft.ipa`
- 上述命令已禁用签名，产物为未签名 IPA

> **必须携带 `-p:MtouchUseLlvm=false`**：`.NET for iOS` 的 LLVM 后端处理本项目的大型合并程序集时会在 `_AOTCompile` 阶段挂起（编译进程 CPU 归零、输出 0 字节、长时间无进展直至死机）。该开关使产物原生代码不经 LLVM 优化，体积略大、性能略低，但可保证编译完成。

> **改了代码必须全量重编**：先删除 `Survivalcraft.IOS/obj`、`Survivalcraft.IOS/bin`、`Engine.IOS/obj`、`Engine.IOS/bin`、`EntitySystem.IOS/obj`、`EntitySystem.IOS/bin` 再执行上述 publish。增量发布会跳过 AOT 原生码重编，打出的 IPA 里 AOT 镜像与新程序集 MVID 不匹配，启动即在 `load_aot_module` 处 SIGABRT。编译中途也不要杀进程，残缺的 AOT 中间产物同样会导致该崩溃。

## 签名

未签名 IPA 需自行签名后才能安装。

### PlayCover（Mac 运行 iOS 应用）

```bash
# 1. 解包
mkdir -p /tmp/ipa && cd /tmp/ipa
unzip -q 你的.ipa

# 2. 对框架和主程序做 ad-hoc 签名（无需证书）
for f in Payload/*.app/Frameworks/*.framework; do
  codesign --force --sign - --timestamp=none "$f"
done
codesign --force --sign - --timestamp=none Payload/*.app

# 3. 重新打包
zip -qr 你的-signed.ipa Payload/
```

将 `你的-signed.ipa` 拖入 PlayCover 即可安装。
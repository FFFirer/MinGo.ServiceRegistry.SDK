# 本地开发

本仓库（`service-registry-sdk`）自身没有跨仓库依赖，可直接构建。若你还要在本地同时运行 Registry Server（`service-registry` 仓库），Server 通过 NuGet 包消费 `MinGo.ServiceRegistry.Abstractions`，需要一个本地引导把该包打进本地 feed。

## 前置条件

- .NET 10 SDK（`dotnet --version` 应为 `10.x`）。
- 构建/测试：

```bash
dotnet build ServiceRegistry.Sdk.slnx
dotnet test  ServiceRegistry.Sdk.slnx
dotnet pack  ServiceRegistry.Sdk.slnx -c Release -o ./artifacts
```

## 无需 Registry Server 的开发（配置降级）

业务侧的下游 `HttpClient` 代码在生产与本地**完全一致**，差别只在 DI 注册的一行：

```csharp
// 生产：由 Registry Server 解析逻辑名
builder.Services.AddRegistryServiceDiscovery();

// 本地：由配置解析逻辑名（官方 Microsoft.Extensions.ServiceDiscovery）
builder.Services.AddServiceDiscovery();
```

下游客户端两种情况下都一样：

```csharp
builder.Services.AddHttpClient("downstream", c => c.BaseAddress = new Uri("http://registration-sample"))
    .AddServiceDiscovery();
```

本地在 `appsettings.json` 里用 `Services` 节点把逻辑名映射到真实地址：

```jsonc
{
  "Services": {
    "registration-sample": { "http": [ "http://localhost:5101" ] }
  }
}
```

`samples/LocalDevelopmentSample` 演示了这种零依赖的本地运行方式。

## 运行 samples

| 样例 | 端口 | 作用 |
| --- | --- | --- |
| `RegistrationSample` | `5101` | 作为提供方注册自身并心跳续约（需要 Registry Server） |
| `DiscoverySample` | `5102` | 作为消费方，经 Registry Server 解析并调用 `registration-sample`（需要 Registry Server） |
| `LocalDevelopmentSample` | `5103` | 作为消费方，经 `appsettings.json` 降级解析（**不需要** Registry Server） |

Registry Server 默认监听 `http://localhost:5080`（见 Server 仓库）。启动顺序：先 Server，再 `RegistrationSample`，然后 `DiscoverySample` 调用 `GET /`。

```bash
dotnet run --project samples/RegistrationSample
dotnet run --project samples/DiscoverySample
dotnet run --project samples/LocalDevelopmentSample
```

## 跨仓库本地引导（同时开发 Server 时）

工作区根目录 `MinGo.ServiceRegistry.All/` 只是本地组织文件夹，**不是** git 仓库，其中的 `local/` 也不纳入任一仓库的版本控制。它提供：

- `local/build-local.ps1`：把 SDK 的 `Abstractions` 打包进 `local/artifacts/`（本地 feed），在机器级注册名为 `mingo-local` 的 NuGet 源，然后分别构建/测试两个仓库。
- 这样 Server 仓库无需真实 feed 即可还原 `MinGo.ServiceRegistry.Abstractions`。

```powershell
# 在工作区根目录执行
pwsh ./local/build-local.ps1            # 打包 + 构建 + 测试两个仓库
pwsh ./local/build-local.ps1 -SkipTests # 只构建
```

CI/生产环境改用真实 feed（nuget.org 或 GitHub Packages）还原 Abstractions，不依赖此脚本。

# MinGo Service Registry SDK

核心抽象 + 客户端 SDK。本仓库只产出 NuGet 包，不包含可独立运行的 Registry Server（见 `service-registry` 仓库）。

## 包划分

| 包 | 职责 |
| --- | --- |
| `MinGo.ServiceRegistry.Abstractions` | 领域模型、协议 DTO、`IServiceRegistryClient` 抽象、JSON 源生成上下文 |
| `MinGo.ServiceRegistry.Client` | 基于 `HttpClient` 的 Registry 协议客户端实现 |
| `MinGo.ServiceRegistry.AspNetCore` | `AddServiceRegistry(...)` + 注册/心跳/注销的 `IHostedService` |
| `MinGo.ServiceRegistry.ServiceDiscovery` | 对接 `Microsoft.Extensions.ServiceDiscovery` 的终结点 Provider |
| `MinGo.ServiceRegistry` | 聚合上述四包的元包，业务服务一键安装 |

## 依赖方向

```
Abstractions  <-  Client  <-  AspNetCore
                        \-  ServiceDiscovery
```

## 快速开始

```csharp
var builder = WebApplication.CreateBuilder(args);

// 1. 作为服务提供方：注册自身 + 心跳
builder.Services.AddServiceRegistry(o =>
{
    o.ServiceName = "user-service";
    o.RegistryEndpoint = new Uri("https://registry:8500");
});

// 2. 作为服务消费方：通过服务名调用（生产用 Registry Provider）
builder.Services.AddRegistryServiceDiscovery();
builder.Services.AddHttpClient<UserServiceClient>(c => c.BaseAddress = new Uri("https://user-service"))
    .AddServiceDiscovery();
```

本地开发可改用官方 Configuration Provider（`builder.Services.AddServiceDiscovery();` + `appsettings.json` 的 `Services` 节点），业务代码不变。详见 `docs/local-development.md`。

## 构建

```bash
dotnet build ServiceRegistry.Sdk.slnx
dotnet test  ServiceRegistry.Sdk.slnx
dotnet pack  ServiceRegistry.Sdk.slnx -c Release -o ./artifacts
```

## 目标框架

`net10.0`

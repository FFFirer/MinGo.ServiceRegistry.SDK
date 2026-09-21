# SDK 架构

本仓库（`service-registry-sdk`）只产出 NuGet 包，为业务服务提供两类能力：

1. **作为服务提供方**：把自身注册到 Registry Server，并通过心跳维持租约（lease）。
2. **作为服务消费方**：用逻辑服务名（如 `http://user-service`）调用下游，由 Microsoft Service Discovery 解析为真实端点。

Registry Server 本身在独立仓库 `service-registry` 中，通过 NuGet 包 `MinGo.ServiceRegistry.Abstractions` 与本仓库共享协议契约。

## 包结构与依赖方向

```
Abstractions  <-  Client  <-  AspNetCore
                        \-  ServiceDiscovery
                              MinGo.ServiceRegistry (元包，聚合以上四包)
```

| 包 | 目标框架 | 职责 |
| --- | --- | --- |
| `MinGo.ServiceRegistry.Abstractions` | `net10.0` | 领域模型、协议 DTO、`IServiceRegistryClient` 抽象、`RegistryJsonContext`（STJ 源生成） |
| `MinGo.ServiceRegistry.Client` | `net10.0` | 基于 `HttpClient` 的协议客户端 `RegistryClient` |
| `MinGo.ServiceRegistry.AspNetCore` | `net10.0` | `AddServiceRegistry(...)` + 注册/心跳/注销的 `ServiceRegistrationHostedService` |
| `MinGo.ServiceRegistry.ServiceDiscovery` | `net10.0` | 对接 `Microsoft.Extensions.ServiceDiscovery` 的终结点 Provider |
| `MinGo.ServiceRegistry` | — | 无代码元包，一键安装上述四包 |

依赖只向下：`Abstractions` 不依赖任何本仓库包；`Client` 仅依赖 `Abstractions`；`AspNetCore` 依赖 `Client`；`ServiceDiscovery` 依赖 `Client`。

## 关键组件

### Abstractions

- **领域模型**：`ServiceRegistration`（注册入参）、`RegistrationResult{ LeaseId, InstanceId, ExpiresAt, TtlSeconds }`、`ServiceInstance`（发现出参）、`RenewResult`、`LeaseOptions`、`enum ServiceHealthStatus { Registered, Healthy, Expired }`。
- **`IServiceRegistryClient`**：`RegisterAsync` / `RenewLeaseAsync(leaseId)` / `DeregisterAsync(leaseId)` / `DiscoverAsync(serviceName)`。客户端刻意**不**内置重试、熔断、负载均衡——这些交给 `Microsoft.Extensions.Http.Resilience` 与 `Microsoft.Extensions.ServiceDiscovery`。
- **`RegistryJsonContext`**：`System.Text.Json` 源生成上下文，`camelCase` 命名、写时忽略 `null`，AOT 友好。Client 与 Server 共用同一份契约。
- **`ServiceRegistryDefaults`**：跨 SDK/Server 共享的默认值（TTL 15s、reaper 5s、发现刷新 15s、默认 scheme `http`）。

### Client

`RegistryClient(HttpClient, IOptions<ServiceRegistryClientOptions>, ILogger)` 把接口方法映射到 HTTP 端点（见 `protocol.md`）。异常模型：

- `RegistryException`：携带 `StatusCode`，表示非成功的协议调用。
- `RegistryLeaseLostException`（派生自 `RegistryException`）：续约遇到 `404` 时抛出，语义为“租约已丢失，调用方必须重新注册”。
- `DeregisterAsync` 幂等：`404` 不视为错误。

DI：`AddServiceRegistryClient(...)` 注册 typed `HttpClient`（`BaseAddress` 来自 `ServiceRegistryClientOptions.RegistryEndpoint`）与 `IServiceRegistryClient`。

### AspNetCore

- `AddServiceRegistry(Action<ServiceRegistryOptions>)`：注册 options、`IServiceRegistryClient` 与 `ServiceRegistrationHostedService`。
- `ServiceRegistryOptions`：`ServiceName`（必填）、`RegistryEndpoint`（必填）、`InstanceId?`、`Scheme/Host/Port?`（缺省从 `IServerAddressesFeature` 探测）、`Metadata`、`LeaseTtlSeconds=15`、`HeartbeatInterval?`（默认 TTL/3）、`RegisterOnStart=true`、`DeregisterOnStop=true`。
- `ServiceRegistrationHostedService`（`IHostedService`）：
  - **Start** → `RegisterAsync` 保存服务端签发的 `leaseId` → 用 `PeriodicTimer`（基于注入的 `TimeProvider`，带 ±10% jitter）周期性 `RenewLeaseAsync`。
  - 续约抛 `RegistryLeaseLostException` → **重新注册**并更新 `leaseId`。
  - 连续失败 → 指数退避。
  - **Stop** → 取消计时器 → best-effort `DeregisterAsync(leaseId)`（受 `DeregisterOnStop` 控制）。

### ServiceDiscovery

- `RegistryServiceEndpointProviderFactory`：对 `http`/`https` 的 `ServiceEndpointQuery` 创建 Provider。
- `RegistryServiceEndpointProvider`（实现 `IServiceEndpointProvider`, `IHostNameFeature`）：`PopulateAsync` 调用 `DiscoverAsync(query.ServiceName)`，为每个实例生成 `ServiceEndpoint`（`UriEndPoint`）加入 `builder.Endpoints`，并挂上刷新 `ChangeToken`。
- `RegistryEndpointChangeTokenSource`：默认每 15s（`ServiceRegistryServiceDiscoveryOptions.RefreshInterval`）轮询产出 `IChangeToken` 触发重解析。MVP 不做 Server 推送/watch。
- DI：`AddRegistryServiceDiscovery()` → `AddServiceDiscoveryCore()` + `RegistryServiceEndpointProviderFactory`。

## 横切设计

- **可测时间源**：SDK 与 Server 全链路注入 `TimeProvider`；测试用 `FakeTimeProvider`（命名空间 `Microsoft.Extensions.Time.Testing`）驱动心跳、过期与刷新，无 `Thread.Sleep`。
- **序列化**：统一走 `RegistryJsonContext` 源生成，避免运行时反射，兼容 Native AOT / trimming。
- **本地降级**：业务侧 `HttpClient` 代码在生产（Registry Provider）与本地（配置 Provider）之间保持一致，详见 `local-development.md`。

# 协议契约（HTTP API）

Registry Server 与 SDK 共享的 HTTP 协议。契约模型定义在 `MinGo.ServiceRegistry.Abstractions`，序列化统一走 `RegistryJsonContext`（`System.Text.Json` 源生成，`camelCase`，写时忽略 `null`）。本文档与 Server 仓库 `docs/api.md` 描述同一套端点。

- **Base path**：`/api/registry`
- **Content-Type**：`application/json`
- **错误模型**：RFC 7807 `application/problem+json`（`ProblemDetails` / `ValidationProblemDetails`）
- **时间**：所有 `expiresAt` 为 UTC ISO-8601（`yyyy-MM-ddTHH:mm:ss.fffffffZ`）

## 端点总览

| 方法 | 路由 | 成功 | 失败 |
| --- | --- | --- | --- |
| `POST` | `/api/registry/services/{serviceName}/instances` | `201` + `RegistrationResult` | `400` `ValidationProblemDetails` |
| `PUT` | `/api/registry/leases/{leaseId}` | `200` + `RenewResult` | `404`（租约丢失/已过期） |
| `DELETE` | `/api/registry/leases/{leaseId}` | `204` | `404` |
| `GET` | `/api/registry/services/{serviceName}/instances` | `200` + `ServiceInstancesResponse` | — |
| `DELETE` | `/api/registry/services/{serviceName}/instances/{instanceId}` | `204`（管理用途） | `404` |
| `GET` | `/api/registry/services` | `200` + `ServiceListResponse` | — |
| `GET` | `/health` | `200`（Server 自身健康） | — |

## 租约（lease）语义

- 注册成功由**服务端签发** `leaseId`（GUID），并通过 `201` 的 `Location: /api/registry/leases/{leaseId}` 返回。
- SDK 的整个生命周期都围绕 `leaseId`：续约 `PUT /leases/{leaseId}`、注销 `DELETE /leases/{leaseId}`。
- **`404` 表示租约丢失**（被 reaper 回收或已过期）。续约收到 `404` 时，SDK 抛 `RegistryLeaseLostException` 并触发**重新注册**；注销收到 `404` 视为幂等成功。
- 已过期的租约**不可续约**（返回 `404`），保证到期剔除行为的确定性。

## 请求 / 响应模型

### `POST .../{serviceName}/instances`

请求体 `ServiceRegistration`：

```jsonc
{
  "instanceId": "optional-client-supplied-id", // 可空，缺省由服务端生成
  "scheme": "http",                            // 缺省 http
  "host": "10.0.0.5",
  "port": 5101,
  "metadata": { "zone": "a" },                 // 可空
  "lease": { "ttlSeconds": 15 }                // 可空；服务端会 clamp 到 [Min,Max]
}
```

`201` 响应体 `RegistrationResult`：

```jsonc
{
  "leaseId": "9c1b...guid",
  "instanceId": "generated-or-supplied",
  "expiresAt": "2024-01-01T00:00:15.0000000Z",
  "ttlSeconds": 15
}
```

`400` 响应体 `ValidationProblemDetails`（字段级错误，如缺失 `host`/非法 `port`）。

### `PUT /leases/{leaseId}`

`200` 响应体 `RenewResult`：

```jsonc
{ "leaseId": "9c1b...guid", "expiresAt": "2024-01-01T00:00:30.0000000Z" }
```

`404`：`ProblemDetails`（`detail: "Lease not found."`）。

### `GET .../{serviceName}/instances`

`200` 响应体 `ServiceInstancesResponse`，**仅返回 Healthy 且未过期**的实例：

```jsonc
{
  "serviceName": "user-service",
  "instances": [
    {
      "serviceName": "user-service",
      "instanceId": "abc",
      "scheme": "http",
      "host": "10.0.0.5",
      "port": 5101,
      "metadata": { "zone": "a" },
      "health": 1 // enum ServiceHealthStatus: 0=Registered,1=Healthy,2=Expired
    }
  ]
}
```

### `GET /api/registry/services`

`200` 响应体 `ServiceListResponse`：

```jsonc
{ "services": ["user-service", "order-service"] }
```

## 序列化约定

- 属性名 `camelCase`；`null` 值不写出。
- `enum ServiceHealthStatus` 以整数写出（`Registered=0`、`Healthy=1`、`Expired=2`）。
- Client 与 Server 都通过 `RegistryJsonContext.Default` 解析，契约一致性由 Server 仓库的 `ProtocolTests` 守护。

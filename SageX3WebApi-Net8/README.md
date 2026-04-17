# Sage X3 Web API (.NET 8)

A modern ASP.NET Core 8 Web API that wraps Sage X3's **CAdxWebServiceXmlCC** SOAP service (the generic web service that invokes ADX sub-programs and performs standard business-object CRUD).

## Endpoints

| Operation | Method | Route |
|---|---|---|
| Get Invoice OK2Pay | `GET` / `POST` | `/api/invoices/ok2pay` |
| Insert Invoice Payment | `POST` | `/api/invoices/payments` |
| Get Lookup Values | `GET` / `POST` | `/api/lookups/{lookupName}` |
| Insert Remit-To Address | `POST` | `/api/remitto` |
| Get Supplier Information (SIM) | `GET` | `/api/suppliers/{supplierCode}/sim` |
| Get Supplier | `GET` | `/api/suppliers/{supplierCode}` |
| Insert Supplier | `POST` | `/api/suppliers` |
| Health check | `GET` | `/health` |
| Swagger UI | `GET` | `/swagger` |

All endpoints return a uniform envelope:

```json
{
  "success": true,
  "data": { /* payload */ },
  "error": null,
  "messages": [ { "type": "INFO", "message": "..." } ]
}
```

## What's new vs. the .NET Framework 4.8 version

| Concern | .NET 4.8 version | .NET 8 version |
|---|---|---|
| Hosting | IIS / `Global.asax` / `App_Start/*` | Minimal hosting in `Program.cs` |
| HTTP | `HttpWebRequest` | Typed `HttpClient` via `IHttpClientFactory` |
| Resilience | None | Polly retry (3x exponential backoff on transient HTTP errors) |
| JSON | Newtonsoft.Json | `System.Text.Json` (`JsonDocument` / `JsonElement`) |
| Configuration | `ConfigurationManager.AppSettings` strings | Strongly-typed `IOptions<SageX3Options>` with validation |
| Logging | none | `ILogger<T>` throughout |
| Cancellation | none | `CancellationToken` plumbed end-to-end |
| Nullability | flag-only | `<Nullable>enable</Nullable>`, explicit `?` and `required` members |
| API docs | Swashbuckle optional | Swashbuckle 6.9 + XML comments on by default |
| Deployment | IIS / IIS Express | Kestrel, Docker, Linux, or IIS |

## Solution structure

```
SageX3WebApi.sln
└── SageX3WebApi/
    ├── Program.cs                      Minimal hosting + DI + Swagger + Polly
    ├── SageX3WebApi.csproj             SDK-style, net8.0
    ├── SageX3WebApi.http               Ready-to-run REST samples
    ├── appsettings.json                Sage X3 connection + sub-prog codes + CORS
    ├── appsettings.Development.json
    ├── Dockerfile
    ├── Properties/launchSettings.json
    ├── SageX3SoapClient/
    │   ├── SageX3Options.cs            Strongly-typed config
    │   ├── SageX3Client.cs             Typed HttpClient
    │   ├── SoapEnvelopeBuilder.cs      run / save / query envelopes
    │   └── SoapResponseParser.cs       status / messages / resultXml/JSON
    ├── Models/                         Request / response DTOs
    ├── Services/                       Interfaces + impls + SagePayload helper
    └── Controllers/                    4 controllers
```

## Getting started

### Prerequisites

- .NET 8 SDK ([download](https://dotnet.microsoft.com/download/dotnet/8.0))
- Any of: Visual Studio 2022 17.8+, Rider 2023.3+, or VS Code with the C# Dev Kit

### Run locally

```bash
dotnet restore
dotnet run --project SageX3WebApi
```

Open `http://localhost:5080/swagger`.

### Configure Sage X3 connection

Edit `SageX3WebApi/appsettings.json` → `SageX3` section:

```json
"SageX3": {
  "EndpointUrl": "http://YOUR-SAGE-SERVER:8124/soap-generic/syracuse/collaboration/syracuse/CAdxWebServiceXmlCC",
  "PoolAlias": "SEED",
  "Username": "admin",
  "Password": "admin",
  "Language": "ENG",
  "RequestConfig": "adxwss.optreturn=JSON&adxwss.beautify=true&adxwss.trace.on=off",
  "TimeoutSeconds": 120,
  "SubPrograms": {
    "GetInvoiceOk2Pay": "YOK2PAY",
    "InsertInvoicePayment": "YINVPAY",
    "GetLookupValues": "YLOOKUP",
    "InsertRemitToAddress": "YREMITTO",
    "GetSupplierInformationSIM": "YSUPSIM",
    "GetSupplier": "YSUPGET",
    "InsertSupplier": "YSUPINS"
  }
}
```

Options are validated on startup — if `EndpointUrl`, `PoolAlias`, `Username`, or `Password` are missing the app will fail fast with a clear error.

### Use user-secrets for credentials (recommended in dev)

```bash
cd SageX3WebApi
dotnet user-secrets init
dotnet user-secrets set "SageX3:Username" "youruser"
dotnet user-secrets set "SageX3:Password" "yourpass"
```

In production use environment variables or an Azure Key Vault configuration provider:

```bash
export SageX3__Username=youruser
export SageX3__Password=yourpass
```

### Sub-program vs. business-object fallback

If you leave a sub-program value empty/missing, services fall back to standard Sage X3 WSDL business objects:

| Service | Fallback |
|---|---|
| `GetSupplier` | `query` on `BPS` (Supplier) |
| `InsertSupplier` | `save` on `BPS` |
| `InsertRemitToAddress` | `save` on `BPA` (Business Partner Address) |

This lets the API work against a stock X3 installation without requiring custom ADX code.

### Docker

```bash
docker build -t sagex3-webapi -f SageX3WebApi/Dockerfile .
docker run -p 8080:8080 \
  -e SageX3__EndpointUrl=http://your-sage-server:8124/... \
  -e SageX3__Username=admin \
  -e SageX3__Password=admin \
  sagex3-webapi
```

## Sample requests

The `SageX3WebApi/SageX3WebApi.http` file has ready-to-run requests for every endpoint. Open it in VS 2022 or VS Code with the REST Client extension and click "Send Request".

## How it talks to Sage X3

The client hand-crafts SOAP envelopes via `SoapEnvelopeBuilder` and posts them with `HttpClient`. We don't use `svcutil` / `dotnet-svcutil` service references because Sage X3's WSDL occasionally contains types that don't round-trip cleanly through generated proxies.

Three envelope shapes:

- **`run`** — invokes an ADX sub-program by `publicName`, passes a string `inputXml` payload. Used for custom entry points (`YOK2PAY`, `YINVPAY`, etc.).
- **`save`** — creates/updates a business object by `publicName` (`BPS`, `BPA`, `PIH`, `PAYO`…). Standard X3 WSDL objects.
- **`query`** — reads a business object by primary key(s).

`requestConfig` sets `adxwss.optreturn=JSON`, so Sage returns JSON inside the `resultXml` element. The parser auto-detects JSON vs. XML payload.

## Field-code mapping

Services build Sage X3 `<PARAM>/<GRP>/<FLD>` payloads using standard field codes (`BPRNUM`, `BPSNAM`, `BPAADDLIG_1`, `POSCOD`, `BIDNUM`, etc.). If your custom sub-programs use different codes, adjust the `SagePayload.AppendField(...)` calls in the relevant service.

## Notes

- **TLS** — modern .NET uses the OS's TLS defaults; no manual `ServicePointManager` fiddling required.
- **CORS** — configurable via `Cors:AllowedOrigins` in `appsettings.json`. Default is `["*"]`; lock it down in production.
- **Cancellation** — every controller and service accepts a `CancellationToken` so client disconnects short-circuit the SOAP call.
- **Retry** — Polly retries transient HTTP errors (5xx, 408, network) 3 times with exponential backoff. Configure in `Program.cs`.
- **Health** — `/health` responds 200 OK when the app is up. Extend `AddHealthChecks()` to add a Sage X3 connectivity probe.

## License

Provided as-is. Use at your own risk.

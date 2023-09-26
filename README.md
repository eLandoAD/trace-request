# Package for Request Tracing with Serilog

This package provides an easy configuration to trace HTTP requests using Serilog. Please follow the steps below to set it up.

# Configuration Steps 

### 1. Configure Serilog for Elasticsearch

In order to use this package, you need to configure Serilog to work with Elasticsearch. You can do this by calling `AddElasticLogging()` in `SerilogConfigurationExtensions.cs`. Additionally, make sure to provide the following configuration parameters:
- `elasticUri`
- `indexPrefix`
- `minLoggingLevel`

#### OR

UseDirect Host Configuration about Serilog (with Elastic). You can do this by calling `builder.Host.AddLogger()` in `HostExtension.cs`. Additionally, make sure to provide the following configuration parameters:
- `builder.Configuration` 
- `LogEventLevel logLevel = LogEventLevel.Information`  
 
### 2. Add configuration property for header name into appsettings.json
```json
{
  "TraceIdKey": "Your-Custom_Key-Name",
  "ElasticUri": "your.elastic.uri/",
  "LoggerPrefix": "Your-Custom_Prefix",
  "GlobalLogingFilter": "Your-Custom_Loging-Filter" // Optional
}
````

### 3. Add HttpContextAccessor via Dependency Injection

Each middleware in this package requires access to the HTTP context. To provide access, add the `HttpContextAccessor` to your DI container using:

```csharp
builder.Services.AddHttpContextAccessor();
```

### 4. Configure HTTP Logging
Configure HTTP request/response logging using the following code:

```csharp 
builder.Services.AddHttpLogging(logging =>
{
    logging.RequestHeaders.Add(builder.Configuration.GetHeaderName());
});
```


### 5. Use Trace Identifier Middleware 
Not needed for application inside the kube8.
Add the TraceIdentifierMiddleware as the first middleware in your application to include a new trace header with a unique GUID:

```csharp 
app.UseMiddleware<HttpTraceMiddleware>();
```
### 6. Use HTTP Logging Middleware
Use UseHttpLogging() as the second middleware in your application to log the HTTP requests and responses:

```csharp
app.UseHttpLogging();
```

### 7. Additional Configuration
Not needed for application inside the kube8.
If you need to trace:

#### 7.1. gRPC Calls
Add the TraceIdentifierInterceptor to your services:

```csharp
services.AddSingleton<HttpTraceInterceptor>();
```

Configure each gRPC client with the TraceIdentifierInterceptor:
```csharp
services.AddGrpcClient<gRPCClient>(options => ...)
        .AddInterceptor<HttpTraceInterceptor>();
```

#### 7.2. HTTP Requests
Note that HTTP requests are not implemented yet and require further development.
 
### 8. Logging Extensions
Use LoggerTraceExtensions based on 'Microsoft.Extensions.Logging.ILogger'.

#### 8.1 Example: How to use LoggerTraceExtensions.
```csharp
var traceId = this.HttpContext.GetTraceId();
_logger.LogModelWithDepthOne(request, traceId);
```
The traceId argument enables tracing of the log in an application & across microservice architecture.

#### 8.2 Example: Redact sensitive data - object
```csharp
var traceId = this.HttpContext.GetTraceId();
_logger.LogModelWithDepthOne(model, traceId, nameof(model.Phone));
```
#### 8.3 Example: Redact sensitive data - collection
```csharp
var traceId = this.HttpContext.GetTraceId();

// New code
IEnumerable<TypeModel> copy = model.DeepCopy();
copy.RedactSensitiveData(nameof(TypeModel.Address), nameof(TypeModel.Phone));

_logger.LogModelWithDepthOne(copy, traceId);
```

### 9. Hints:

#### 9.1 Task.Run(....)
```csharp
   var traceId = _contextAccessor.HttpContext.GetTraceId(_configuration);

 var headers = new Metadata {{_configuration.GetHeaderName(), traceId.ToString() } }

_grpcClient.CallAsync (new RPCModelRequest(), headers);

```

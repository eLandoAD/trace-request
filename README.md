# Package for Request Tracing with Serilog

This package provides an easy configuration to trace HTTP requests using Serilog. Please follow the steps below to set it up.

## Configuration Steps

### 1. Configure Serilog for Elasticsearch

In order to use this package, you need to configure Serilog to work with Elasticsearch. You can do this by calling `AddElasticLogging()` in `SerilogConfigurationExtensions.cs`. Additionally, make sure to provide the following configuration parameters:
- `elasticUri`
- `indexPrefix`
- `minLoggingLevel`

### 2. Add HttpContextAccessor via Dependency Injection

Each middleware in this package requires access to the HTTP context. To provide access, add the `HttpContextAccessor` to your DI container using:

```csharp
builder.Services.AddHttpContextAccessor();
```
### 3. Configure HTTP Logging
Configure HTTP request/response logging using the following code:

```csharp 
builder.Services.AddHttpLogging(logging =>
{
    logging.RequestHeaders.Add(TRACE_IDENTIFIER_KEY);
});
```
### 4. Use Trace Identifier Middleware
Add the TraceIdentifierMiddleware as the first middleware in your application to include a new trace header with a unique GUID:

```csharp 
app.UseMiddleware<TraceIdentifierMiddleware>();
```
### 5. Use HTTP Logging Middleware
Use UseHttpLogging() as the second middleware in your application to log the HTTP requests and responses:

```csharp
app.UseHttpLogging();
```

### 6. Additional Configuration
If you need to trace:

## 6.1. gRPC Calls
Add the TraceIdentifierInterceptor to your services:

```csharp
services.AddSingleton<TraceIdentifierInterceptor>();
```

Configure each gRPC client with the TraceIdentifierInterceptor:
```csharp
services.AddGrpcClient<gRPCClient>(options => ...)
        .AddInterceptor(new TraceIdentifierInterceptor(IHttpContextAccessor, TRACE_IDENTIFIER_KEY));
```

## 6.2. HTTP Requests
Note that HTTP requests are not implemented yet and require further development.
  
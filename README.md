# trace-request
Packege implement easy configuration to trace reqests via Serrilog. To setup, Please, read the description!

TO configure the app do:

 1. Confirure Serrilog to work with ElasticSearch - use AddElasticLogging() in SerrilogCOnfigurationExtesions.cs. Also you must elasticUri, indexPrefix and minLoggingLevel;
 2. Each middlware need Http Accessor, that if need add it via DI builder.Services.AddHttpContextAccessor();
 3. Use Http logging with options:
                builder.Services.AddHttpLogging(logging =>
                { 
                    logging.RequestHeaders.Add(TRACE_IDENTIFIER_KEY);
                });
 4. Use HttpTraceMiddleware.cs as 1st middleware to add new trace header with new Guid;
                app.UseMiddleware<TraceIdentifierMiddleware>();

 5. Use UseHttpLogging() as 2nd middleware to add log the http;
               app.UseHttpLogging();

 6. Next. If make:
  6.1. gRPC calls- use HttpTraceMiddlaware.cs:
    6.1.1. Add to builder app:
                    services.AddSingleton<TraceIdentifierInterceptor>();
    6.1.2. Add to each gRpc Client configuration
                   services.AddGrpcClient<gRPCClient>(options...)
                           .AddInterceptor<>(new TraceIdentifierInterceptor (IHttpContextAccessor, TRACE_IDENTIFIER_KEY))
  6.2. Http requests - Not Implemented yeth. 
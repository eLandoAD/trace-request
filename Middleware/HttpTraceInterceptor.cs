#region Usings
using elando.ELK.TraceLogging.Extensions;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.AspNetCore.Http;
#endregion

namespace elando.ELK.TraceLogging.Middleware
{
    //
    // Summary:
    //     gRPC Intercetor. Add Trace Header to gRPC context with key from HttpContext.
    //
    // Type parameters:
    //   ContextAccessor:
    //     The type of IHttpContextAccessor.
    //
    //   TraceHeaderName:
    //     The type of string.
    public class HttpTraceInterceptor : Interceptor
    {
        #region Fields
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly string _traceHeaderName;
        #endregion

        #region Ctors
        /// <summary>
        /// Add TRACE_IDENTIFIER_KEY from HttpContext
        /// </summary>
        /// <param name="contextAccessor"></param>
        /// <param name="traceHeaderName"></param>
        public HttpTraceInterceptor(IHttpContextAccessor contextAccessor, string traceHeaderName)
        {
            _httpContextAccessor = contextAccessor;
            _traceHeaderName = traceHeaderName;
        }
        #endregion

        #region Overrides Server Calls
        /// <summary>
        /// Override of AsyncUnaryCall, add trace-key (from httpContext) to gRPC context 
        /// </summary>
        /// <typeparam name="TRequest"></typeparam>
        /// <typeparam name="TResponse"></typeparam>
        /// <param name="request"></param>
        /// <param name="context"></param>
        /// <param name="continuation"></param>
        /// <returns></returns>
        public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(TRequest request, ClientInterceptorContext<TRequest, TResponse> context, AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
        {
            return base.AsyncUnaryCall(request, ContextAddTracing(context), continuation);
        }

        /// <summary>
        /// Override of AsyncClientStreamingCall, add trace-key (from httpContext) to gRPC context 
        /// </summary>
        /// <typeparam name="TRequest"></typeparam>
        /// <typeparam name="TResponse"></typeparam>
        /// <param name="context"></param>
        /// <param name="continuation"></param>
        /// <returns></returns>
        public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(ClientInterceptorContext<TRequest, TResponse> context, AsyncClientStreamingCallContinuation<TRequest, TResponse> continuation)
        {
            return base.AsyncClientStreamingCall(ContextAddTracing(context), continuation);
        }

        /// <summary>
        /// Override of AsyncDuplexStreamingCall, add trace-key (from httpContext) to gRPC context
        /// </summary>
        /// <typeparam name="TRequest"></typeparam>
        /// <typeparam name="TResponse"></typeparam>
        /// <param name="context"></param>
        /// <param name="continuation"></param>
        /// <returns></returns>
        public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(ClientInterceptorContext<TRequest, TResponse> context, AsyncDuplexStreamingCallContinuation<TRequest, TResponse> continuation)
        {
            return base.AsyncDuplexStreamingCall(ContextAddTracing(context), continuation);
        }

        /// <summary>
        /// Override of AsyncServerStreamingCall, add trace-key (from httpContext) to gRPC context 
        /// </summary>
        /// <typeparam name="TRequest"></typeparam>
        /// <typeparam name="TResponse"></typeparam>
        /// <param name="request"></param>
        /// <param name="context"></param>
        /// <param name="continuation"></param>
        /// <returns></returns>
        public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(TRequest request, ClientInterceptorContext<TRequest, TResponse> context, AsyncServerStreamingCallContinuation<TRequest, TResponse> continuation)
        {
            return base.AsyncServerStreamingCall(request, ContextAddTracing(context), continuation);
        }

        /// <summary>
        ///  Override of BlockingUnaryCall, add trace-key (from httpContext) to gRPC context 
        /// </summary>
        /// <typeparam name="TRequest"></typeparam>
        /// <typeparam name="TResponse"></typeparam>
        /// <param name="request"></param>
        /// <param name="context"></param>
        /// <param name="continuation"></param>
        /// <returns></returns>
        public override TResponse BlockingUnaryCall<TRequest, TResponse>(TRequest request, ClientInterceptorContext<TRequest, TResponse> context, BlockingUnaryCallContinuation<TRequest, TResponse> continuation)
        {
            return base.BlockingUnaryCall(request, ContextAddTracing(context), continuation);
        }
        #endregion

        #region Privates
        private ClientInterceptorContext<TRequest, TResponse> ContextAddTracing<TRequest, TResponse>(ClientInterceptorContext<TRequest, TResponse> context)
            where TRequest : class
            where TResponse : class
        {
            var traceId = _httpContextAccessor.HttpContext.GetTraceHeader(_traceHeaderName);

            if (string.IsNullOrWhiteSpace(traceId))
            {
                return context;
            }

            return new ClientInterceptorContext<TRequest, TResponse>(
                    context.Method,
                    context.Host,
                   new CallOptions(
                       headers: new Metadata { { _traceHeaderName, traceId.ToString() } },
                       deadline: context.Options.Deadline,
                       cancellationToken: context.Options.CancellationToken,
                       writeOptions: context.Options.WriteOptions,
                       propagationToken: context.Options.PropagationToken,
                       credentials: context.Options.Credentials)
                  );
        }
        #endregion
    }
}

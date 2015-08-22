#if NETFX_CORE
using System;
using System.Threading.Tasks;
using System.Threading;

namespace RestSharp
{
    public class RestRequestAsyncHandle
    {
        //  This object should be a CancellationTokenSource
        private object cancelToken;

        public CancellationTokenSource CancelToken 
        {
            get
            {
                return (CancellationTokenSource)cancelToken;
            }
        }

        public RestRequestAsyncHandle()
        {
            cancelToken = new CancellationTokenSource();
        }

        public RestRequestAsyncHandle(CancellationTokenSource token)
        {
            cancelToken = token;
        }

        public void Abort()
        {
            if(cancelToken != null)
            {
                ((CancellationTokenSource)cancelToken).Cancel();
            }
        }
    }
}
#endif
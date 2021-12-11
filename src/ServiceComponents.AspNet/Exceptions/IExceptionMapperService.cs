using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ServiceComponents.AspNet.Exceptions
{
    public interface IExceptionMapperService
    {
        Task WriteResponse(Exception exception, HttpResponse httpResponse);
        Task ThrowExceptionIfNeeded(HttpResponseMessage httpResponse);
    }
}
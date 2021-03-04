using System;

namespace ServiceComponents.Api.Mediator
{
    public abstract class Query<TResult> : IQuery<TResult>
    {
        public string QueryId { get; }

        protected Query(string queryId)
        {
            QueryId = queryId ?? Guid.NewGuid().ToString();
        }
    }
}
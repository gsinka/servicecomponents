//using System;
//using System.Threading;
//using System.Threading.Tasks;
//using NHibernate;
//using ReferenceApplication.Api;
//using ServiceComponents.Application;
//using ServiceComponents.Application.Mediator;
//using ServiceComponents.Application.Monitoring;
//using ServiceComponents.Core.Services;

//namespace ReferenceApplication.Application.TestProcess
//{
//    public class TestProcessManager : IPreHandleCommand<TestCommand>, IPreHandleEvent<TestEvent>, IPostHandleCommand<TestCommand2>
//    {
//        private readonly IMetricsService _metricsService;
//        private readonly ISession _session;
//        private readonly ICorrelation _correlation;
//        private readonly IClock _clock;

//        public TestProcessManager(IMetricsService metricsService, ISession session, ICorrelation correlation, IClock clock)
//        {
//            _metricsService = metricsService;
//            _session = session;
//            _correlation = correlation;
//            _clock = clock;
//        }

//        private Guid CorrelationId => Guid.TryParse(_correlation.CorrelationId, out var processId) ? processId : Guid.NewGuid();
       

//        public async Task PreHandleAsync(TestCommand command, CancellationToken cancellationToken = default)
//        {
//            using var tx = _session.BeginTransaction();
//            await _session.SaveAsync(new TestProcessEntity() { Id = CorrelationId, StartTime = _clock.UtcNow }, cancellationToken);
//            await tx.CommitAsync(cancellationToken);

//        }

//        public async Task PreHandleAsync(TestEvent @event, CancellationToken cancellationToken = default)
//        {
//            using var tx = _session.BeginTransaction();
//            var process = await _session.GetAsync<TestProcessEntity>(CorrelationId, cancellationToken);
//            process.EventTime = _clock.UtcNow;
//            await _session.UpdateAsync(process, cancellationToken);
//            await tx.CommitAsync(cancellationToken);

//            _metricsService.Observe(new MultiStepBusinessProcessMetric("test_process", "event_received"), (process.EventTime - process.StartTime).TotalMilliseconds);
//        }

//        public async Task PostHandleAsync(TestCommand2 command, CancellationToken cancellationToken = default)
//        {
//            var now = _clock.UtcNow;

//            using var tx = _session.BeginTransaction();
//            var process = await _session.GetAsync<TestProcessEntity>(CorrelationId, cancellationToken);
            
//            _metricsService.Observe(new MultiStepBusinessProcessMetric("test_process", "process_closed"), (now - process.EventTime).TotalMilliseconds);
//            _metricsService.Observe(new MultiStepBusinessProcessMetric("test_process", "process_overall"), (now - process.StartTime).TotalMilliseconds);

//            _metricsService.Observe(new BusinessProcessHistogram("test_process"), (now - process.StartTime).TotalMilliseconds);

//            await _session.DeleteAsync(process, cancellationToken);
//            await tx.CommitAsync(cancellationToken);
//        }
//    }
//}

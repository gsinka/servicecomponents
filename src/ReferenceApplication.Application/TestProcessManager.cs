using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ReferenceApplication.Api;
using ReferenceApplication.Application.Entities;
using ServiceComponents.Application;
using ServiceComponents.Application.Mediator;
using ServiceComponents.Application.Monitoring;
using ServiceComponents.Application.ProcessManagement;
using ServiceComponents.Core.Services;
using ServiceComponents.Infrastructure.Monitoring;

namespace ReferenceApplication.Application
{
    public class TestProcessManager : IPreHandleCommand<TestCommand>, IPreHandleEvent<TestEvent>, IPostHandleCommand<TestCommand2>
    {
        private readonly IMetricsService _metricsService;
        private readonly IProcessRepository<TestProcessEntity> _repository;
        private readonly ICorrelation _correlation;
        private readonly IClock _clock;

        public TestProcessManager(IMetricsService metricsService, IProcessRepository<TestProcessEntity> repository, ICorrelation correlation, IClock clock)
        {
            _metricsService = metricsService;
            _repository = repository;
            _correlation = correlation;
            _clock = clock;
        }

        private Guid CorrelationId => Guid.TryParse(_correlation.CorrelationId, out var processId) ? processId : Guid.NewGuid();
       

        public async Task PreHandleAsync(TestCommand command, CancellationToken cancellationToken = default)
        {
            await _repository.Save(new TestProcessEntity() { Id = CorrelationId, StartTime = _clock.UtcNow }, cancellationToken);
        }

        public async Task PreHandleAsync(TestEvent @event, CancellationToken cancellationToken = default)
        {
            var process = await _repository.GetByIdAsync(CorrelationId, cancellationToken);
            process.EventTime = _clock.UtcNow;
            await _repository.Save(process, cancellationToken);

            _metricsService.Observe(new MultiStepBusinessProcessMetric("test_process", "event_received"), (process.EventTime - process.StartTime).TotalMilliseconds);
        }

        public async Task PostHandleAsync(TestCommand2 command, CancellationToken cancellationToken = default)
        {
            var process = await _repository.GetByIdAsync(CorrelationId, cancellationToken);
            process.CloseTime = _clock.UtcNow;
            await _repository.Save(process, cancellationToken);

            _metricsService.Observe(new MultiStepBusinessProcessMetric("test_process", "process_closed"), (process.CloseTime - process.EventTime).TotalMilliseconds);
            _metricsService.Observe(new MultiStepBusinessProcessMetric("test_process", "process_overall"), (process.CloseTime - process.StartTime).TotalMilliseconds);
        }
    }


    [MetricDescription("business_process", "Business processes")]
    public class BusinessProcessMetric
    {
        public string ProcessName { get; }

        public BusinessProcessMetric(string processName)
        {
            ProcessName = processName;
        }
    }
    
    [MetricDescription("business_process", "Business processes")]
    public class MultiStepBusinessProcessMetric
    {
        [MetricField("process_name", "Process")]
        public string ProcessName { get; }
        
        [MetricField("step_name", "Process step" )]
        public string StepName { get; }

        public MultiStepBusinessProcessMetric(string processName, string stepName)
        {
            ProcessName = processName;
            StepName = stepName;
        }
    }
}

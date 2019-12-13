using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Transacto.Framework;

namespace Transacto.Testing {
    public class EventCentricTestSpecificationRunner : IEventCentricTestSpecificationRunner {
        private readonly IFactComparer _comparer;
        private readonly object _handler;
        private readonly IFactRecorder _factRecorder;

        public EventCentricTestSpecificationRunner(IFactComparer comparer, object handler, IFactRecorder factRecorder) {
            if (comparer == null) throw new ArgumentNullException(nameof(comparer));
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            if (factRecorder == null) throw new ArgumentNullException(nameof(factRecorder));
            _comparer = comparer;
            _factRecorder = factRecorder;
            _handler = handler;
        }

        public async Task<EventCentricTestResult> Run(EventCentricTestSpecification specification) {
            var methodInfo = _handler.GetType().GetMethods().Single(
                mi => {
                    var parameters = mi.GetParameters();
                    return mi.IsPublic &&
                           mi.ReturnType == typeof(ValueTask) &&
                           parameters.Length == 2 &&
                           parameters[0].ParameterType == specification.When.GetType() &&
                           parameters[1].ParameterType == typeof(CancellationToken);
                });

            ValueTask Handler(object o, CancellationToken ct) => (ValueTask)methodInfo.Invoke(_handler, new[] {o, ct})!;

            _factRecorder.Record(specification.Givens);

            Optional<Exception> optionalException = Optional<Exception>.Empty;

            try {
                await Handler(specification.When, CancellationToken.None);
            } catch (Exception ex) {
                optionalException = ex;
            }

            var then = await _factRecorder.GetFacts().Skip(specification.Givens.Length).ToArrayAsync();
            return new EventCentricTestResult(specification,
                !optionalException.HasValue &&
                specification.Thens.SequenceEqual(then, new FactEqualityComparer(_comparer))
                    ? TestResultState.Passed
                    : TestResultState.Failed,
                then, optionalException);
        }
    }
}

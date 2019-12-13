using System.Collections.Generic;
using Transacto.Framework;

namespace Transacto.Testing {
    public interface IFactRecorder {
        void Record(string identifier, IEnumerable<object> events);
        void Record(IEnumerable<Fact> facts);
        void Record(string identifier, AggregateRoot aggregate);
        IAsyncEnumerable<Fact> GetFacts();
    }
}

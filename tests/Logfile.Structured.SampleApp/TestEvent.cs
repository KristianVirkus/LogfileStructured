using Logfile.Core.Details;

namespace Logfile.Structured.SampleApp
{
    [ID(1)]
    enum TestEvent
    {
        [Parameters("P1", "P2")]
        One = 1,
        Two = 2,
    }
}

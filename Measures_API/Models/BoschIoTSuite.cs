using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Measures_API.Models
{
    public class BoschIoTSuiteTelmetry
    {
        public string Topic { get; set; }

        public string Path { get; set; }
        [BindRequired]
        public BoschIoTSuiteValue Value { get; set; }
    }

    public class BoschIoTSuiteValue
    {
        [BindRequired]
        public MeasureDTO Status { get; set; }
    }
}

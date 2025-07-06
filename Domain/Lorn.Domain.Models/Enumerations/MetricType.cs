namespace Lorn.Domain.Models.Enumerations
{
    /// <summary>
    /// Metric type enumeration
    /// </summary>
    public sealed class MetricType : Enumeration
    {
        /// <summary>
        /// Execution performance metrics
        /// </summary>
        public static readonly MetricType ExecutionPerformance = new(1, nameof(ExecutionPerformance), "milliseconds");

        /// <summary>
        /// Resource usage metrics
        /// </summary>
        public static readonly MetricType ResourceUsage = new(2, nameof(ResourceUsage), "bytes");

        /// <summary>
        /// Business metrics
        /// </summary>
        public static readonly MetricType BusinessMetric = new(3, nameof(BusinessMetric), "count");

        /// <summary>
        /// System health metrics
        /// </summary>
        public static readonly MetricType SystemHealth = new(4, nameof(SystemHealth), "percentage");

        /// <summary>
        /// User behavior metrics
        /// </summary>
        public static readonly MetricType UserBehavior = new(5, nameof(UserBehavior), "count");

        /// <summary>
        /// Gets the default unit for this metric type
        /// </summary>
        public string Unit { get; }

        /// <summary>
        /// Initializes a new instance of the MetricType class
        /// </summary>
        /// <param name="id">The unique identifier</param>
        /// <param name="name">The name</param>
        /// <param name="unit">The default unit</param>
        private MetricType(int id, string name, string unit) : base(id, name)
        {
            Unit = unit;
        }

        /// <summary>
        /// Gets the aggregation methods for this metric type
        /// </summary>
        /// <returns>List of aggregation methods</returns>
        public List<string> GetAggregationMethods()
        {
            return this switch
            {
                var t when t == ExecutionPerformance => new List<string> { "Average", "Median", "P95", "P99", "Min", "Max" },
                var t when t == ResourceUsage => new List<string> { "Average", "Max", "Sum" },
                var t when t == BusinessMetric => new List<string> { "Sum", "Count", "Average" },
                var t when t == SystemHealth => new List<string> { "Average", "Min", "Max" },
                var t when t == UserBehavior => new List<string> { "Count", "Sum", "Unique" },
                _ => new List<string> { "Average", "Count" }
            };
        }
    }
}

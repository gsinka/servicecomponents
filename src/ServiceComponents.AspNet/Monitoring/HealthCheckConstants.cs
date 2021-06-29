namespace ServiceComponents.AspNet.Monitoring
{
    public static class HealthCheckConstants
    {
        public const string LivenessTag = "Liveness";
        public const string ReadinessTag = "Readiness";
        public const string ReadinessPath = "/.well-known/ready";
        public const string LivenessPath = "/.well-known/live";
    }
   


}

namespace IBSMobile.Statics
{
    public static class ApiServerNames
    {
        public const string Saturn = "Saturn";
        public const string Neptune = "Neptune";

        public const string SaturnDisplay = "SaturnServer";
        public const string NeptuneDisplay = "NeptuneServer";

        public static bool IsValid(string? value)
        {
            return string.Equals(value, Saturn, StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, Neptune, StringComparison.OrdinalIgnoreCase);
        }

        public static string Normalize(string? value)
        {
            if (string.Equals(value, Neptune, StringComparison.OrdinalIgnoreCase))
                return Neptune;
            return Saturn;
        }
    }
}

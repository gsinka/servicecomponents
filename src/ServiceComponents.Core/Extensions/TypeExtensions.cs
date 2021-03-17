namespace ServiceComponents.Core.Extensions
{
    public static class TypeExtensions
    {
        public static string DisplayName(this object obj)
        {
            return obj.GetType().Name;
        }


        public static string AssemblyVersionlessQualifiedName(this object obj)
        {
            var type = obj.GetType();
            return $"{type.FullName}, {type.Assembly.GetName().Name}";
        }
    }
}

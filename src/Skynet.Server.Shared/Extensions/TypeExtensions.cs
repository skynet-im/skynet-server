using System;

namespace Skynet.Server.Extensions
{
    public static class TypeExtensions
    {
        public static Type GetGenericInterface(this Type givenType, Type genericType)
        {
            Type[] interfaceTypes = givenType.GetInterfaces();

            foreach (var it in interfaceTypes)
            {
                if (it.IsGenericType && it.GetGenericTypeDefinition() == genericType)
                    return it;
            }

            static Type GetBaseRecursive(Type givenType, Type genericType)
            {
                if (givenType.IsGenericType && givenType.GetGenericTypeDefinition() == genericType)
                    return givenType;

                Type baseType = givenType.BaseType;
                if (baseType == null) return null;

                return GetBaseRecursive(baseType, genericType);
            }

            return GetBaseRecursive(givenType, genericType);
        }
    }
}

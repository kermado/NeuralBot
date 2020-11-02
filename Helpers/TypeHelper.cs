using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AimBot.Helpers
{
    public static class TypeHelper
    {
        public static IEnumerable<Type> ConcreteTypes(Type baseType)
        {
            foreach (var concreteType in baseType.Assembly.GetTypes().Where(t => baseType.IsAssignableFrom(t) && t.IsAbstract == false && t.IsInterface == false))
            {
                yield return concreteType;
            }
        }

        public static IEnumerable<string> ConcreteTypeNames(Type baseType)
        {
            foreach (var concreteType in ConcreteTypes(baseType))
            {
                yield return concreteType.Name;
            }
        }

        public static IEnumerable<PropertyInfo> PublicProperties(object obj)
        {
            if (obj != null)
            {
                foreach (var prop in obj.GetType().GetProperties())
                {
                    yield return prop;
                }
            }
            else
            {
                yield break;
            }
        }
    }
}

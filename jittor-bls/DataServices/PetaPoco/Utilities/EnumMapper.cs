using System;
using System.Collections.Generic;

namespace PetaPoco.Internal
{
    internal static class EnumMapper
    {
        private static readonly Cache<Type, Dictionary<string, object>> _types = new Cache<Type, Dictionary<string, object>>();

        public static object EnumFromString(Type enumType, string value)
        {
            Dictionary<string, object> map = _types.Get(enumType, () =>
            {
                Array values = Enum.GetValues(enumType);

                Dictionary<string, object> newmap = new Dictionary<string, object>(values.Length, StringComparer.InvariantCultureIgnoreCase);

                foreach (object v in values)
                {
                    newmap.Add(v.ToString(), v);
                }

                return newmap;
            });

            try
            {
                return map[value];
            }
            catch (KeyNotFoundException inner)
            {
                throw new KeyNotFoundException(
                    $"Requested value '{value}' was not found in enum {enumType.Name}.",
                    inner);
            }
        }
    }
}
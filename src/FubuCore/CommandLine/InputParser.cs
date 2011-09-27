﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using FubuCore.Reflection;

namespace FubuCore.CommandLine
{
    public static class InputParser
    {
        public static readonly string FLAG_PREFIX = "-";
        public static readonly string FLAG_SUFFIX = "Flag";
        private static readonly ObjectConverter _converter = new ObjectConverter();


        public static List<ITokenHandler> GetHandlers(Type inputType)
        {
            return inputType.GetProperties()
                .Where(prop => prop.CanWrite)
                .Where(prop => !prop.HasAttribute<IgnoreOnCommandLineAttribute>())
                .Select(BuildHandler).ToList();
        }

        private static List<Tuple<Predicate<PropertyInfo>, Func<PropertyInfo, ITokenHandler>>> _handlers =
            new List<Tuple<Predicate<PropertyInfo>, Func<PropertyInfo, ITokenHandler>>>(); 
        public static void AddBuildHandler(Predicate<PropertyInfo> shouldHandleProperty, Func<PropertyInfo, ITokenHandler> tokenHandler)
        {
            var item = new Tuple<Predicate<PropertyInfo>, Func<PropertyInfo, ITokenHandler>>(shouldHandleProperty,
                                                                                             tokenHandler);
            _handlers.Add(item);
        }

        public static ITokenHandler BuildHandler(PropertyInfo property)
        {

            foreach (var handler in _handlers)
            {
                if (handler.Item1(property))
                    return handler.Item2(property);
            }

            if (property.PropertyType != typeof(string) && property.PropertyType.Closes(typeof(IEnumerable<>)))
            {
                return new EnumerableArgument(property, _converter);
            }

            if (!property.Name.EndsWith(FLAG_SUFFIX))
            {
                return new Argument(property, _converter);
            }

            if (property.PropertyType == typeof(bool))
            {
                return new BooleanFlag(property);
            }



            return new Flag(property, _converter);
        }

        public static string ToFlagName(PropertyInfo property)
        {

            var name = property.Name;
            if (name.EndsWith("Flag"))
            {
                name = name.Substring(0, property.Name.Length - 4);
            }

            property.ForAttribute<FlagAliasAttribute>(att => name = att.Alias);
            return FLAG_PREFIX + name.ToLower();
        }

    }
}
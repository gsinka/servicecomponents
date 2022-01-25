using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ServiceComponents.Core
{
    public abstract class Enumeration : IComparable
    {
        public string Name { get; }

        public int Id { get; }

        protected Enumeration(int id, string name)
        {
            Id = id;
            Name = name;
        }

        override public string ToString()
        {
            return Name;
        }

        public static IEnumerable<T> GetAll<T>() where T : Enumeration
        {
            var fields = typeof(T).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
            return fields.Select(f => f.GetValue(null)).Cast<T>();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Enumeration otherValue)) return false;

            var typeMatches = GetType() == obj.GetType();
            var valueMatches = Id.Equals(otherValue.Id);

            return typeMatches && valueMatches;
        }

        public static T FromId<T>(int id) where T : Enumeration, new()
    {
        var matchingItem = parse<T, int>(id, "ID", item => item.Id == id);
        return matchingItem;
    }

    public static T FromName<T>(string name) where T : Enumeration, new()
    {
        var matchingItem = parse<T, string>(name, "name", item => item.Name == name);
        return matchingItem;
    }

    private static T parse<T, K>(K value, string description, Func<T, bool> predicate) where T : Enumeration, new()
    {
        var matchingItem = GetAll<T>().FirstOrDefault(predicate);

        if (matchingItem == null)
        {
            var message = string.Format("'{0}' is not a valid {1} in {2}", value, description, typeof(T));
            throw new ApplicationException(message);
        }

        return matchingItem;
    }


        public override int GetHashCode()
        {
            return Id;
        }

        public int CompareTo(object other) => Id.CompareTo(((Enumeration)other).Id);
    }
}

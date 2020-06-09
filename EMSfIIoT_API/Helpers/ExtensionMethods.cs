using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EMSfIIoT_API.Entities;

namespace EMSfIIoT_API.Helpers
{
    public static class ExtensionMethods
    {
        public static IEnumerable<User> WithoutPasswords(this IEnumerable<User> users)
        {
            return users.Select(x => x.WithoutPassword());
        }

        public static User WithoutPassword(this User user)
        {
            return new User
            {
                Username = user.Username
            };
        }
    }

    /// <summary>
    /// This attribute is used to represent a string value
    /// for a value in an enum.
    /// </summary>
    public class StringValueAttribute : Attribute
    {
        #region Properties
        /// <summary>
        /// Holds the stringvalue for a value in an enum.
        /// </summary>
        public string StringValue { get; protected set; }
        #endregion

        #region Constructor
        /// <summary>
        /// Constructor used to init a StringValue Attribute
        /// </summary>
        /// <param name="value"></param>
        public StringValueAttribute(string value)
        {
            this.StringValue = value;
        }
        #endregion
    }

    /// <summary>
    /// This attribute is used to represent a string value
    /// for a value in an enum.
    /// </summary>
    public class CronValueAttribute : Attribute
    {
        #region Properties
        /// <summary>
        /// Holds the stringvalue for a value in an enum.
        /// </summary>
        public string CronValue { get; protected set; }
        #endregion

        #region Constructor
        /// <summary>
        /// Constructor used to init a StringValue Attribute
        /// </summary>
        /// <param name="value"></param>
        public CronValueAttribute(string value)
        {
            this.CronValue = value;
        }
        #endregion
    }

    public static class EnumExt
    {
        /// <summary>
        /// Will get the string value for a given enums value, this will
        /// only work if you assign the StringValue attribute to
        /// the items in your enum.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string GetStringValue(this Enum value)
        {
            // Get the type
            Type type = value.GetType();

            // Get fieldinfo for this type
            FieldInfo fieldInfo = type.GetField(value.ToString());

            // Get the stringvalue attributes
            StringValueAttribute[] attribs = fieldInfo.GetCustomAttributes(
                typeof(StringValueAttribute), false) as StringValueAttribute[];

            // Return the first if there was a match.
            return attribs.Length > 0 ? attribs[0].StringValue : value.ToString();
        }

        /// <summary>
        /// Will get the string value for a given enums value, this will
        /// only work if you assign the StringValue attribute to
        /// the items in your enum.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string[] GetStringValues(this Type type)
        {
            if (type == null) throw new NullReferenceException();
            if (!type.IsEnum) throw new InvalidCastException("object is not an Enumeration");

            FieldInfo[] fields = type.GetFields();
            List<string> result = new List<string>();

            // Get the stringvalue attributes
            fields.ToList().ForEach(field =>
            {
                // Get the stringvalue attributes
                StringValueAttribute[] attribs = field.GetCustomAttributes(
                    typeof(StringValueAttribute), false) as StringValueAttribute[];

                // Return the first if there was a match.
                if (attribs.Length > 0)
                {
                    result.Add(attribs[0].StringValue);
                }
            });

            return result.ToArray();
        }

        /// <summary>
        /// Will get the string value for a given enums value, this will
        /// only work if you assign the StringValue attribute to
        /// the items in your enum.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string GetCronValue(this Enum value)
        {
            // Get the type
            Type type = value.GetType();

            // Get fieldinfo for this type
            FieldInfo fieldInfo = type.GetField(value.ToString());

            // Get the stringvalue attributes
            CronValueAttribute[] attribs = fieldInfo.GetCustomAttributes(
                typeof(CronValueAttribute), false) as CronValueAttribute[];

            // Return the first if there was a match.
            return attribs.Length > 0 ? attribs[0].CronValue : value.ToString();
        }

        /// <summary>
        /// Will get the string value for a given enums value, this will
        /// only work if you assign the StringValue attribute to
        /// the items in your enum.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string[] GetCronValues(this Type type)
        {
            if (type == null) throw new NullReferenceException();
            if (!type.IsEnum) throw new InvalidCastException("object is not an Enumeration");

            FieldInfo[] fields = type.GetFields();
            List<string> result = new List<string>();

            // Get the stringvalue attributes
            fields.ToList().ForEach(field =>
            {
                // Get the stringvalue attributes
                CronValueAttribute[] attribs = field.GetCustomAttributes(
                    typeof(CronValueAttribute), false) as CronValueAttribute[];

                // Return the first if there was a match.
                if (attribs.Length > 0)
                {
                    result.Add(attribs[0].CronValue);
                }
            });

            return result.ToArray();
        }
    }
}
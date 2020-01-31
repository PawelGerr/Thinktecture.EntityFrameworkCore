#if !NETSTANDARD2_1

#pragma warning disable 1591
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable CheckNamespace

namespace System.Diagnostics.CodeAnalysis
{
   [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
   internal sealed class AllowNullAttribute : Attribute
   {
   }

   [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
   internal sealed class MaybeNullAttribute : Attribute
   {
   }

   [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
   internal sealed class DisallowNullAttribute : Attribute
   {
   }

   [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
   internal sealed class NotNullAttribute : Attribute
   {
   }

   [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = true)]
   internal sealed class NotNullIfNotNullAttribute : Attribute
   {
      public string ParameterName { get; }

      public NotNullIfNotNullAttribute(string parameterName)
      {
         ParameterName = parameterName;
      }
   }
}
#endif

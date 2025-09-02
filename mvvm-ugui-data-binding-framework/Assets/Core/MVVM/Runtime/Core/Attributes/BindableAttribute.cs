using System;

namespace MVVM.Runtime.Core
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class BindableAttribute : Attribute { }
}
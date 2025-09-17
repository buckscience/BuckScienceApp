using System;

namespace BuckScience.Web.Security;

// Put this on endpoints that must bypass the onboarding flow enforcement
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class SkipSetupCheckAttribute : Attribute { }
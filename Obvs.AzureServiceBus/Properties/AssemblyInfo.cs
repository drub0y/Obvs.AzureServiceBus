using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: AssemblyTitle("Obvs.AzureServiceBus")]
[assembly: AssemblyDescription("Azure ServiceBus support for the Obvs framework.")]
#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif
[assembly: AssemblyCompany("Drew Marsh")]
[assembly: AssemblyProduct("Obvs.AzureServiceBus")]
[assembly: AssemblyCopyright("Copyright © Drew Marsh 2015")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

[assembly: AssemblyVersion("0.15.1.*")]
[assembly: AssemblyInformationalVersion("0.15.1-beta2")]

[assembly: InternalsVisibleTo("Obvs.AzureServiceBus.Tests")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
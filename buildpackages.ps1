Param(
	[string]$Configuration="Release"
)

$ErrorActionPreference="Stop"

Set-Alias Build-Pkg .\packages\NuGet.CommandLine.1.5.20905.5\tools\NuGet.exe

$scriptRoot = Split-Path (Resolve-Path $myInvocation.MyCommand.Path) 
$scriptRoot = $scriptRoot + "\"

function GetAssemblyVersion($assembly)
{   
   $assembly = $scriptRoot + $assembly
   write-host $assembly
   $Myasm = [System.Reflection.Assembly]::Loadfile($assembly)   
   $Aname = $Myasm.GetName()
   $Aver =  $Aname.version
   return $Aver
}

$CoreVersion = GetAssemblyVersion("System\System\Bin\" + $Configuration + "\Composable.Core.dll")
$WindsorVersion = GetAssemblyVersion("CQRS\CQRS.Windsor\Bin\" + $Configuration + "\Composable.CQRS.Windsor.dll")
$NServiceBusVersion = GetAssemblyVersion("CQRS\Composable.CQRS.ServiceBus.NServiceBus\Bin\" + $Configuration + "\Composable.CQRS.ServiceBus.NServiceBus.dll")
$CqrsVersion = GetAssemblyVersion("CQRS\CQRS\Bin\" + $Configuration + "\Composable.CQRS.dll")
$DomainEventsVersion = GetAssemblyVersion("Composable.DomainEvents\Bin\" + $Configuration + "\Composable.DomainEvents.dll")
$AutoMapperVersion = GetAssemblyVersion("AutoMapper\Composable.AutoMapper\Bin\" + $Configuration + "\Composable.AutoMapper.dll")


Build-Pkg pack ".\System\System\Composable.Core.csproj" -Symbols -OutputDirectory "..\NuGetFeed" -Prop Configuration=$Configuration
Build-Pkg pack ".\CQRS\CQRS\Composable.CQRS.csproj" -Symbols -OutputDirectory "..\NuGetFeed" -Prop Configuration=$Configuration -Prop CoreVersion=$CoreVersion -Prop CqrsVersion=$CqrsVersion -Prop WindsorVersion=$WindsorVersion -Prop DomainEventsVersion=$DomainEventsVersion -Prop NServiceBusVersion=$NServiceBusVersion -Prop AutoMapperVersion=$AutoMapperVersion
Build-Pkg pack ".\CQRS\NHibernateRepositories\Composable.CQRS.NHibernate.csproj" -Symbols -OutputDirectory "..\NuGetFeed" -Prop Configuration=$Configuration -Prop CoreVersion=$CoreVersion -Prop CqrsVersion=$CqrsVersion -Prop WindsorVersion=$WindsorVersion -Prop DomainEventsVersion=$DomainEventsVersion -Prop NServiceBusVersion=$NServiceBusVersion -Prop AutoMapperVersion=$AutoMapperVersion
Build-Pkg pack ".\CQRS\Composable.CQRS.ServiceBus.NServiceBus\Composable.CQRS.ServiceBus.NServiceBus.csproj" -Symbols -OutputDirectory "..\NuGetFeed" -Prop Configuration=$Configuration -Prop CoreVersion=$CoreVersion -Prop CqrsVersion=$CqrsVersion -Prop WindsorVersion=$WindsorVersion -Prop DomainEventsVersion=$DomainEventsVersion -Prop NServiceBusVersion=$NServiceBusVersion -Prop AutoMapperVersion=$AutoMapperVersion
Build-Pkg pack ".\Composable.DomainEvents\Composable.DomainEvents.csproj" -Symbols -OutputDirectory "..\NuGetFeed" -Prop Configuration=$Configuration -Prop CoreVersion=$CoreVersion -Prop CqrsVersion=$CqrsVersion -Prop WindsorVersion=$WindsorVersion -Prop DomainEventsVersion=$DomainEventsVersion -Prop NServiceBusVersion=$NServiceBusVersion -Prop AutoMapperVersion=$AutoMapperVersion
Build-Pkg pack ".\AutoMapper\Composable.AutoMapper\Composable.AutoMapper.csproj" -Symbols -OutputDirectory "..\NuGetFeed" -Prop Configuration=$Configuration -Prop CoreVersion=$CoreVersion -Prop CqrsVersion=$CqrsVersion -Prop WindsorVersion=$WindsorVersion -Prop DomainEventsVersion=$DomainEventsVersion -Prop NServiceBusVersion=$NServiceBusVersion -Prop AutoMapperVersion=$AutoMapperVersion
Build-Pkg pack ".\CQRS\CQRS.Windsor\Composable.CQRS.Windsor.csproj" -Symbols -OutputDirectory "..\NuGetFeed" -Prop Configuration=$Configuration -Prop CoreVersion=$CoreVersion -Prop CqrsVersion=$CqrsVersion -Prop WindsorVersion=$WindsorVersion -Prop DomainEventsVersion=$DomainEventsVersion -Prop NServiceBusVersion=$NServiceBusVersion -Prop AutoMapperVersion=$AutoMapperVersion
Build-Pkg pack ".\CQRS\Testing\Composable.CQRS.Testing\Composable.CQRS.Testing.csproj" -Symbols -OutputDirectory "..\NuGetFeed" -Prop Configuration=$Configuration -Prop CoreVersion=$CoreVersion -Prop CqrsVersion=$CqrsVersion -Prop WindsorVersion=$WindsorVersion -Prop DomainEventsVersion=$DomainEventsVersion -Prop NServiceBusVersion=$NServiceBusVersion -Prop AutoMapperVersion=$AutoMapperVersion
Build-Pkg pack ".\CQRS\Composable.CQRS.Population.Client\Composable.CQRS.Population.Client.csproj" -Symbols -OutputDirectory "..\NuGetFeed" -Prop Configuration=$Configuration -Prop CoreVersion=$CoreVersion -Prop CqrsVersion=$CqrsVersion -Prop WindsorVersion=$WindsorVersion -Prop DomainEventsVersion=$DomainEventsVersion -Prop NServiceBusVersion=$NServiceBusVersion -Prop AutoMapperVersion=$AutoMapperVersion
Build-Pkg pack ".\CQRS\Composable.CQRS.Population.Server\Composable.CQRS.Population.Server.csproj" -Symbols -OutputDirectory "..\NuGetFeed" -Prop Configuration=$Configuration -Prop CoreVersion=$CoreVersion -Prop CqrsVersion=$CqrsVersion -Prop WindsorVersion=$WindsorVersion -Prop DomainEventsVersion=$DomainEventsVersion -Prop NServiceBusVersion=$NServiceBusVersion -Prop AutoMapperVersion=$AutoMapperVersion


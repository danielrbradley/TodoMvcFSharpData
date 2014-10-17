namespace System
open System.Reflection

[<assembly: AssemblyTitleAttribute("TodoMvcBackend")>]
[<assembly: AssemblyProductAttribute("TodoMvcBackend")>]
[<assembly: AssemblyDescriptionAttribute("Implementation of the TODO MVC Backend using F#, F# Data, OWIN and WebAPI.")>]
[<assembly: AssemblyVersionAttribute("1.0")>]
[<assembly: AssemblyFileVersionAttribute("1.0")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "1.0"

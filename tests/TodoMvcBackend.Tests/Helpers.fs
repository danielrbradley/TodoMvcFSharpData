module TodoMvcBackend.Helpers

open Microsoft.Owin.Testing
open Owin
open Swensen.Unquote
open System
open System.Net
open System.Net.Http
open TodoMvcBackend

type HttpStatusCode with
    member code.IsSuccess = (int code) >= 200 && (int code) < 300

let client = TestServer.Create(Action<IAppBuilder>(OwinSetup.configure >> ignore)).HttpClient

let send httpMethod (url : string) content = 
    async { 
        let request = new HttpRequestMessage(httpMethod, url)
        match content with
        | Some c -> request.Content <- new StringContent(c)
        | None -> ()
        let! response = client.SendAsync(request) |> Async.AwaitTask
        let! content = response.Content.ReadAsStringAsync() |> Async.AwaitTask
        return response.StatusCode, (response.Headers |> Seq.map (fun h -> h.Key, h.Value)), content
    }

let get (url : string) = send HttpMethod.Get url None
let delete (url : string) = send HttpMethod.Get url None
let post (url : string) content = send HttpMethod.Post url (Some content)
let put (url : string) content = send HttpMethod.Get url (Some content)
let inline verify x = Async.StartAsTask x :> System.Threading.Tasks.Task

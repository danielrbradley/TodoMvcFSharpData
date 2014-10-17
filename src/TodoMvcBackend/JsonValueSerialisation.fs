namespace TodoMvcBackend

open FSharp.Data
open FSharp.Data.Runtime
open System
open System.Collections

module JsonValueSerialisation = 
    let isJsonDocument (t : System.Type) = typeof<IJsonDocument>.IsAssignableFrom(t)
    
    let isSeq (t : System.Type) = 
        let genericSeqType = typedefof<_ seq>
        (t.IsGenericType && t.GetGenericTypeDefinition() = genericSeqType) 
        || (t.GetInterfaces() 
            |> Array.exists (fun i -> i.IsGenericType && i.GetGenericTypeDefinition() = genericSeqType))
    
    let isJsonDocumentSeq (t : System.Type) = 
        if not (t |> isSeq) then false
        else 
            let args = t.GetGenericArguments()
            args.Length = 1 && isJsonDocument args.[0]
    
    let isJsonValue t = t = typeof<JsonValue>
    
    let isJsonValueSeq (t : System.Type) = 
        if not (t |> isSeq) then false
        else 
            let args = t.GetGenericArguments()
            args.Length = 1 && isJsonValue args.[0]
    
    let isJson (t : Type) = isJsonDocument t || isJsonDocumentSeq t || isJsonValue t || isJsonValueSeq t
    
    let (|JsonDocument|_|) (v : obj) = 
        if v.GetType() |> isJsonDocument then (v :?> IJsonDocument).JsonValue |> Some
        else None
    
    let (|JsonDocumentSeq|_|) (v : obj) = 
        if v.GetType() |> isJsonDocumentSeq then 
            (v :?> IEnumerable)
            |> Seq.cast<IJsonDocument>
            |> Seq.map (fun d -> d.JsonValue)
            |> Seq.toArray
            |> JsonValue.Array
            |> Some
        else None
    
    let (|JsonValue|_|) (v : obj) = 
        if v.GetType() |> isJsonValue then (v :?> JsonValue) |> Some
        else None
    
    let (|JsonValueSeq|_|) (v : obj) = 
        if v.GetType() |> isJsonValueSeq then 
            (v :?> IEnumerable)
            |> Seq.cast<IJsonDocument>
            |> Seq.map (fun d -> d.JsonValue)
            |> Seq.toArray
            |> JsonValue.Array
            |> Some
        else None
    
    let rec getJsonValue v = 
        match v with
        | JsonDocument j | JsonDocumentSeq j | JsonValue j | JsonValueSeq j -> j
        | _ -> failwith "Not a valid document"

open JsonValueSerialisation

type JsonValueFormatter() as formatter = 
    inherit System.Net.Http.Formatting.MediaTypeFormatter()
    do formatter.SupportedMediaTypes.Add(new System.Net.Http.Headers.MediaTypeHeaderValue("application/json"))
    override x.CanReadType t = isJsonDocument t
    override x.CanWriteType t = isJson t
    
    override x.ReadFromStreamAsync(t, stream, contentHeaders, formatterContext) = 
        async { 
            let jsonValue = JsonValue.Load(stream)
            // HACK: can't seem to get around the compiller warning - so calling via reflection.
            let jsonDocument = typeof<JsonDocument>
            
            let create = 
                jsonDocument.GetMethod("Create", 
                                       [| typeof<JsonValue>
                                          typeof<string> |])
            
            let invoked = create.Invoke(null, [| jsonValue; "" |])
            return invoked
        }
        |> Async.StartAsTask
    
    override x.WriteToStreamAsync(t, value, stream, content, transport) = 
        async { 
            let jsonValue = getJsonValue value
            let writer = new System.IO.StreamWriter(stream)
            jsonValue.WriteTo(writer, JsonSaveOptions.DisableFormatting)
            writer.Flush()
            stream.Flush()
        }
        |> Async.StartAsTask :> System.Threading.Tasks.Task

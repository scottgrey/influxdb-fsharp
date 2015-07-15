﻿namespace InfluxDB.FSharp

[<Sealed>]
type AsyncChoiceBuilder () =
    member __.Return value : Async<Choice<'T, 'Error>> =
        async.Return (Choice1Of2 value)

    member __.ReturnFrom (asyncChoice : Async<Choice<'T, 'Error>>) =
        asyncChoice

    member inline __.ReturnFrom (choice : Choice<'T, 'Error>) : Async<Choice<'T, 'Error>> =
        async.Return choice

    member inline this.Zero () : Async<Choice<unit, 'Error>> =
        this.Return ()

    member inline __.Delay (generator : unit -> Async<Choice<'T, 'Error>>) : Async<Choice<'T, 'Error>> =
        async.Delay generator

    member __.Combine (r1, r2) : Async<Choice<'T, 'Error>> =
        async {
            let! r1' = r1
            match r1' with
            | Choice2Of2 error ->
                return Choice2Of2 error
            | Choice1Of2 () ->
                return! r2
        }

    member __.Bind (value : Async<Choice<'T, 'Error>>, binder : 'T -> Async<Choice<'U, 'Error>>) : Async<Choice<'U, 'Error>> =
        async {
            let! value' = value
            match value' with
            | Choice2Of2 error ->
                return Choice2Of2 error
            | Choice1Of2 x ->
                return! binder x
        }

    member inline __.Bind (value : Choice<'T, 'Error>, binder : 'T -> Async<Choice<'U, 'Error>>) : Async<Choice<'U, 'Error>> =
        async {
            match value with
            | Choice1Of2 x -> return! binder x
            | Choice2Of2 error -> return Choice2Of2 error
        }

    member inline __.TryWith (computation : Async<Choice<'T, 'Error>>, catchHandler : exn -> Async<Choice<'T, 'Error>>)
        : Async<Choice<'T, 'Error>> =
        async.TryWith(computation, catchHandler)

    member inline __.TryFinally (computation : Async<Choice<'T, 'Error>>, compensation : unit -> unit)
        : Async<Choice<'T, 'Error>> =
        async.TryFinally (computation, compensation)

    member inline __.Using (resource : ('T :> System.IDisposable), binder : _ -> Async<Choice<'U, 'Error>>)
        : Async<Choice<'U, 'Error>> =
        async.Using (resource, binder)

    member this.While (guard, body : Async<Choice<unit, 'Error>>) : Async<Choice<_,_>> =
        if guard () then
            this.Bind (body, (fun () -> this.While (guard, body)))
        else
            this.Zero ()

    member this.For (sequence : seq<_>, body : 'T -> Async<Choice<unit, 'Error>>) =
        this.Using (sequence.GetEnumerator (), fun enum ->
            this.While (
                enum.MoveNext,
                this.Delay (fun () ->
                    body enum.Current)))

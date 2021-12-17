open BenchmarkDotNet.Running
open BenchmarkDotNet.Attributes

// [<MemoryDiagnoser>]
type Benchmarks () =

    let rng = System.Random 123
    let minLength = 10
    let maxLength = 10_000
    let arrayCount = 100_000
    let arrays =
        [|1 .. arrayCount|]
        |> Array.map (fun _ ->
            let length = rng.Next (minLength, maxLength)
            [| 1 .. length |]
            |> Array.map (fun _ -> rng.Next ())
        )


    let mutationFreeMax (x: array<int>) =
        // I am must going to assume x is non-null and not empty

        let rec loop (acc: int) (idx: int) (array: array<int>) =
            if idx < array.Length then
                let curr = array.[idx]
                if curr > acc then
                    loop curr (idx + 1) array
                else
                    loop acc (idx + 1) array
            else
                acc

        loop x.[0] 1 x


    let mutationMax (x: array<int>) =
        // I am must going to assume x is non-null and not empty
        let mutable acc = x.[0]
        let mutable idx = 1

        while idx < x.Length do
            let curr = x.[idx]
            if curr > acc then
                acc <- curr
            idx <- idx + 1

        acc


    let rec sort (values: list<int>) =
        match values with
        | [] -> values
        | [x] -> values
        | head::tail ->
            let less, greater = List.partition ((>=) head) tail
            List.concat [sort(less); [head]; sort(greater)]

    let sortValues =
        [for _ in 0 .. 1_000 -> rng.Next ()]

    [<Benchmark>]
    member _.ImmutableQuicksort () =
        sort sortValues

    [<Benchmark>]
    member _.BuiltInSort () =
        List.sort sortValues

    [<Benchmark>]
    member _.MutationFree () =
        arrays
        |> Array.map mutationFreeMax

    [<Benchmark>]
    member _.MutationBased () =
        arrays
        |> Array.map mutationMax


[<EntryPoint>]
let main args =

    let summary = BenchmarkRunner.Run<Benchmarks>()
    
    0

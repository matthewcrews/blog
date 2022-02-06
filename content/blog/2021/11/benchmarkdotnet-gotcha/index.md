---
title: BenchmarkDotNet Gotcha with F#
date: 2021-11-16
draft: false
tag: fsharp, high-performance
---

I have been working on creating some types which allow me to wrap an array and index it with an `int` which has a Unit of Measure (UoM). Right now, if you want to index into an array with an `int` that has a UoM, you need to remove the units.

```fsharp
[<Measure>] type ItemIdx

let a = [|1.0 .. 10.0|]
let idx = 1<ItemIdx>

let x = a[idx] // This will raise an error saying that the type `int<ItemIdx>` is not correct

let y = a[int idx] // This will work because the units are removed when calling `int`
```

You may think, "Matthew, that call to `int` is going to cause a problem, isn't it?" That's a great question. Let's put together an experiment and see what assembly is generated. This code...

```fsharp
let test (a: array<float>) =
    let x = 1
    a[x]

```

Will generate this assembly.

```assembly
; Core CLR 6.0.21.52210 on amd64

_.test(System.Double[])
    L0000: sub rsp, 0x28
    L0004: vzeroupper
    L0007: cmp dword ptr [rcx+8], 1
    L000b: jbe short L0017
    L000d: vmovsd xmm0, [rcx+0x18]
    L0012: add rsp, 0x28
    L0016: ret
    L0017: call 0x00007ffd109ee750
    L001c: int3
```

Let's add a UoM to the index and see what happens when we use it to index into the array while using `int` to remove the units.

```fsharp
[<Measure>] type ItemIdx

let test (a: array<float>) =
    let x = 1<ItemIdx>
    a[int x] // Does calling `int` here incur a performance penalty?
```

And here is the assembly...

```assembly
; Core CLR 6.0.21.52210 on amd64

_.test(System.Double[])
    L0000: sub rsp, 0x28
    L0004: vzeroupper
    L0007: cmp dword ptr [rcx+8], 1
    L000b: jbe short L0017
    L000d: vmovsd xmm0, [rcx+0x18]
    L0012: add rsp, 0x28
    L0016: ret
    L0017: call 0x00007ffd109ee750
    L001c: int3
```

You should notice that we are getting the exact same result. The F# compiler is smart enough to see that we are calling the `int` conversion function on a type that is already an `int` so it removes it.

This kind of thing is annoying to have to do manually all the time and I really wanted a wrapper around an `array` which had a UoM type associated with the index. I decided to code up something simple.

```fsharp
type ClassWrapper<[<Measure>] 'Measure, 'Value>(values: array<'Value>) =

    member _.Values = values

    member this.Item
        with get (idx: int<'Measure>) =
            this.Values.[int idx]

    member this.Length = 
        LanguagePrimitives.Int32WithMeasure<'Measure> this.Values.Length
```

We now have a class which is taking an `array<'Value>` as part of its constructor and it is giving us a view of the underlying `array` which is forcing the use of an `int` with a UoM to retrieve values. This allows us to do the following.

```fsharp
[<Measure>] type ItemIdx

let classWrapper =
   [|1.0 .. (float numberCount)|] 
   |> ClassWrapper<ItemIdx, float>
```

`ClassWrapper` will now force us to use an `int<ItemIdx>` to retrieve values. You may think this is cumbersome but if you are working with many arrays simultaneously it can be easy to mix up which index is meant to be associated with which array. I like the compiler to be able to help me out so the idea of using UoM as a way provide some guarantees is nice provided there is not a speed penalty.

I also thought, "You know, why not use a Struct instead of a Class to wrap the value? Using a Struct means the reference to the array will be on the stack, right? That should save you chasing a reference before getting to the array." Rather than assuming that was the case I decided to put together a test using [BenchmarkDotNet]( https://github.com/dotnet/BenchmarkDotNet) to verify my assumption was correct.

## The Setup

The first thing I need to do is define a Struct for wrapping my `array`.

```fsharp
[<Struct>]
type StructWrapper<[<Measure>] 'Measure, 'Value> =
    val Values : array<'Value>

    new (values: array<'Value>) =
        {
            Values = values
        }

    member inline this.Item
        with inline get (idx: int<'Measure>) =
            this.Values[int idx]

    member this.Length = 
        LanguagePrimitives.Int32WithMeasure<'Measure> this.Values.Length
```

Then I setup some test data. I typically am working with small arrays so I'm just going to be summing up the values from `1.0` to `100.0` and I'll perform that `100,000` times. I create my three different types for my testing.

```fsharp
let iterations = 100_000
let numberCount = 100

let rawArray = [|1.0 .. (float numberCount)|]

let classWrapper =
   [|1.0 .. (float numberCount)|] 
   |> ClassWrapper<ItemIdx, float>

let structWrapper =
    [|1.0 .. (float numberCount)|] 
    |> StructWrapper<ItemIdx, _>
```

Alright, data prepared, time to create some tests. I open the namespaces I need from BenchmarkDotNet and create my `Benchmark` class. I create three tests to see which approach is faster. I’m assuming that the raw array is the absolute limit (short of SIMD).

```fsharp
type Benchmarks () =

    [<Benchmark>]
    member _.RawArray () =
        let mutable iterationIdx = 0
        let mutable result = 0.0

        while iterationIdx < iterations do
            let mutable idx = 0
            let len = rawArray.Length
            while idx < len do
                result <- result + rawArray[idx]
                idx <- idx + 1
                
            result <- 0.0 // Reset
            iterationIdx <- iterationIdx + 1

        result


    [<Benchmark>]
    member _.ClassWrapper () =
        let mutable iterationIdx = 0
        let mutable result = 0.0

        while iterationIdx < iterations do
            let mutable idx = 0<ItemIdx>
            let len = classWrapper.Length
            while idx < len do
                result <- result + classWrapper[idx]
                idx <- idx + 1<ItemIdx>
                
            result <- 0.0 // Reset
            iterationIdx <- iterationIdx + 1

        result


    [<Benchmark>]
    member _.StructWrapper () =
        let mutable iterationIdx = 0
        let mutable result = 0.0

        while iterationIdx < iterations do
            let mutable idx = 0<ItemIdx>
            let len = structWrapper.Length
            while idx < len do
                result <- result + structWrapper[idx]
                idx <- idx + 1<ItemIdx>
                
            result <- 0.0 // Reset
            iterationIdx <- iterationIdx + 1

        result
```

I run the benchmarks and get an unexpected result.

|        Method |      Mean |     Error |    StdDev |
|-------------- |----------:|----------:|----------:|
|      RawArray |  6.434 ms | 0.1241 ms | 0.1100 ms |
|  ClassWrapper |  6.969 ms | 0.1177 ms | 0.1101 ms |
| StructWrapper | 23.983 ms | 0.2800 ms | 0.2619 ms |

I am shocked that the `StructWrapper` performed so much more poorly that either the `RawArray` or `ClassWrapper`. This does not make any sense to me. If anything, `StructWrapper` should be faster than `ClassWrapper` but these numbers aren't lying. The .NET Runtime has some special optimizations it can perform for Struct. In .NET 6.0 this includes keeping the values of the struct in the registers. You can check out the work [here](https://github.com/dotnet/runtime/issues/43867)

## The Fix

I go to StackOverflow and Twitter to see if anyone had insight into what is going on. Upon the recommendation of [Phillip Carter](https://twitter.com/_cartermp) I move the code for generating the test data to inside the `Benchmark` class. When I do this, I get these results.

|                Method |     Mean |     Error |    StdDev |
|---------------------- |---------:|----------:|----------:|
|      InternalRawArray | 5.788 ms | 0.0241 ms | 0.0225 ms |
|  InternalClassWrapper | 5.983 ms | 0.0174 ms | 0.0154 ms |
| InternalStructWrapper | 5.980 ms | 0.0423 ms | 0.0396 ms |

Now the performance is roughly equivalent. Apparently, there are some gotchas with the BenchmarkDotNet library and F# modules. I go ahead and define some additional types for wrapping an `array`. I wrap an `array` using a Record and a Record with the `[<Struct>]` attribute. I create tests where the data is defined inside the `Benchmark` class and tests where the data is defined in a separate module. Here is what I ended up finding.

|                       Method |      Mean |     Error |    StdDev |
|----------------------------- |----------:|----------:|----------:|
|             InternalRawArray |  5.839 ms | 0.0517 ms | 0.0431 ms |
|             ExternalRawArray |  6.537 ms | 0.0429 ms | 0.0401 ms |
|         InternalClassWrapper |  5.866 ms | 0.0183 ms | 0.0203 ms |
|         ExternalClassWrapper |  6.903 ms | 0.0734 ms | 0.0686 ms |
|        InternalStructWrapper |  6.032 ms | 0.0933 ms | 0.0827 ms |
|        ExternalStructWrapper | 21.042 ms | 0.0932 ms | 0.0826 ms |
|       InternalRecordApproach |  5.920 ms | 0.0728 ms | 0.0608 ms |
|       ExternalRecordApproach |  6.899 ms | 0.0760 ms | 0.0674 ms |
| InternalStructRecordApproach |  5.899 ms | 0.0947 ms | 0.1297 ms |
| ExternalStructRecordApproach |  5.841 ms | 0.0576 ms | 0.0450 ms |

As you can see, I stumbled upon what appears to be a single outlier. You can also see that across the board the tests that are operating on data defined inside the `Benchmark` class outperform those where the data is defined externally. The only exception is the Struct Record but the difference in means is withen the noise of the tests. I think it's important for an F# developer whose looking for performance to be aware that where data is declared can affect your benchmarks and could lead to incorrect conclusions. The guidance I received from Phillip was to declare the data in the `Benchmark` class. [Bartosz Adamczewski](https://twitter.com/badamczewski01) recommends writing the library code in F# and the benchmarks in C#. This makes sense as I believe the BenchmarkDotNet library considers the C# use case primarily.

If you would like to see the full set of tests you can check out the repo [here](https://github.com/matthewcrews/UnitsOfMeasureIndexArray). Until next time, stay safe out there and have fun with your benchmarking!

Please send me an email at matthewcrews@gmail.com if you have any questions and subscribe so you can stay on top new posts and products I am offering.

{{<subscribe>}}

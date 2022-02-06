---
title: Fast Tracking of Item Observations
date: 2022-02-07
draft: false
tag: fsharp, high-performance
---

I am writing a graph algorithm at the heart of one of the products we are developing at Simulation Dynamics. A part of the algorithm is tracking whether a node or vertex has been observed yet or not. As the algorithm progresses, it will mark whether it has completed processing a node or a vertex to know it does not need to continue through the graph.

This algorithm is part of the inner loop of the simulation and needs to be as fast as possible. Speed is a critical feature for us, so essentially, anything is allowed if it gives us more speed. The only requirement is it be written in .NET (preferably F#) and runs on a modern x86-64 processor.

These graphs are almost always small, say 20 nodes and 30 vertices. We are modeling manufacturing processes, and the types of facilities that we simulate can be simplified down to these small graphs. This means we don't necessarily need to handle `N` nodes and `M` vertices, and we need an algorithm that is as fast as possible for these small cases.

# The F# Set

I love the F# `Set<'T>` collection. When I just need to process sets of value and perform comparisons between them, nothing beats the tried and true `Set<'T>`. For those curious, the F# `Set` and `Map` collections are backed by a variation on the AVL tree. We actually have [Victor Baybekov](https://twitter.com/buybackoff) to thank for some massive performance improvements he provided in [this pull request](https://github.com/dotnet/fsharp/pull/10188).

Before I test out various approaches, I create a baseline performance test. I will generate 1,000,000 different indexes in a range of `0` to `50`. For each of these indices, I will check whether it is in the `Set` or not. If it is not in the `Set`, I will add it. If it is in the `Set`, I will remove it. This will simulate the type of behavior that the graph algorithm uses while traversing the graph. I will also use the `BenchmarkDotNet` library to measure my performance.

```fsharp
open System
open FSharp.NativeInterop
open BenchmarkDotNet.Attributes
open BenchmarkDotNet.Running
open BenchmarkDotNet.Diagnosers

[<MemoryDiagnoser>]
type Benchmarks () =

    let testIndexCount = 1_000_000
    let indexRange = 50
    let rng = Random 123

    // Generate a set of random indices to check
    let testIndexes =
        [| for _ = 1 to testIndexCount do
            // Note: Next is exclusive on the upper bound
            rng.Next (0, indexRange) 
        |]

    [<Benchmark>]
    member _.SetTracker () =
        let mutable tracker = Set.empty

        for i = 0 to testIndexes.Length - 1 do
            let testIndex = testIndexes[i]
            if tracker.Contains testIndex then
                // Real world we would do work here and then flip the case
                tracker <- tracker.Add testIndex
            else
                tracker <- tracker.Remove testIndex

        tracker
```

When I run this benchmark, I get the following result.

```
|           Method |       Mean |     Error |    StdDev |     Median |     Gen 0 |    Allocated |
|----------------- |-----------:|----------:|----------:|-----------:|----------:|-------------:|
|       SetTracker | 9,511.0 us | 163.52 us | 152.96 us | 9,568.6 us | 4781.2500 | 40,000,008 B |
```

We now have a baseline to compare against.

## The .NET HashSet

The first thing someone will likely point out is, "Matthew, you are using an immutable collection that you are constantly updating and rebinding. Why not use a mutable collection?" Great question! Let's reach for the next most obvious collection in .NET, the `HashSet`. `HashSet` is found in the `System.Collections.Generic` namespace. Instead of using a tree to keep track of items, it uses a hash table. This should give us constant-time lookup, insertion, and removal.

We add a new test to our `Benchmarks` class...

```fsharp
[<Benchmark>]
member _.HashSetTracker () =
    let mutable tracker = Collections.Generic.HashSet ()

    for i = 0 to testIndexes.Length - 1 do
        let testIndex = testIndexes[i]
        if tracker.Contains testIndex then
            // Real world we would do work here and then flip the case
            tracker.Add testIndex |> ignore
        else
            tracker.Remove testIndex |> ignore

    tracker
```

... and re-run our benchmarks.

```
|           Method |       Mean |     Error |    StdDev |     Median |     Gen 0 |    Allocated |
|----------------- |-----------:|----------:|----------:|-----------:|----------:|-------------:|
|       SetTracker | 9,511.0 us | 163.52 us | 152.96 us | 9,568.6 us | 4781.2500 | 40,000,008 B |
|   HashSetTracker | 5,476.4 us |  58.96 us |  52.26 us | 5,478.8 us |         - |         68 B |
```

We see that the `HashSet` is giving us a performance boost over the F# `Set`. This is expected since we don't have to re-arrange a tree when we perform insertion and deletion. This is NOT to say that `Set` is inferior to `HashSet`; please don't consider that the takeaway. `Set` can do many things that `HashSet` cannot. It just happens in this instance, `HashSet` appears to be a better choice. Oh, but we aren't done yet...

## Plain Old Arrays

One of my favorite talks on the internet is one by [Scott Meyers](https://youtu.be/WDIkqP4JbkE), where he talks about the criticality of understanding memory hierarchies when writing high-performance code. My favorite moment is when he quotes a developer who writes high-frequency trading algorithms. The developer says, "I don't care how fancy your data structure is; an array will beat it."

I can map all nodes and vertices to `int` values ahead of time for my use case. This means that I could easily track whether I have visited a node or index by looking up a `bool` in an array using the value for the node or vertex as an index into the array. Let's try that and see what we get. I add a new benchmark to my `Benchmarks` class to see how this approach performs.

```fsharp
[<Benchmark>]
member _.BoolArrayTracker () =
    let tracker = Array.create indexRange false

    for i = 0 to testIndexes.Length - 1 do
        let testIndex = testIndexes[i]
        if tracker[testIndex] = false then
            // Real world we would do work here and then flip the case
            tracker[testIndex] <- true
        else
            tracker[testIndex] <- false

    tracker
```

We can now see how using an `array<bool>` performs against a `Set` and `HashSet`.

```
|           Method |       Mean |     Error |    StdDev |     Median |     Gen 0 |    Allocated |
|----------------- |-----------:|----------:|----------:|-----------:|----------:|-------------:|
|       SetTracker | 9,511.0 us | 163.52 us | 152.96 us | 9,568.6 us | 4781.2500 | 40,000,008 B |
|   HashSetTracker | 5,476.4 us |  58.96 us |  52.26 us | 5,478.8 us |         - |         68 B |
| BoolArrayTracker | 5,058.9 us |  66.05 us |  61.78 us | 5,065.1 us |         - |         84 B |
```

Alright, we see another modest speed boost. Using just an array, we've cut out some cycles that the `HashSet` has to perform when it performs a lookup. Now we are just taking a pointer to the head of the array and offsetting it to perform the lookup. It's hard to get much faster than this... or is it?

### Enter Data-Oriented Design

I've been on a quest to become a better developer, and my latest wanderings have brought me to the world of Data-Oriented Design. I love [Mike Acton's talk](https://youtu.be/rX0ItVEVjHc) at CPP Con, and I highly recommend it for anyone who hasn't seen it. He mentions that when he sees a `bool` in a `Struct` he almost always knows that something has gone wrong. The reason is that a `bool` only contains 1 bit of information, but it still takes up 8 bits of memory. This means that if you are moving many types back and forth in the cache which contains bools, you are wasting a large amount of memory bandwidth.

At the time I watched it, I thought, "Okay, I see your point about wasting memory bandwidth, but what if you really do need to know whether something is true or not?" That sat in the back of my mind for a while, and I didn't see a solution. The other day though, [Jérémie Chassaing](https://twitter.com/thinkb4coding) gave an excellent [talk](https://youtu.be/eVJ5b1lwMJ8) on writing high-performance F# with no memory allocation. In it, he showed an elegant trick for storing two `int32` in a single `int64`. You can watch the bit [here](https://www.youtube.com/watch?v=eVJ5b1lwMJ8&t=4008s). He holds one of the `int32` in the bottom 32-bits of the `int64` and the other `int32` value in the top 32-bits. That may seem silly, but it provides the compiler a unique optimization that it wouldn't have otherwise. It can store both of those `int32` in a single 64-bit register. This is the fastest form of memory in a CPU.

Now, who don't we take that same trick, and instead of storing 2 `int32` values in an `int64`, what about 64 bools in an `int64`? Each bit of the `int64` can correspond to a node or vertex in the graph. Instead of passing an `array<bool>` around, we can pass a single `int64`. Remember that for this use case, it is incredibly rare to see more than 20 nodes and 30 vertices, so 64-bits is plenty to store whether we have observed them or not.

Let's create a new type for wrapping our `int64` and provide some convenience methods for working with it. We will define it as a `Struct` since that provides the .NET compiler a large number of options for optimizing it.

```fsharp
[<Struct>]
type Int64Tracker =
    private {
        mutable Value : int64
    }
    static member Create () =
        { Value = 0L }

    // Check whether a position has been set to 1 or not
    member this.IsSet (position: int) =
        (this.Value &&& (1L <<< position)) <> 0L

    // Sets a bit at a position to 1
    member this.Set (position: int) =
        this.Value <- (1L <<< position) ||| this.Value

    // Sets a bit at a position to 0
    member this.UnSet (position: int) =
        this.Value <- ~~~ (1 <<< position) &&& this.Value
```

Let's unpack this. The `Value` field is used to track whether something has been observed or not. The `Create` method gives us a new `Int64Tracker` where all the bits are set to `0`. The `IsSet` method takes a position and checks whether that position is 0 or not. It does this by taking a value of `1L`, which puts a value of 1 in the bottom bit and then left, shifting it to the desired position. It then does a bitwise `AND` between the `Value` and the bit-shifted value. This will return 0s for every location except for possibly the position we are interested in. If there is a 1 in the desired position, it will `AND` with our shifted 1 and return a non-zero number. If there is a `0` in the position of interest, all the bits will return `0`, and the resulting value will be equal to `0`.

The `Set` method takes a position and sets the bit in that position to `1`. It does this by taking a value of `1L`, which has a `1` in the bottom bit, left-shifts it to the desired position, and then performs a bitwise `OR` with `Value`. If the value in the given position was already `1`, it will still be a `1`. If it was `0`, it will be changed to `1`. Here's a [quick video](https://youtu.be/ZRNO-ewsNcQ?t=64) showing the operation.

The final method, `UnSet`, takes `1L` and left-shifts it to the desired position. It then does a bitwise negation. This means that all the bits will be `1` except for the position that we want to turn into a `0`. We then `AND` that mask with `Value` to get our updated `Value`. Here's a [video](https://youtu.be/ZRNO-ewsNcQ?t=113) showing how this is done.

We now have a new type that allows us to track up to 64 different items, which is enough for our use case. Let's add a benchmark.

```fsharp
[<Benchmark>]
member _.Int64Tracker () =
    let mutable tracker = Int64Tracker.Create ()
    
    for i = 0 to testIndexes.Length - 1 do
        let testIndex = testIndexes[i]
        if tracker.IsSet testIndex then
            // Real world we would do work here and then flip the case
            tracker.Set testIndex
        else
            tracker.UnSet testIndex

    tracker
```

And we run our benchmarks to see how fast we are now.

```
|           Method |       Mean |     Error |    StdDev |     Median |     Gen 0 |    Allocated |
|----------------- |-----------:|----------:|----------:|-----------:|----------:|-------------:|
|       SetTracker | 9,863.6 us | 193.62 us | 244.86 us | 9,776.3 us | 4781.2500 | 40,000,008 B |
|   HashSetTracker | 5,561.7 us |  75.37 us |  70.50 us | 5,583.8 us |         - |         68 B |
| BoolArrayTracker | 5,013.9 us |  58.71 us |  54.92 us | 5,006.0 us |         - |         84 B |
|     Int64Tracker |   745.3 us |  14.87 us |  14.60 us |   748.2 us |         - |          1 B |
```

That is an excellent speedup. We are now 13X faster than our first benchmark and ~6.5X faster than an `array<bool>`. We should be feeling good at this point. We've found a much faster way to track observations for most of our use cases.

Let's be honest, though; we will need to track more than 64 items at some point. What would we do in that case? We could fall back to the `array<bool>`. Wouldn't it be nice if we could use those bit tricks with a larger number of items, though?

## Enter the Span

In cases where we need to work with more than 64 items, we can't rely on a single `int64`. It doesn't have enough memory. In theory, we could use an `array<int64>` but that means we will allocate something on the heap. Wouldn't it be nice to just get a chunk of memory, use it, and then throw it away? Fortunately, we can! F# 4.5 added support to stackalloc memory.

The syntax for creating it is not as clean as it is in C#, but [Bartosz Sypytkowski](https://twitter.com/Horusiath) provided a great [blog post](https://bartoszsypytkowski.com/writing-high-performance-f-code/) where he provides the following function for conveniently stack allocating a `Span`.

```fsharp
let inline stackalloc<'a when 'a: unmanaged> (length: int): Span<'a> =
    let p = NativePtr.stackalloc<'a> length |> NativePtr.toVoidPtr
    Span<'a>(p, length)
```

Now that we can grab small chunks of memory let's write a new benchmark that uses a `Span<int64>` to track observations.

```fsharp
[<Benchmark>]
member _.SpanInt64Tracker () =
    let bitsPerInt64 = 64
    let requiredInt64s = (indexRange + bitsPerInt64 - 1) / bitsPerInt64
    let spanIndex = stackalloc<int64> requiredInt64s

    for i = 0 to testIndexes.Length - 1 do
        let testIndex = testIndexes[i]
        let int64Index = testIndex / bitsPerInt64
        let bitIndex = testIndex % bitsPerInt64

        if (spanIndex[int64Index] &&& (1 <<< bitIndex)) <> 0 then
            // Real world we would do work here and then flip the case
            spanIndex[int64Index] <- (1 <<< bitIndex) ||| spanIndex[int64Index]
        else
            spanIndex[int64Index] <- ~~~ (1 <<< bitIndex) &&& spanIndex[int64Index]

    spanIndex
```

The main difference here is that we need to calculate which `int64` in our `Span<int64>` we need to check and which bit in the `int64` we want to test. That is why we have to calculate the `int64Index` and `bitIndex`. Let's run this benchmark and see what we get.

```
|           Method |       Mean |     Error |    StdDev |     Gen 0 |    Allocated |
|----------------- |-----------:|----------:|----------:|----------:|-------------:|
|       SetTracker | 9,293.6 us | 163.95 us | 264.75 us | 4781.2500 | 40,000,008 B |
|   HashSetTracker | 5,398.9 us |  57.63 us |  53.91 us |         - |         68 B |
| BoolArrayTracker | 5,007.6 us |  67.62 us |  63.25 us |         - |         84 B |
|     Int64Tracker |   735.5 us |   5.40 us |   4.79 us |         - |            - |
| SpanInt64Tracker | 2,893.7 us |  57.76 us | 155.18 us |         - |          2 B |
```

Not bad. It's not as fast as the `Int64Tracker` but is still faster than the `BoolArrayTracker`. The major upside compared to the `Int64Tracker` is that it is not limited to 64 items, and we can track as many as we would like. I feel like there's more that could be done here, but I wanted to share what I have found so far.

If you liked this post and want to hear more about writing high-performance F# and Mathematical Programming, please subscribe to my list!

{{<subscribe>}}
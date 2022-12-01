# Fast Iteration of Set Bits in BitSet


> This article is for the 2022 F# Advent Calendar

> Full code for article found [here](https://github.com/matthewcrews/FSharpPerformance/tree/main/BitSetEnumeration)

While building the Aidos engine for Discrete Rate Simulation, I often have to build custom collections to meet the project's performance requirements. Don't get me wrong, many of the collections built into .NET are great, but they are general-purpose. General-purpose collections must meet the requirements of many use cases. I have a single use case in mind for my work, and performance is one of the critical features. If our engine isn't orders of magnitude faster than the competition, we don't have a compelling product.

In Aidos, I often need to track items that have changed during a time step of the simulation. I also track entities with an `int` that has been annotated with a Unit of Measure. This means an entity ends up being an `int<'EntityType>`. I also cannot have duplicates for my use case, so I need to maintain a distinct set of entities that have changed. One way to do this would be to use a `HashSet`. HashSet is built into .NET and provides `O(1)` insertion, which you would think would be ideal for this use case. The downside to a `HashSet` is that its memory will be allocated on the heap. If you have to create a `HashSet` for every iteration of a hot loop, this can cause excess GC pressure.

Instead, what I use is a custom `BitSet`. `BitSet` is a struct that wraps an array of `uint64,` which acts as a bit array that I manually manage. The .NET runtime has a `BitArray` class, but it does not provide the API I need for my use cases. I need to iterate through all the set bits and call a function with the index of the set bit as an argument.

One of the advantages of the `BitSet` approach over `HashSet` is that the array used by the `BitSet` can be allocated from an `ArrayPool`, which means that `BitSet` will never increase GC pressure or take up room on the heap. The other is that it is faster than `HashSet` for iterating through the set bits using the `BitSet.iter` function.

Here is the definition of the `BitSet` type:

```fsharp
[<Struct>]
type BitSet<[<Measure>] 'Measure>(buckets: uint64[]) =

    new(capacity: int) =
        let bucketsRequired = (capacity + 63) >>> 6
        let buckets: uint64[] = Array.zeroCreate bucketsRequired
        BitSet<_> buckets

    /// WARNING: Public for inlining
    member _._buckets = buckets
    member _.Capacity = buckets.Length * 64

    member b.Count =
        let mutable total = 0

        for bucket in b._buckets do
            total <- total + System.Numerics.BitOperations.PopCount bucket

        total

    member b.Item
        with get (itemKey: int<'Measure>) =
            let bucketId, mask = Helpers.computeBucketAndMask itemKey
            let buckets = b._buckets
            let bucket = buckets[bucketId]
            (bucket &&& mask) <> 0UL

    member b.Contains(itemKey: int<'Measure>) =
        let bucketId, mask = Helpers.computeBucketAndMask itemKey
        let buckets = b._buckets
        let bucket = buckets[bucketId]
        (bucket &&& mask) <> 0UL

    member b.Add(itemKey: int<'Measure>) =
        let bucketId, mask = Helpers.computeBucketAndMask itemKey
        let bucket = buckets[bucketId]
        buckets[bucketId] <- bucket ||| mask

    member b.Remove(itemKey: int<'Measure>) =
        let bucketId, mask = Helpers.computeBucketAndMask itemKey
        let buckets = b._buckets
        let bucket = buckets[bucketId]
        buckets[bucketId] <- bucket &&& ~~~mask
```

We also have a `BitSet` module where we define the functions for operating on `BitSet`. Here I show just the `iter` function. `iter` loops through each set bit in the array and calls the lambda `f` with the index of the set bit as the argument.

```fsharp
module BitSet =

    let inline iter ([<InlineIfLambda>] f: int<'Measure> -> unit) (b: BitSet<'Measure>) =
        let mutable i = 0

        // Source of algorithm: https://lemire.me/blog/2018/02/21/iterating-over-set-bits-quickly/
        while i < b._buckets.Length do
            let mutable bitSet = b._buckets[i]

            while bitSet <> 0UL do
                let r = System.Numerics.BitOperations.TrailingZeroCount bitSet
                let itemId = (i <<< 6) + r |> LanguagePrimitives.Int32WithMeasure<'Measure>

                (f itemId)

                bitSet <- bitSet ^^^ (1UL <<< r)

            i <- i + 1
```

I set up a benchmark to compare the performance of `HashSet` and `BitSet`. I define a unit of measure, `Chicken`, which I will use as the entity type to annotate my `int` values. I add 10 `int<Chicken>` between the values of `0<Chicken>` and `99<Chicken>` to both of these collections. This range of values is a good representation of the use case that I am concerned with. I then write a benchmark for measuring the time to iterate through the values in both collections to see how long it takes for both collections. When I run the benchmark using `BenchmarkDotNet` I get the following result:

```fsharp
|           Method |      Mean |     Error |    StdDev |   Gen0 | Allocated |
|----------------- |----------:|----------:|----------:|-------:|----------:|
|          HashSet | 23.377 ns | 0.3017 ns | 0.2822 ns |      - |         - |
|             Iter |  7.048 ns | 0.1168 ns | 0.1092 ns |      - |         - |
```

We see that the `iter` function for `BitSet` is approximately 3x faster than `HashSet` for iterating through the values.

## The Problem

Now, you may be thinking that `BitSet` is great, but there is a downside to this approach. The `iter` function takes a lambda as one of its arguments. Whenever the `BitSet` encounters a set bit, it then calls the lambda with the index of the set bit. Lambdas are intrinsic to programming in F#, but they have limitations. One of those limitations is that they cannot capture `Span<'T>` or `ByRefLike` types. Most of the time, this is not a big deal. F# developers are not often known as hardcore performance programmers, so most F# developers will not run into this problem.

I, on the other hand, work with `Span<'T>` and `ByRefLike` types all the time. They can be incredibly powerful for increasing your program's performance and decreasing memory allocations. Now, a word of caution. You probably don't need this. You can lead a very happy life as an F# developer, and never worry about this. This limitation only becomes an issue when you are trying to eke out every bit of performance you can, and you are likely not in that scenario. I happen to be in an odd situation because I work for a company with an F# dev team, and I'm tasked with writing libraries for others to use that must be fast. For strategic reasons, we constrain ourselves to F#, so calling out to C/C++/Rust is not an option. You will appreciate what we cover next if you find yourself in a similar situation.

I have asked about relaxing some of the compiler restrictions around lambdas and capturing `Span<'T>`, but the effort would be large. The more I dug into how the F# compiler and the CLR interact, my appreciation for the complexity of the problem grows. This is also not the most important feature for the growth of F#, so I'm not going to push for it. I hope to get good enough to contribute it to the F# compiler someday 😊.

## The Solution

So how do we get around this limitation? `BitSet` is intended for these hot loops where we likely want to be able to use some stack-allocated memory. This means we must be able to work with `BitSet` and `Span<'T>` simultaneously. The simple solution is to expose a new way of iterating through the set bits in the `BitSet`. We can implement `IEnumerable<'T>` for `BitSet` and use a `for...in...do` loop.

The easiest way to implement `IEnumerable<'T>` for `BitSet` is to define a `BitSetEnumerator`, which takes the logic used in the `iter` function but exposes it in a way that the `IEnumerable<'T>` interface expects. Let's see what that looks like:

```fsharp
type BitSetEnumerator<[<Measure>] 'Measure>(buckets: uint64[]) =
    let mutable bucketIdx = 0
    let mutable curBucket = 0UL
    let mutable curItem = LanguagePrimitives.Int32WithMeasure<'Measure> -1

    member _.Current =
        if curItem < 0<_> then
            raise (InvalidOperationException "Enumeration has not started. Call MoveNext.")
        else
            curItem

    member b.MoveNext() =
        // Check if we have actually started iteration
        if curItem < 0<_> then
            curBucket <- buckets[bucketIdx]

        // There are still items in the Current bucket we should return
        if curBucket <> 0UL then
            let r = System.Numerics.BitOperations.TrailingZeroCount curBucket
            curItem <- LanguagePrimitives.Int32WithMeasure<'Measure>((bucketIdx <<< 6) + r)
            curBucket <- curBucket ^^^ (1UL <<< r)
            true

        // We need to move to the next bucket of items
        else
            bucketIdx <- bucketIdx + 1

            if bucketIdx < buckets.Length then
                curBucket <- buckets[bucketIdx]
                b.MoveNext()
            else
                false

    member _.Reset() =
        bucketIdx <- 0
        curBucket <- 0UL
        curItem <- LanguagePrimitives.Int32WithMeasure<'Measure> -1

    interface IEnumerator<int<'Measure>> with
        member b.Current = b.Current :> Object
        member b.Current = b.Current
        member b.MoveNext() = b.MoveNext()
        member b.Reset() = b.Reset()
        member b.Dispose() = ()
```

The `BitSetEnumerator` type defines three methods for fulfilling the `IEnumerable<'T>` contract: `Current`, `MoveNext`, and `Reset`. You can see how the `BitSetEnumerator` fulfills the `IEnumerable<'T>` interface at the bottom. The type uses the same bit-shifting logic `iter` uses but breaks it up to support the methods that `IEnumerable<'T>` expects.

We can then have the `BitSet` collection implement the `IEnumerable<'T>` interface by returning an instance of the `BitSetEnumerator` when calling the `GetEnumerator` method.

```fsharp
open System.Collections.Generic

[<Struct>]
type BitSet<[<Measure>] 'Measure>(buckets: uint64[]) =
    // Previous logic here

    interface System.Collections.IEnumerable with
        member b.GetEnumerator() =
            (new BitSetEnumerator<'Measure>(buckets)) :> System.Collections.IEnumerator

    interface IEnumerable<int<'Measure>> with
        member s.GetEnumerator() = new BitSetEnumerator<'Measure>(buckets)

```

When we add this approach to the benchmarks, we get the following result:

```fsharp
|     Method |      Mean |     Error |    StdDev |   Gen0 | Allocated |
|----------- |----------:|----------:|----------:|-------:|----------:|
|    HashSet | 23.347 ns | 0.4024 ns | 0.3764 ns |      - |         - |
|       Iter |  7.087 ns | 0.1000 ns | 0.0935 ns |      - |         - |
| Enumerable | 55.521 ns | 0.9228 ns | 0.8181 ns | 0.0048 |      40 B |
```

The `IEnumerable<'T>` approach is twice as slow as using a `HashSet`. This is less than ideal. It is also allocating on the heap. This is because the interface necessitates the creation of an object on the heap. We've negated most, if not all, of the benefits we hope to get from `BitSet`. What can we do?

## Ducks All The Way Down

There's a feature of .NET that I don't hear about much but is important in this scenario. The .NET runtime will use duck-typing to implement C# `foreach` loops and their equivalents. The runtime will look at the type and see if it has a `GetEnumerator` method that returns a type with the `Current` field and the `MoveNext` method. Well, the `for...in...do` loop in F# is the equivalent to the C# `foreach` loop.

What if instead of implementing `IEnumerable<'T>` we rely on the .NET duck-typing approach? We can change our enumerator to be a `struct` so that it doesn't allocate any memory on the heap, and we'll avoid the overhead of an interface.

Here's what the `BitSetEnumerator` looks like as a struct with only the necessary pieces for duck-typing.

```fsharp
[<Struct>]
type BitSetEnumerator<[<Measure>] 'Measure> =
    val mutable BucketIdx: int
    val mutable CurBucket: uint64
    val mutable CurItem: int<'Measure>
    val Buckets: uint64[]

    new(buckets: uint64[]) =
        {
            BucketIdx = 0
            CurBucket = 0UL
            CurItem = LanguagePrimitives.Int32WithMeasure<'Measure> -1
            Buckets = buckets
        }

    member b.Current =
        if b.CurItem < 0<_> then
            raise (InvalidOperationException "Enumeration has not started. Call MoveNext.")
        else
            b.CurItem

    member b.MoveNext() =
        // Check if we have actually started iteration
        if b.CurItem < 0<_> then
            b.CurBucket <- b.Buckets[b.BucketIdx]

        // There are still items in the Current bucket we should return
        if b.CurBucket <> 0UL then
            let r = System.Numerics.BitOperations.TrailingZeroCount b.CurBucket
            b.CurItem <- LanguagePrimitives.Int32WithMeasure<'Measure>((b.BucketIdx <<< 6) + r)
            b.CurBucket <- b.CurBucket ^^^ (1UL <<< r)
            true

        // We need to move to the next bucket of items
        else
            b.BucketIdx <- b.BucketIdx + 1

            if b.BucketIdx < b.Buckets.Length then
                b.CurBucket <- b.Buckets[b.BucketIdx]
                b.MoveNext()
            else
                false
```

Things look a bit different since `BitSetEnumerator` is now a struct and therefore requires different approaches to handling the internal data.

We also change the `BitSet` type to only have a `GetEnumerator()` method instead of implementing `IEnumerable<'T>`.

```fsharp
[<Struct>]
type BitSet<[<Measure>] 'Measure>(buckets: uint64[]) =
    // Previous logic

    member b.GetEnumerator() = BitSetEnumerator<'Measure>(buckets)
```

When we benchmark this approach, we get the following:

```
|     Method |      Mean |     Error |    StdDev |   Gen0 | Allocated |
|----------- |----------:|----------:|----------:|-------:|----------:|
|    HashSet | 23.347 ns | 0.4024 ns | 0.3764 ns |      - |         - |
|       Iter |  7.087 ns | 0.1000 ns | 0.0935 ns |      - |         - |
| Enumerable | 55.521 ns | 0.9228 ns | 0.8181 ns | 0.0048 |      40 B |
| DuckTyping | 28.039 ns | 0.5183 ns | 0.4848 ns |      - |         - |
```

This is much better. Our performance is almost that of a `HashSet`. Something to be aware of, the duck-typing approach and the `IEnumerable<'T>` are not mutually exclusive. If you implement both, the runtime will pick the faster approach in the testing I have done. In the production code, we include both because the `IEnumerable<'T>` is necessary for using the `BitSet` with the `Seq` module.

## Inline All The Things (When it helps)

You have probably noticed that our loop-based approach's performance is still not near the `iter` method. That's to be expected. The for-loop approach adds overhead to the iteration. The F# compiler has some special transforms that it does for arrays which makes using a `for...in...do` loop over the elements of an array incredibly fast, but most other collections do not get that special treatment.

There is something we can do to get a little more performance, though. Right now, each time the `MoveNext` method is called, it creates a new stack frame. This adds overhead to the loop when it has to copy data for each instance of the stack frame. If we could inline the logic of the `MoveNext` method, we could reduce the number of stack frames created and potentially get a performance boost.

If you try to add the `inline` keyword to `Current` and `MoveNext` on `BitSetEnumerator`, you will have a problem. The compiler will give you an error that looks something like this:

```
D:\Documents\GitHub\FSharpPerformance\BitSetEnumeration\DuckTyping.fs(55,17): error FS1114: The value 'BitSetEnumeration.DuckTyping.BitSetEnumerator.MoveNext' was marked inline but was not bound in the optimi
zation environment [D:\Documents\GitHub\FSharpPerformance\BitSetEnumeration\BitSetEnumeration.fsproj]
D:\Documents\GitHub\FSharpPerformance\BitSetEnumeration\DuckTyping.fs(37,21): error FS1113: The value 'MoveNext' was marked inline but its implementation makes use of an internal or private function which is
not sufficiently accessible [D:\Documents\GitHub\FSharpPerformance\BitSetEnumeration\BitSetEnumeration.fsproj]
D:\Documents\GitHub\FSharpPerformance\BitSetEnumeration\DuckTyping.fs(55,17): warning FS1116: A value marked as 'inline' has an unexpected value [D:\Documents\GitHub\FSharpPerformance\BitSetEnumeration\BitSet
Enumeration.fsproj]
D:\Documents\GitHub\FSharpPerformance\BitSetEnumeration\DuckTyping.fs(55,17): error FS1118: Failed to inline the value 'MoveNext' marked 'inline', perhaps because a recursive value was marked 'inline' [D:\Doc
uments\GitHub\FSharpPerformance\BitSetEnumeration\BitSetEnumeration.fsproj]

The build failed. Fix the build errors and run again.
```

That looks like a lot of garbage, but the important part is near the end. It reports an error on line 55 of our `DuckTyping.fs`, which mentions "perhaps because a recursive value was marked `inline`." That's the clue we need. The `MoveNext` method is recursive at the moment, so the inlining logic of the F# compiler cannot work. What we need to do is remove this recursion. When we remove the recursion from the `MoveNext` method, we get the following:

```fsharp
    member inline b.MoveNext() =
        // Check if we have actually started iteration
        if b.CurItem < 0<_> then
            b.CurBucket <- b.Buckets[b.BucketIdx]

        // There are still items in the Current bucket we should return
        if b.CurBucket <> 0UL then
            let r = System.Numerics.BitOperations.TrailingZeroCount b.CurBucket
            b.CurItem <- LanguagePrimitives.Int32WithMeasure<'Measure>((b.BucketIdx <<< 6) + r)
            b.CurBucket <- b.CurBucket ^^^ (1UL <<< r)
            true

        // We need to move to the next bucket of items
        else
            b.BucketIdx <- b.BucketIdx + 1
            let mutable result = false
            
            while b.BucketIdx < b.Buckets.Length && (not result) do
                b.CurBucket <- b.Buckets[b.BucketIdx]
                
                // There are still items in the Current bucket we should return
                if b.CurBucket <> 0UL then
                    let r = System.Numerics.BitOperations.TrailingZeroCount b.CurBucket
                    b.CurItem <- LanguagePrimitives.Int32WithMeasure<'Measure>((b.BucketIdx <<< 6) + r)
                    b.CurBucket <- b.CurBucket ^^^ (1UL <<< r)
                    result <- true
                
                if not result then
                    b.BucketIdx <- b.BucketIdx + 1
            
            result
```

The logic for moving to the next bucket and checking for values has gotten more complex, but it no longer recurses. This allows us to use the `inline` keyword to get the F# compiler to inline this logic where it is used. This will reduce the number of stack frames used in our loop. Let's see what the performance of this version is:

```fsharp
|     Method |      Mean |     Error |    StdDev |   Gen0 | Allocated |
|----------- |----------:|----------:|----------:|-------:|----------:|
|    HashSet | 23.347 ns | 0.4024 ns | 0.3764 ns |      - |         - |
|       Iter |  7.087 ns | 0.1000 ns | 0.0935 ns |      - |         - |
| Enumerable | 55.521 ns | 0.9228 ns | 0.8181 ns | 0.0048 |      40 B |
| DuckTyping | 28.039 ns | 0.5183 ns | 0.4848 ns |      - |         - |
|   Inlining | 13.315 ns | 0.2178 ns | 0.2037 ns |      - |         - |
```

Inlining is now faster than `HashSet` but still slower than `Iter`. This is a win for me because there's now no performance downside to `BitSet` compared to `HashSet` for this scenario. Would I like to be able to match the performance of `Iter`? Yes, absolutely, but this is already nowhere near the bottleneck of our engine, so I moved on to other problems.

## Conclusion

You've learned a little about implementing `IEnumerable<'T>` for custom collections that you write and how to use the duck-typing of the `foreach` loop in .NET to get even better performance. We've also shown that we can perform even better using the `inline` keyword to remove stack frames.

I recommend that you stick with the built-in looping functions provided by F#: `map`, `iter`, `iteri`, etc. They are highly optimized and will give you great performance out of the box. In rare cases, you should consider other options where the need to capture a `Span<'T>` or another restriction forces you to use other looping constructs. I hope you find this helpful. Please feel free to reach out with any questions or critiques 😊.

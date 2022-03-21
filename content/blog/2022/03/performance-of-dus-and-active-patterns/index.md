---
title: Performance of Discriminated Unions and Active Patterns
date: 2022-03-20
draft: false
tag: fsharp, performance
---

Part of my work is writing algorithms to analyze networks of nodes representing manufacturing systems. Each node can be one of four different types: Buffer, Constraint, Merge, and Split. A manufacturing system that we would want to simulate is typically made up of no more than 100 of these nodes. A natural way to encode these types would be to use Discriminated Unions (DU). I also use Units of Measure to annotate integers that are the ids for these manufacturing nodes.

```fsharp
[<Measure>] type BufferId
[<Measure>] type ConstraintId
[<Measure>] type SplitId
[<Measure>] type MergeId

[<RequireQualifiedAccess>]
type Node =
    | BufferId of bufferId : int<BufferId>
    | ConstraintId of constraintId : int<ConstraintId>
    | MergeId of mergeId : int<MergeId>
    | SplitId of splitId : int<SplitId>
```

The default encoding for a DU in F# is as a reference type. This means that if you have a `Node array`, each array element will be a pointer to where the `Node` itself is stored in memory. If you need to quickly lookup up Nodes and what type they are, you can run into a phenomenon known as Pointer Chasing. Pointer Chasing is when the CPU is trying to run your code but constantly has to look up new regions of memory because the data is spread out. As .NET developers, we tend not to think about this much, but it can become a severe problem in performance-sensitive code.

Fortunately, F# allows to encode DUs as structs using the `[<Struct>]` attribute. Here is what that looks like:

```fsharp
[<Struct; RequireQualifiedAccess>]
type Node =
    | BufferId of bufferId : int<BufferId>
    | ConstraintId of constraintId : int<ConstraintId>
    | MergeId of mergeId : int<MergeId>
    | SplitId of splitId : int<SplitId>
```

Now, if you have a `Node array`, the data for the node will be stored in the array itself so you can eliminate having to perform an additional lookup. There is a serious downside to this, though. The F# compiler will allocate space for each possible case of the DU instead of only the area necessary for the instantiated individual case. This means that instead of just taking up the space of just two `int`, this struct `Node` will take up one `int` to encode the case and four more `int` for each possible case. For a deeper explanation of this, I refer you to [this](https://bartoszsypytkowski.com/writing-high-performance-f-code/) excellent post by [Bartosz Sypytkowski](https://bartoszsypytkowski.com/). If a DU has too many cases, the benefits of the struct layout will quickly be negated by this padding.

## Alternative Encoding

I was curious if there was another way to approach my problem. I like the elegance of the `match ... with` syntax in F#, and I am loathed to give it up. Since my `Node` type is just encoding different possible `int` values, why not do some bit hacking? Now, I will be the first to say this is non-traditional F# code, but I'm curious, so why not perform the experiment?

I'll define a new version of `Node` that will use an `int` to hold the data about which case it represents and the value. I will use the last 4 bits of the `Value` field to encode, which case the `Node` represents and the top 28 bits will hold the value. This is cutting off some of the space that `int` can express, but since our networks are never more than 1,000 nodes, there is no practical loss of modeling space.

```fsharp
[<Struct>]
type Node =
    private {
    	Value : int
    }
    static member BufferIdCode = 0
    static member ConstraintIdCode = 1
    static member MergeIdCode = 2
    static member SplitIdCode = 3
```

The static members `BufferIdCode`, `ConstraintIdCode`, `MergeIdCode`, and `SplitIdCode,` will be the values I use to encode the Node cases. To still use the `match...with` syntax, I will need to define an [Active Patterns](https://docs.microsoft.com/en-us/dotnet/fsharp/language-reference/active-patterns) for unpacking the case. Active Patterns are an elegant feature of F# for decomposing data into different forms. In this case, I will take the `int` held in my `Node` type, check which case it is, and then return the corresponding id.

I use a mask and a bitwise AND operation to get the last 4 bits (also known as a nibble) of the `Value` field, which gives me an `int`. I compare that `int` with the possible code values to figure out which type of node it is. I then bit shift the `Value` field to the right 4 bits to convert it to the id value it stores and multiply the result by the right Unit of Measure to ensure type safety.

```fsharp
let (|BufferId|ConstraintId|SplitId|MergeId|) (node: Node) =
    // Get the nibble which encodes the type of Id
    let nodeType = node.Value &&& 0x0000000F
    // Get the value of the Id
    let idValue = node.Value >>> 4

    if nodeType = Node.BufferIdCode then
        BufferId (idValue * 1<BufferId>)

    elif nodeType = Node.ConstraintIdCode then
        ConstraintId (idValue * 1<ConstraintId>)

    elif nodeType = Node.MergeIdCode then
        MergeId (idValue * 1<MergeId>)

    elif nodeType = Node.SplitIdCode then
        SplitId (idValue * 1<SplitId>)

    else
        invalidArg (nameof node) "Node Id type does not match known Node Types"
```

There is a downside to this technique, though. Active Patterns will cause additional allocations and trigger additional Garbage Collection (GC). Fortunately, F# recognized this and enabled the returning of a struct from a [Partial Active Pattern](https://docs.microsoft.com/en-us/dotnet/fsharp/language-reference/active-patterns#struct-representations-for-partial-active-patterns). The syntax is a little odd due to a limitation in the compiler, but it works for our purposes.

Here is the equivalent approach using the Partial Active Pattern that returns a `ValueStruct` to reduce GC pressure. Compared to our first Active Pattern, the downside is that we have to define a separate one for each case we want to match against.

```fsharp
[<return: Struct>]
let (|BufferId|_|) (node: Node) =
    let nodeType = node.Value &&& 0x0000000F

    if nodeType = Node.BufferIdCode then
        let idValue = node.Value >>> 4
        ValueSome (idValue * 1<BufferId>)
    else
        ValueNone


[<return: Struct>]
let (|ConstraintId|_|) (node: Node) =
    let nodeType = node.Value &&& 0x0000000F

    if nodeType = Node.ConstraintIdCode then
        let idValue = node.Value >>> 4
        ValueSome (idValue * 1<ConstraintId>)
    else
        ValueNone


[<return: Struct>]
let (|MergeId|_|) (node: Node) =
    let nodeType = node.Value &&& 0x0000000F

    if nodeType = Node.MergeIdCode then
        let idValue = node.Value >>> 4
        ValueSome (idValue * 1<MergeId>)
    else
        ValueNone


[<return: Struct>]
let (|SplitId|_|) (node: Node) =
    let nodeType = node.Value &&& 0x0000000F

    if nodeType = Node.SplitIdCode then
        let idValue = node.Value >>> 4
        ValueSome (idValue * 1<SplitId>)
    else
        ValueNone
```

## Benchmark Setup

We will now set up two types of tests. The first set of tests will randomly jump around a `Node array`, check the Node type, and perform different work based on the case. This is most similar to the workloads I experience when writing algorithms for simulating manufacturing systems. For curiosity, I will also have a test for a linear traversal of a `Node array` and perform the same work as the random access. This should illustrate the difference in performance between a predictable access pattern and a random one. The branch predictor in the CPU will have a more challenging time with the random access, and we expect it to be slower.

To see the impact of the Active Pattern on memory allocation and GC, we will include the `[<MemoryDiagnoser>]` attribute on a `Benchmarks` class that holds our tests. This will tell BenchmarkDotNet to monitor how much allocation is occurring. We should see the Active Pattern approach incur more GC activity. We also include the `BranchMispredictions` and `CacheMisses` hardware counters to how well the CPU can optimize our code. The ideal code has `0` Branch Mispredictions. Whenever we mispredict a branch, we can lose 20 - 30 cycles worth of work depending on the CPU. Cache Misses occur when our data is not in the cache, and the CPU has to go out to memory to retrieve the data. The CPU will do its best to predict what data it needs and fetch it ahead of time. When it guesses wrong, we can incur a severe performance penalty.

```fsharp
[<MemoryDiagnoser; HardwareCounters(HardwareCounter.BranchMispredictions, HardwareCounter.CacheMisses)>]
type Benchmarks () =

    let rng = Random 123
    let nodeCount = 100
    let lookupCount = 100
    let loopCount = 100_000

    let nodes =
        [|for i in 0 .. nodeCount - 1 ->
            match rng.Next (0, 4) with
            | 0 -> DuEncoding.Node.BufferId 1<BufferId>
            | 1 -> DuEncoding.Node.ConstraintId 1<ConstraintId>
            | 2 -> DuEncoding.Node.MergeId 1<MergeId>
            | 3 -> DuEncoding.Node.SplitId 1<SplitId>
            | _ -> failwith "The RNG generated a number outside the allowed bounds"
        |]
```

At the beginning of the `Benchmarks` class, we declare a random number generator `rng` which we will use to produce random nodes and indices to look up. We have a `nodeCount` of 100, which is the number of nodes we will generate. The `lookupCount` is the number of node lookups we will perform during each test loop. The `loopCount` is the number of loops we will perform in each test. `nodes` is an array of randomly generated nodes for our test.

Our first test performs random lookups in the `nodes` array. It matches against the case of the DU and then adds a different amount to an accumulator based on the case. This is to simulate some amount of work being done for each node that was looked up.

```fsharp
[<Benchmark>]
member _.DuEncodingRandomAccess () =
    let mutable acc = 0

    for lookupsIndex = 0 to loopCount - 1 do
        let lookups = randomNodeIndices[lookupsIndex]

        for lookupIndex = 0 to lookups.Length - 1 do
            let randomNodeIndex = lookups[lookupIndex]

            match nodes[randomNodeIndex] with
            | DuEncoding.Node.BufferId bufferId -> acc <- acc + 1
            | DuEncoding.Node.ConstraintId constraintId -> acc <- acc + 2
            | DuEncoding.Node.MergeId mergeId -> acc <- acc + 3
            | DuEncoding.Node.SplitId splitId -> acc <- acc + 4

    acc
```

The second test does the same work, but the `Node` type is the struct representation instead of the reference-based one.

```fsharp
[<Benchmark>]
member _.StructDuEncodingRandomAccess () =
    let mutable acc = 0

    for lookupsIndex = 0 to loopCount - 1 do
        let lookups = randomNodeIndices[lookupsIndex]
        
        for lookupIndex = 0 to lookups.Length - 1 do
            let randomNodeIndex = lookups[lookupIndex]

            match structNodes[randomNodeIndex] with
            | StructDuEncoding.Node.BufferId bufferId -> acc <- acc + 1
            | StructDuEncoding.Node.ConstraintId constraintId -> acc <- acc + 2
            | StructDuEncoding.Node.MergeId mergeId -> acc <- acc + 3
            | StructDuEncoding.Node.SplitId splitId -> acc <- acc + 4

    acc
```

The third and fourth tests also perform the same work but with the Active Pattern and Partial Active Pattern approaches. Remember, the Active Pattern allocates objects while the Partial Active Pattern is returning a struct and therefore not causing any GC overhead.

```fsharp
[<Benchmark>]
member _.IntEncodingWithActivePatternRandomAccess () =
    let mutable acc = 0


    for lookupsIndex = 0 to loopCount - 1 do
        let lookups = randomNodeIndices[lookupsIndex]
        
        for lookupIndex = 0 to lookups.Length - 1 do
            let randomNodeIndex = lookups[lookupIndex]

            match intEncodedNodes[randomNodeIndex] with
            | IntEncoding.ActivePattern.BufferId bufferId -> acc <- acc + 1
            | IntEncoding.ActivePattern.ConstraintId constraintId -> acc <- acc + 2
            | IntEncoding.ActivePattern.MergeId mergeId -> acc <- acc + 3
            | IntEncoding.ActivePattern.SplitId splitId -> acc <- acc + 4

    acc


[<Benchmark>]
member _.IntEncodingWithPartialActivePatternRandomAccess () =
    let mutable acc = 0

    for lookupsIndex = 0 to loopCount - 1 do
        let lookups = randomNodeIndices[lookupsIndex]
        
        for lookupIndex = 0 to lookups.Length - 1 do
            let randomNodeIndex = lookups[lookupIndex]

            match intEncodedNodes[randomNodeIndex] with
            | IntEncoding.PartialActivePattern.BufferId bufferId -> acc <- acc + 1
            | IntEncoding.PartialActivePattern.ConstraintId constraintId -> acc <- acc + 2
            | IntEncoding.PartialActivePattern.MergeId mergeId -> acc <- acc + 3
            | IntEncoding.PartialActivePattern.SplitId splitId -> acc <- acc + 4
            | _ -> failwith "¯\_(ツ)_/¯"

    acc
```

I also create four additional tests which perform the same amount of work as the above four, but iterate through the arrays in order instead of randomly jumping around. This will show us the performance difference between random versus sequential access.

## Results

Since I am measuring hardware counters, I have to run the terminal as admin; otherwise, I won't have access to the data. If you want to test this yourself, you need to do the same.

When I run the tests, I get the following table:

| Method                                          |      Mean |    Error |   StdDev | BranchMispredictions/Op | CacheMisses/Op |      Gen 0 |     Allocated |
| ----------------------------------------------- | --------: | -------: | -------: | ----------------------: | -------------: | ---------: | ------------: |
| DuEncodingRandomAccess                          |  83.09 ms | 1.122 ms | 0.995 ms |               7,414,534 |        400,225 |          - |         175 B |
| StructDuEncodingRandomAccess                    |  83.56 ms | 0.766 ms | 0.640 ms |               7,415,626 |        409,418 |          - |         175 B |
| IntEncodingWithActivePatternRandomAccess        | 134.88 ms | 2.650 ms | 4.845 ms |               8,126,171 |      1,070,592 | 28500.0000 | 240,000,358 B |
| IntEncodingWithPartialActivePatternRandomAccess |  86.43 ms | 0.841 ms | 0.787 ms |               7,995,620 |        406,096 |          - |         191 B |
| DuEncodingLinearAccess                          |  14.86 ms | 0.090 ms | 0.084 ms |                   5,073 |          7,701 |          - |          22 B |
| StructDuEncodingLinearAccess                    |  17.35 ms | 0.151 ms | 0.142 ms |                 119,799 |         13,508 |          - |          36 B |
| IntEncodingWithActivePatternLinearAccess        |  74.67 ms | 1.018 ms | 0.903 ms |                 167,078 |        677,829 | 28571.4286 | 240,000,191 B |
| IntEncodingWithPartialActivePatternLinearAccess |  22.83 ms | 0.372 ms | 0.348 ms |                   8,151 |         33,225 |          - |          45 B |

We see that the normal reference-based DU encoding gives us the best performance for both tests. This honestly surprised me. I would have thought that the Int Encoding would have yielded better results. There is some serious voodoo going on in the F# compiler. I wanted to dig into this more, but my work demands that I cut myself off here. I would like to get this info out for others to look at since I have not been able to find other blog posts which deal with this.

I welcome feedback and critique. You can find the code [here](https://github.com/matthewcrews/ActivePatternPerformance). Let me know if I missed something obvious. Eventually, I hope to have the time to dig deeper into this. In the meantime, I plan to stick with the default DU until I can figure out if I can make something faster for my use case. Let me know if you have ideas for going faster or other ideas I should test. I can be found on [Twitter](https://twitter.com/McCrews) @McCrews, or you can email `matthew@crews.email`.
# F# Loves Mutation


Mutation gets a bad wrap, and some believe Mutation is evil and should be avoided at all costs. For a while, I was also in this camp. As time went by, I realized there is nothing wrong with Mutation. The problem is when Mutation is misapplied.

Some of you may be familiar with a paper by Edgar Dijkstra called ["Go To Statement Considered Harmful"](https://homepages.cwi.nl/~storm/teaching/reader/Dijkstra68.pdf). In it, Dijkstra argues that the `goto` statement is a serious source of problems in code and should be avoided. His opening paragraph states that it should be abolished from all "higher level" languages and reserved for machine code.

![XKCD Goto Comic](https://imgs.xkcd.com/comics/goto.png)

Some would also put Mutation into the same list of things that should be forbidden. Mutation can indeed be a challenging thing to debug. Some would like the `pure` keyword added to the F# language so that the compiler can ensure that a function does not use Mutation.

There was a time that I, too, was caught up in the idea that Mutation should be abolished. I thought that to write high-quality software, we needed to work exclusively with immutable data and data structures.

Over time my understanding has evolved, though. Mutation is a tool, just like the Go To statement, and it can be misapplied and overused, but it isn't bad. Let's look at some code and contrast a mutation-free approach and Mutation based approach. For the first example, let's find the maximum element in an array of integers.

## Max Item in Array

Our first function will not use any mutation to find the maximum value, and I'm assuming that the array passed to the function is non-null and not empty to keep the code simple. The most straightforward approach I could come up with is a recursive function that loops through the values and keeps track of the observed maximum value.

```fsharp
// Maximum value in array
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
```

For someone who has worked with recursion, this is not intimidating, but if you haven't written many recursive functions, this may confuse you. Only in the last two years did I become comfortable with recursion, so I know firsthand how disorienting code like this can be. Let's contrast that with an implementation that can use Mutation.

```fsharp
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
```

I believe that most developers would find this easier to follow. We are using two mutable values, `acc` and `idx`, but they don't leak outside the function. The Mutation is isolated to the context in which it is used. In fact, this example comes from the F# [source code](https://github.com/dotnet/fsharp/blob/1f1d4bd02b3ea3b3c24e1366bab9a88a9444b145/src/fsharp/FSharp.Core/array.fs#L1127).

Let's take this a step further and see the performance difference between these two implementations. I'm going to create 100,000 arrays with between 10 and 10,000 random, positive integers and call the two different `max` function implementations to see the difference in performance. You can check the code and run it yourself [here](./Benchmark/Program.fs). I am using [BenchmarkDotNet](https://github.com/dotnet/BenchmarkDotNet) for the testing. When I run them I get the following result.

|        Method |     Mean |   Error |   StdDev |   Median |
|-------------- |---------:|--------:|---------:|---------:|
|  MutationFree | 292.7 ms | 6.65 ms | 18.10 ms | 285.4 ms |
| MutationBased | 281.8 ms | 5.04 ms |  4.71 ms | 281.5 ms |

Turns out the mutation-free approach is ever so slightly slower, along with being more confusing for a beginner developer. Now, a more extreme case.

## Sorting a List

Let's look at how Quicksort performs on a list of integers. I am referencing [Yan Ciu](https://theburningmonk.com/2012/02/f-simple-quicksort-implementation/) for my implementation.

```fsharp
    let rec sort (values: list<int>) =
        match values with
        | [] -> values
        | [x] -> values
        | head::tail ->
            let less, greater = List.partition ((>=) head) tail
            List.concat [sort(less); [head]; sort(greater)]
```

Quicksort is easy to express in F# but let's see how the performance stacks up against the built-in `List.sort` function. We'll have each function sort a list of 1,000 positive integers for our testing. You can see the tests [here](./Benchmark/Program.fs).

When we run it, we see the following.

|             Method |      Mean |    Error |   StdDev |
|------------------- |----------:|---------:|---------:|
| ImmutableQuicksort | 154.46 us | 2.939 us | 3.717 us |
|        BuiltInSort |  14.73 us | 0.286 us | 0.436 us |

The performance is not even close. Granted, this Quicksort is not optimized. It's a naive approach, but I would suggest it will never get as fast as the built-in sort. If you look at the [source code](https://github.com/dotnet/fsharp/blob/1f1d4bd02b3ea3b3c24e1366bab9a88a9444b145/src/fsharp/FSharp.Core/list.fs#L508) for `List.sort` you'll see this.

```fsharp
let sort list =
    match list with
    | [] | [_] -> list
    | _ ->
        let array = Microsoft.FSharp.Primitives.Basics.List.toArray list
        Microsoft.FSharp.Primitives.Basics.Array.stableSortInPlace array
        Microsoft.FSharp.Primitives.Basics.List.ofArray array
```

F# is converting the list to an array, using an optimized sort on the array, and then turning it back into a list for the return value. You will see this kind of behavior all over the F# source.

We as users are getting an immutable experience when we work with these functions and types, but under the hood, there is Mutation.

## Conclusion

Mutation is a powerful tool for when you need to go fast. What I failed to realize early on was that Mutation has a place. The key thing is that the Mutation is isolated and does not leak out into the rest of the program.

I am actually working on an update to the SliceMap library for Flips. The underlying data structure will re-arrange itself to best serve the query whenever you query the data. There is constant Mutation occurring under the covers, but the user never sees it.

If you liked this post and want to stay in the loop for the work I am doing with Mathematical Programming, please subscribe to my list!

{{<subscribe>}}


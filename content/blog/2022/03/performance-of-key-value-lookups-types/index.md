---
title: Performance of Key/Value Collections for Updating
date: 2022-03-19
draft: false
tag: fsharp, performance
---

I have been working on a simulation engine that requires a key/value collection for holding the flow rates through a network as part of a [Push-Relabel algorithm](https://en.wikipedia.org/wiki/Push%E2%80%93relabel_maximum_flow_algorithm). This is the most performance-critical code in the engine, so I needed to find the fastest way to perform a lookup, update, and store for a key/value pair. The prevailing wisdom is to use a .NET `Dictionary,` but I was curious how the performance would compare to the F# `Map` type. A `Map` is backed by an [AVL Tree](https://en.wikipedia.org/wiki/AVL_tree) and has a read and write performance of $O(log(n))$ while `Dictionary` is a [Hash Table](https://en.wikipedia.org/wiki/Hash_table) with an algorithmic complexity of $O(1)$ for reads and writes.

For my use case, I need to read a value from the collection, perform a minor update, and then update the value for the key in the collection.

```fsharp
dictionary[key] <- dictionary[key] + 1.0 // Trivial work example
```

## Test  Setup

To make it easier to set up tests across various collection sizes in `benchmarkDotNet,` I defined an Enum `Size` that would indicate the size of the collection I wanted to test against.

```fsharp
// Enum for the different size
[<Struct>]
type Size =
    | ``10`` = 0
    | ``100`` = 1
    | ``1_000`` = 2
    | ``10_000`` = 3
    | ``100_000`` = 4
    | ``1_000_000`` = 5
```

The Enum cases will map to the index of the data set that I want to test against. I now define a `Benchmark` class to hold my tests and generate the necessary data.

```fsharp
type Benchmarks () =

    // The number of lookups I will perform in each test
    let lookupCount = 10
    // A random number generator to create random indices
    // into the collections.
    let rng = Random 1337

    // Lookup array to map Size -> Count
    let sizeToCount =
        [|
            10
            100
            1_000
            10_000
            100_000
            1_000_000
        |]

    // An array of different Maps for each size in Size
    let maps =
        sizeToCount
        |> Array.map (fun count ->
            Map [for i in 0 .. count - 1 -> string i, 0.0]
            )
    
    // An array of different Dictionaries for each size in Size
    let dictionaries =
        sizeToCount
        |> Array.map (fun count ->
            Dictionary [for i in 0 .. count - 1 -> KeyValuePair (string i, 0.0)]
            )
```

I then add a member to the `Benchmarks` class called `Size` so that `benchmarkDotNet` can update the field to automatically test across each of the cases in the `Size` Enum.

```fsharp
[<Params(Size.``10``, Size.``100``, Size.``1_000``, Size.``10_000``,
            Size.``100_000``, Size.``1_000_000``)>]
member val Size = Size.``10`` with get, set
```

When `benchmarkDotNet` runs, it will see that the `Size` property has been decorated with the different values we want it to test. It will run each of our tests with every value we decorate the `Size` property with.

I now create the test for the `Map` collections. I index into the `maps` array and retrieve the `Map` associated with the case of `Size`. I then retrieve the `keys` associated with the `Size`. This ensures that all of the keys we will lookup can be found in the collection. You will see that I use `mutable` on the `map` value and then return it at the end of the method. This is to ensure that the CLR doesn't compile the work away. This is not how I would typically use a `Map`.

```fsharp
[<Benchmark>]
member b.Map () =
    // We using mutation to ensure the compiler doesn't eliminate unnecessary work
    let mutable map = maps[int b.Size]
    let keys = keysForSize[int b.Size]

    // We are making memory access pattern as predictable as possible
    // to eliminate cache hits from the work of getting the key. We don't use
    // IEnumerable to reduce the overhead.
    for i = 0 to keys.Length - 1 do
        let key = keys[i]
        let newValue = map[key] + 1.0
        map <- map.Add (key, newValue) // Do a minimal amount of work

    map
```

We iterate through each key in the `keys` array associated with the size we are testing. I wanted to try more than one lookup, so I wasn't getting unexpected performance benefits from the CPU being lucky for a lookup of a single value.

I create the same test for the `Dictionary` type. The work is the same, even though it looks slightly different. This is due to `Dictionary` having a different API than `Map`.

```fsharp
[<Benchmark>]
member b.Dictionary () =
    let dictionary = dictionaries[int b.Size]
    let keys = keysForSize[int b.Size]

    for i = 0 to keys.Length - 1 do
        let key = keys[i]
        dictionary[key] <- dictionary[key] + 1.0 // Do a minimal amount of work

    dictionary
```

I now define my `main` method and run the benchmarks.

```fsharp
[<EntryPoint>]
let main _ =

    // I don't care about what Run returns so I'm ignoring it
    let _ = BenchmarkRunner.Run<Benchmarks>()
    0
```

Another thing worth mentioning is that I am restricted to the `x64` platform, so I update the `.fsproj` of the project to make sure that I only build and test for `x64`.

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net6.0</TargetFramework>
<!--    Restricts to the x64 platform-->
    <Platform>x64</Platform> 
  </PropertyGroup>

  <ItemGroup>
    <Compile Include="Program.fs" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="BenchmarkDotNet" Version="0.13.1" />
    <PackageReference Include="BenchmarkDotNet.Diagnostics.Windows" Version="0.13.1" />
  </ItemGroup>

</Project>
```

I get the following table when I run these benchmarks with `dotnet run -c Release`.

| Method                 | Size      |       Mean |     Error |    StdDev |     Median |
| ---------------------- | --------- | ---------: | --------: | --------: | ---------: |
| Map                    | 10        |   986.3 ns |  19.51 ns |  38.06 ns |   972.5 ns |
| Dictionary             | 10        |   253.2 ns |   3.57 ns |   3.17 ns |   252.4 ns |
| Map                    | 100       | 2,062.4 ns |  39.33 ns |  36.79 ns | 2,053.8 ns |
| Dictionary             | 100       |   325.2 ns |   6.18 ns |   6.34 ns |   324.7 ns |
| Map                    | 1_000     | 3,468.0 ns |  68.38 ns | 130.09 ns | 3,394.7 ns |
| Dictionary             | 1_000     |   299.8 ns |   5.99 ns |  10.02 ns |   298.1 ns |
| Map                    | 10_000    | 4,383.1 ns |  86.00 ns |  80.45 ns | 4,393.2 ns |
| Dictionary             | 10_000    |   322.4 ns |   2.15 ns |   1.79 ns |   322.6 ns |
| Map                    | 100_000   | 5,571.8 ns |  90.32 ns |  84.48 ns | 5,569.3 ns |
| Dictionary             | 100_000   |   338.0 ns |   2.71 ns |   2.26 ns |   338.6 ns |
| Map                    | 1_000_000 | 7,695.1 ns | 150.65 ns | 263.86 ns | 7,690.7 ns |
| Dictionary             | 1_000_000 |   369.2 ns |   2.69 ns |   2.10 ns |   369.5 ns |

This is in alignment with my expectations. Since `Map` is an immutable data structure, it needs to copy a significant amount of data when creating the updated `Map`. This scenario is one of the worst ways you can use a `Map`. `Dictionary,` on the other hand, is a mutable data structure, so in this case, all of the work is in computing the hash code to find the correct bucket in the backing array and then the equality check to make sure the key in the bucket is the same as the one you are looking up.

`Map` is a great data structure, but this is not the best use case for it. I knew this ahead of time, but it's good to validate your assumptions.

## Even Faster?

Can we go even faster, though? You may notice that we have to look up the key twice for each update. Once to get the value so that we can add `1.0` to it and a second time to store the updated value. It's all on this single line of code:

```fsharp
// First lookup is here to get the value to add 1.0 to it
//                             ↓
dictionary[key] <- dictionary[key] + 1.0
//          ↑
// Second lookup happens here to store the value
```

Wouldn't it be nice if we didn't have to do that work twice? What if instead of the `Dictionary` returning by value, it returns by reference? This way, we only need to perform the lookup once?

Now, some of you may start balking, saying that's dangerous. You can create some hideous bugs if you misuse this. It's difficult enough that you will not find it in the `Dictionary` class itself. You need to use a method found on the `CollectionsMarshal` class, in the `System.Runtime.InteropServices` namespace. The name of the method I want is `GetValueRefOrAddDefault`. This method has an unusual function signature, so I want to unpack what is happening.

```fsharp
CollectionsMarshal.GetValueRefOrAddDefault<'TKey,'TValue>(dictionary: Dictionary<'TKey,'TValue>, key: 'TKey, exists: byref<bool>) : byref<'TValue
```

F# does some interesting things for you implicitly regarding the ref types: `byref`, `inref`, and `outref`. I highly recommend you read the Microsoft [docs](https://docs.microsoft.com/en-us/dotnet/fsharp/language-reference/byrefs) on refs. The first time you read it, you may be confused. I was, but the more I work with the ref types, the more it makes sense.

> **Aside:** F# is designed as a succinct, expressive, and efficient language. It sometimes gets a reputation for being slow. I will concede if you write entirely idiomatic F#, your performance may not be on the level of a C++ program. BUT, that's not to say you can't write fast F# code. F# has defaults and idioms, making it easier to compose correct programs quickly.

>  People don't talk about much, though you can turn all the safeties in F# off if you want to. If you're going to drop down to raw native pointers, you can. F# forces you to be more explicit about wanting to violate the safeties

The usual way of working with a .NET API, which uses a `byref` as one of the parameters for the method, is to use a `match...with` statement to unpack the values. The most common method I use with this behavior is the `TryGetValue` method of `Dictionary`. `TryGetValue` has the following signature:

```fsharp
Dictionary.TryGetValue(key: string, value: byref<'T>) : bool
```

You will see that the method expects you to pass a `byref<'T>`. If the dictionary has the value, it will put it in the location `byref<'T>` points and return a `true`. If it does not find the value, it will not update the value the `byref<'T>` points to and returns `false`. Instead of declaring a `byref<'T>,` we instead use the `match ... with` syntax, and F# will implicitly do the work of creating the `byref<'T>` for us.

```fsharp
match dictionary.TryGetValue key with
| true, value -> () // Do something with value
| false, _ -> () // Do something without the value
```

`value`, in this case, will be the value that was found. It will NOT be a `byref<'T>` pointing to the value found. F# implicitly dereferences the `byref` for us. This implicit dereferencing is nice most of the time but, in our case, is the opposite of what we want. Therefore we must define our byrefs and pass them to the method.

```fsharp
// dictionary is a Dictionary<int, float>
// key is a `int` we are wanting to the look up the float for
let mutable wasFound = Unchecked.defaultof<_>
let valueRef = &CollectionsMarshal.GetValueRefOrAddDefault (dictionary, key, &wasFound)
//             ↑ Notice this `&`
```

`wasFound` is a `byref<bool>` that we pass to the method. You'll notice that we are not giving in a `byref<float>` for the method to fill in for us. Instead, we are using the `&` operator prepended to the method to tell F# that we want it to return the `byref` for us, not the value. If we did not prepend the `&` to the method call, F# would implicitly dereference the `byref` for us. This is another case of the F# defaults leaning toward safety. Fortunately, we can turn the safeties off.

Now that we know how to work with the `GetValueRefOrAddDefault` method, we create a test and compare the performance to our other tests.

```fsharp
[<Benchmark>]
member b.DictionaryGetRef () =
    let dictionary = dictionaries[int b.Size]
    let keys = keysForSize[int b.Size]
    let mutable wasFound = Unchecked.defaultof<_>

    for i = 0 to keys.Length - 1 do
        let key = keys[i]
        let valueRef = &CollectionsMarshal.GetValueRefOrAddDefault (dictionary, key, &wasFound)
        valueRef <- valueRef + 1.0

    dictionary
```

When we want to add 1.0 to our value, you'll notice that we don't have to dereference the `byref<float>`. F# is doing that work for us. This contrasts with C++, where you would need to dereference a pointer explicitly.

We get the following result if we run our benchmarks with this new test.

| Method                 | Size      |       Mean |     Error |    StdDev |     Median |
| ---------------------- | --------- | ---------: | --------: | --------: | ---------: |
| Map                    | 10        |   986.3 ns |  19.51 ns |  38.06 ns |   972.5 ns |
| Dictionary             | 10        |   253.2 ns |   3.57 ns |   3.17 ns |   252.4 ns |
| DictionaryGetRef       | 10        |   119.3 ns |   1.58 ns |   1.48 ns |   119.0 ns |
| Map                    | 100       | 2,062.4 ns |  39.33 ns |  36.79 ns | 2,053.8 ns |
| Dictionary             | 100       |   325.2 ns |   6.18 ns |   6.34 ns |   324.7 ns |
| DictionaryGetRef       | 100       |   124.5 ns |   2.52 ns |   3.10 ns |   124.2 ns |
| Map                    | 1_000     | 3,468.0 ns |  68.38 ns | 130.09 ns | 3,394.7 ns |
| Dictionary             | 1_000     |   299.8 ns |   5.99 ns |  10.02 ns |   298.1 ns |
| DictionaryGetRef       | 1_000     |   130.9 ns |   2.52 ns |   2.69 ns |   130.1 ns |
| Map                    | 10_000    | 4,383.1 ns |  86.00 ns |  80.45 ns | 4,393.2 ns |
| Dictionary             | 10_000    |   322.4 ns |   2.15 ns |   1.79 ns |   322.6 ns |
| DictionaryGetRef       | 10_000    |   141.4 ns |   2.12 ns |   1.77 ns |   140.9 ns |
| Map                    | 100_000   | 5,571.8 ns |  90.32 ns |  84.48 ns | 5,569.3 ns |
| Dictionary             | 100_000   |   338.0 ns |   2.71 ns |   2.26 ns |   338.6 ns |
| DictionaryGetRef       | 100_000   |   130.5 ns |   1.13 ns |   1.05 ns |   130.2 ns |
| Map                    | 1_000_000 | 7,695.1 ns | 150.65 ns | 263.86 ns | 7,690.7 ns |
| Dictionary             | 1_000_000 |   369.2 ns |   2.69 ns |   2.10 ns |   369.5 ns |
| DictionaryGetRef       | 1_000_000 |   152.1 ns |   1.09 ns |   0.91 ns |   152.1 ns |

We see that the `GetValueRefOrAddDefault` method approach is more than twice as fast. A word of warning before you go and rewrite how you use `Dictionary`. If you hold onto the reference to the value while performing other updates on the `Dictionary`, you could get into some messy situations where the `Dictionary` has moved items around due to deletions or inserts. I am doing no other work between getting the reference, calculating an update, and then updating the value.

## A Safer Approach

Instead of getting a reference to the value, we could wrap our values in a `ValueWrapper` class and store those in the `Dictionary`. This was proposed in a GitHub discussion where people were debating the addition of the `GetValueRefOrAddDefault` method. I decided to code one up and compare the performance out of curiosity.

```fsharp
type ValueWrapper<'T when 'T : struct> (value: 'T) =
    member val Value = value with get, set
```

I test this approach; I now have to wrap my values in the ValueWrapper type.

```fsharp
let wrappedValueDictionaries =
    sizeToCount
    |> Array.map (fun count ->
        Dictionary [for i in 0 .. count - 1 -> KeyValuePair (string i, ValueWrapper 0.0)]
        )
```

And create a test for it...

```fsharp
[<Benchmark>]
member b.ValueWrappedDictionary () =
    let valueWrappedDictionary = wrappedValueDictionaries[int b.Size]
    let keys = keysForSize[int b.Size]

    for i = 0 to keys.Length - 1 do
        let key = keys[i]
        let v = valueWrappedDictionary[key]
        v.Value <- v.Value + 1.0 // Do a minimal amount of work

    valueWrappedDictionary
```

We see that this WrappedValue approach is just as fast when we run the benchmarks.

| Method                 | Size      |       Mean |     Error |    StdDev |     Median |
| ---------------------- | --------- | ---------: | --------: | --------: | ---------: |
| Map                    | 10        |   986.3 ns |  19.51 ns |  38.06 ns |   972.5 ns |
| Dictionary             | 10        |   253.2 ns |   3.57 ns |   3.17 ns |   252.4 ns |
| ValueWrappedDictionary | 10        |   104.8 ns |   1.99 ns |   1.86 ns |   104.0 ns |
| DictionaryGetRef       | 10        |   119.3 ns |   1.58 ns |   1.48 ns |   119.0 ns |
| Map                    | 100       | 2,062.4 ns |  39.33 ns |  36.79 ns | 2,053.8 ns |
| Dictionary             | 100       |   325.2 ns |   6.18 ns |   6.34 ns |   324.7 ns |
| ValueWrappedDictionary | 100       |   132.3 ns |   2.08 ns |   1.95 ns |   131.7 ns |
| DictionaryGetRef       | 100       |   124.5 ns |   2.52 ns |   3.10 ns |   124.2 ns |
| Map                    | 1_000     | 3,468.0 ns |  68.38 ns | 130.09 ns | 3,394.7 ns |
| Dictionary             | 1_000     |   299.8 ns |   5.99 ns |  10.02 ns |   298.1 ns |
| ValueWrappedDictionary | 1_000     |   132.2 ns |   1.06 ns |   0.94 ns |   132.2 ns |
| DictionaryGetRef       | 1_000     |   130.9 ns |   2.52 ns |   2.69 ns |   130.1 ns |
| Map                    | 10_000    | 4,383.1 ns |  86.00 ns |  80.45 ns | 4,393.2 ns |
| Dictionary             | 10_000    |   322.4 ns |   2.15 ns |   1.79 ns |   322.6 ns |
| ValueWrappedDictionary | 10_000    |   150.8 ns |   0.90 ns |   0.80 ns |   150.6 ns |
| DictionaryGetRef       | 10_000    |   141.4 ns |   2.12 ns |   1.77 ns |   140.9 ns |
| Map                    | 100_000   | 5,571.8 ns |  90.32 ns |  84.48 ns | 5,569.3 ns |
| Dictionary             | 100_000   |   338.0 ns |   2.71 ns |   2.26 ns |   338.6 ns |
| ValueWrappedDictionary | 100_000   |   151.3 ns |   0.75 ns |   0.63 ns |   151.4 ns |
| DictionaryGetRef       | 100_000   |   130.5 ns |   1.13 ns |   1.05 ns |   130.2 ns |
| Map                    | 1_000_000 | 7,695.1 ns | 150.65 ns | 263.86 ns | 7,690.7 ns |
| Dictionary             | 1_000_000 |   369.2 ns |   2.69 ns |   2.10 ns |   369.5 ns |
| ValueWrappedDictionary | 1_000_000 |   153.3 ns |   1.21 ns |   1.13 ns |   153.2 ns |
| DictionaryGetRef       | 1_000_000 |   152.1 ns |   1.09 ns |   0.91 ns |   152.1 ns |

The nice thing about this approach is that even when the `Dictionary` gets reorganized, our `ValueWrapper` will still point to the correct data piece. The downside is that this will allocate more memory since each `ValueWrapper` is an object that needs to be allocated on the heap. You also lose any cache locality benefits since the `ValueWrapper` objects could spread all over memory. We aren't observing any downsides in this tiny benchmark, but it's essential to be aware. There could be some performance implications in the context of a larger program.

If you feel like playing with the code yourself, you can find all the tests [here](https://github.com/matthewcrews/KeyValueLookupPerformance). Let me know if you have ideas for going faster or other collections I should test. I can be found on [Twitter](https://twitter.com/McCrews) @McCrews, or you can email `matthew@crews.email`.